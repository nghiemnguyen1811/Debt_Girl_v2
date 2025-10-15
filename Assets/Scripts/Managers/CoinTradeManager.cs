using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages coin trading with delayed hidden price fluctuation logic.
/// Coin value scales with player level but only updates to the new level's value
/// after the player has sold all previously owned coins.
/// </summary>
public class CoinTradeManager : SingletonMonobehaviour<CoinTradeManager>
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────

    [Header("Coin UI")]
    [SerializeField] private Image coinTrendImage;
    [SerializeField] private TextMeshProUGUI coinValueText;
    [SerializeField] private TextMeshProUGUI buyAmountText;
    [SerializeField] private TextMeshProUGUI sellAmountText;
    [SerializeField] private TextMeshProUGUI ownedCoinsText;
    [SerializeField] private TextMeshProUGUI fluctuationTimerText;

    [Header("Price Delta UI Array (0 = decrease, 1 = increase)")]
    [SerializeField] private PriceDeltaUI[] priceDeltaUIs;

    [Header("Buy Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button plusBuyButton;
    [SerializeField] private Button minusBuyButton;

    [Header("Sell Buttons")]
    [SerializeField] private Button sellButton;
    [SerializeField] private Button plusSellButton;
    [SerializeField] private Button minusSellButton;

    [Header("Coin Trend Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    [Header("Fluctuation Settings")]
    [SerializeField] private Vector2 fluctuationInterval = new(120f, 180f);
    [SerializeField] private Vector2 fluctuationPercentRange = new(0.1f, 0.4f);
    [SerializeField] private float minCoinValue = 10f;

    [Header("Level Scaling Settings")]
    [SerializeField] private double baseValue = 100;
    [SerializeField] private double growthFactor = 20;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────

    private double coinValue;
    private double pendingCoinValue;
    private double nextLevelCoinValue;

    private int buyAmount = 0;
    private int sellAmount = 0;
    private int ownedCoins = 0;

    private Coroutine fluctuationCoroutine;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        SetupUI();
        RegisterUIEvents();
        HandleLevelChanged();
        ResetFluctuationTimer();
        HideAllPriceDeltaUI();

        GameManager.Instance.OnLevelChanged += HandleLevelChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= HandleLevelChanged;
    }

    // ─────────────────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────────────────

    private void SetupUI()
    {
        UpdateCoinTrend(normalSprite);
        buyButton.interactable = true;
        sellButton.interactable = false;
        UpdateUI();
    }

    private void RegisterUIEvents()
    {
        plusBuyButton.onClick.AddListener(() => ChangeBuyAmount(+1));
        minusBuyButton.onClick.AddListener(() => ChangeBuyAmount(-1));
        plusSellButton.onClick.AddListener(() => ChangeSellAmount(+1));
        minusSellButton.onClick.AddListener(() => ChangeSellAmount(-1));
        buyButton.onClick.AddListener(HandleBuy);
        sellButton.onClick.AddListener(HandleSell);
    }

    // ─────────────────────────────────────────────────────
    // Level Scaling
    // ─────────────────────────────────────────────────────

    private void HandleLevelChanged()
    {
        int level = GameManager.Instance.CurrentLevel;

        if (ownedCoins <= 0)
        {
            // Apply new value immediately if no coins owned
            coinValue = baseValue + (level - 1) * growthFactor;
            pendingCoinValue = coinValue;
        }
        else
        {
            // Store next level value until player sells all coins
            nextLevelCoinValue = baseValue + (level - 1) * growthFactor;
        }

        UpdateUI();
    }

    // ─────────────────────────────────────────────────────
    // Buy / Sell Logic
    // ─────────────────────────────────────────────────────

    private void ChangeBuyAmount(int delta)
    {
        int newAmount = buyAmount + delta;
        double totalCost = newAmount * coinValue;

        if (newAmount >= 0 && MoneyManager.Instance.HasEnoughMoney(totalCost))
        {
            buyAmount = newAmount;
            buyAmountText.text = buyAmount.ToString();
        }
    }

    private void ChangeSellAmount(int delta)
    {
        int newAmount = sellAmount + delta;

        if (newAmount >= 0 && newAmount <= ownedCoins)
        {
            sellAmount = newAmount;
            sellAmountText.text = sellAmount.ToString();
        }
    }

    private void HandleBuy()
    {
        if (buyAmount <= 0) return;

        double totalCost = buyAmount * coinValue;
        if (!MoneyManager.Instance.HasEnoughMoney(totalCost)) return;

        MoneyManager.Instance.ChangeMoneys(-totalCost);
        ownedCoins += buyAmount;
        buyAmount = 0;

        AutoSave();

        buyButton.interactable = false;
        sellButton.interactable = true;

        if (fluctuationCoroutine != null)
            StopCoroutine(fluctuationCoroutine);

        fluctuationCoroutine = StartCoroutine(CoinFluctuationLoop());

        UpdateUI();
        AudioManager.Instance.PlayInteractSound(3);
    }

    private void HandleSell()
    {
        if (sellAmount <= 0 || ownedCoins <= 0) return;

        // Apply current pending value for this transaction
        coinValue = pendingCoinValue;
        double gain = sellAmount * coinValue;

        ownedCoins -= sellAmount;
        sellAmount = 0;

        AutoSave();
        MoneyManager.Instance.ChangeMoneys(gain);

        if (ownedCoins <= 0)
        {
            // Apply next stored level value if available
            if (nextLevelCoinValue > 0)
            {
                coinValue = nextLevelCoinValue;
                pendingCoinValue = nextLevelCoinValue;
                nextLevelCoinValue = 0;
            }

            buyButton.interactable = true;
            sellButton.interactable = false;

            if (fluctuationCoroutine != null)
                StopCoroutine(fluctuationCoroutine);

            UpdateCoinTrend(normalSprite);
            ResetFluctuationTimer();
            HideAllPriceDeltaUI();
        }

        UpdateUI();
        AudioManager.Instance.PlayInteractSound(1);
    }

    // ─────────────────────────────────────────────────────
    // Reset Helpers
    // ─────────────────────────────────────────────────────

    public void ResetAll()
    {
        buyAmount = 0;
        sellAmount = 0;
        UpdateUI();
        ResetFluctuationTimer();
    }

    private void ResetFluctuationTimer()
    {
        UpdateFluctuationTimer(0f);
    }

    private void UpdateFluctuationTimer(float remaining)
    {
        if (fluctuationTimerText == null) return;
        fluctuationTimerText.text = DoubleUtilities.UpdateTime(Mathf.CeilToInt(remaining));
    }

    // ─────────────────────────────────────────────────────
    // UI Helpers
    // ─────────────────────────────────────────────────────

    private void UpdateUI()
    {
        coinValueText.text = $"{Mathf.RoundToInt((float)coinValue)}원";
        buyAmountText.text = buyAmount.ToString();
        sellAmountText.text = sellAmount.ToString();
        ownedCoinsText.text = ownedCoins.ToString();

        UIManager.Instance?.UpdateMoney(MoneyManager.Instance.GetMoneys(), true);
    }

    private void UpdateCoinTrend(Sprite sprite)
    {
        if (coinTrendImage == null || sprite == null) return;

        coinTrendImage.sprite = sprite;
        coinTrendImage.enabled = false;
        coinTrendImage.enabled = true;
    }

    private void HideAllPriceDeltaUI()
    {
        if (priceDeltaUIs.Length >= 2)
        {
            priceDeltaUIs[0].Hide();
            priceDeltaUIs[1].Hide();
        }
    }

    // ─────────────────────────────────────────────────────
    // Price Fluctuation Logic
    // ─────────────────────────────────────────────────────

    private IEnumerator CoinFluctuationLoop()
    {
        UpdateCoinTrend(normalSprite);

        while (true)
        {
            // Random wait time for next fluctuation
            float waitTime = Random.Range(fluctuationInterval.x, fluctuationInterval.y);
            float remaining = waitTime;

            // Countdown until fluctuation
            while (remaining > 0)
            {
                UpdateFluctuationTimer(remaining);
                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            // Apply price fluctuation when countdown ends
            float random = Random.value;
            float fluctuation = Random.Range(fluctuationPercentRange.x, fluctuationPercentRange.y);

            if (random < 0.8f)
            {
                double newValue = System.Math.Max(minCoinValue, coinValue * (1 - fluctuation));
                ApplyFluctuation(true, newValue);
            }

            else
            {
                double newValue = coinValue * (1 + fluctuation);
                ApplyFluctuation(false, newValue);
            }
        }
    }

    private void ApplyFluctuation(bool isDecrease, double newValue)
    {
        pendingCoinValue = newValue;

        UpdateCoinTrend(isDecrease ? downSprite : upSprite);

        if (priceDeltaUIs.Length >= 2)
        {
            int showIndex = isDecrease ? 0 : 1;
            int hideIndex = isDecrease ? 1 : 0;

            priceDeltaUIs[showIndex].Show(pendingCoinValue);
            priceDeltaUIs[hideIndex].Hide();
        }
    }

    // ─────────────────────────────────────────────────────
    // Save / Load API
    // ─────────────────────────────────────────────────────

    public void AutoSave()
    {
        if (SaveManager.Data == null) return;

        SaveManager.Data.ownedCoins = ownedCoins;
        SaveManager.SaveGame();

        Debug.Log($"[CoinTradeManager] AutoSaved → ownedCoins={ownedCoins}");
    }

    public void ImportSaveData(SaveData data)
    {
        if (data == null) return;

        ownedCoins = data.ownedCoins;

        // Update UI & restore proper button states
        if (ownedCoins > 0)
        {
            buyButton.interactable = false;
            sellButton.interactable = true;

            if (fluctuationCoroutine != null)
                StopCoroutine(fluctuationCoroutine);

            fluctuationCoroutine = StartCoroutine(CoinFluctuationLoop());
        }

        else
        {
            buyButton.interactable = true;
            sellButton.interactable = false;
        }

        UpdateUI();
    }

}

// ─────────────────────────────────────────────────────
// Nested Serializable Class
// ─────────────────────────────────────────────────────

[System.Serializable]
public class PriceDeltaUI
{
    [SerializeField] private GameObject deltaGroup;     // UI group (icon + text)
    [SerializeField] private TextMeshProUGUI deltaText; // UI text

    public void Show(double value)
    {
        if (deltaGroup == null || deltaText == null) return;

        deltaGroup.SetActive(true);
        deltaText.text = $"{System.Math.Round(value)}원";
    }

    public void Hide()
    {
        if (deltaGroup != null)
            deltaGroup.SetActive(false);
    }
}
