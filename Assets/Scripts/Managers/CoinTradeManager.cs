using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages coin trading with delayed hidden price fluctuation logic.
/// </summary>
public class CoinTradeManager : MonoBehaviour
{
    [Header("Coin UI")]
    [SerializeField] private Image coinTrendImage;
    [SerializeField] private TextMeshProUGUI coinValueText;
    [SerializeField] private TextMeshProUGUI buyAmountText;
    [SerializeField] private TextMeshProUGUI sellAmountText;
    [SerializeField] private TextMeshProUGUI ownedCoinsText;

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
    [SerializeField] private Vector2 fluctuationInterval = new(30f, 60f);
    [SerializeField] private Vector2 fluctuationPercentRange = new(0.1f, 0.3f);
    [SerializeField] private float minCoinValue = 10f;

    private double coinValue;
    private double pendingCoinValue;
    private int buyAmount = 0;
    private int sellAmount = 0;
    private int ownedCoins = 0;
    private Coroutine fluctuationCoroutine;

    // ─────────────────────────────────────────────────────

    private void OnEnable()
    {
        buyAmount = 0;
        sellAmount = 0;
        UpdateUI();
    }

    private void Start()
    {
        InitializeCoinValue();
        SetupUI();
        RegisterUIEvents();
    }

    private void InitializeCoinValue()
    {
        coinValue = Random.Range(100, 501);
        pendingCoinValue = coinValue;
    }

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

        coinValue = pendingCoinValue;
        double gain = sellAmount * coinValue;

        ownedCoins -= sellAmount;
        sellAmount = 0;
        MoneyManager.Instance.ChangeMoneys(gain);

        if (ownedCoins <= 0)
        {
            buyButton.interactable = true;
            sellButton.interactable = false;

            if (fluctuationCoroutine != null)
                StopCoroutine(fluctuationCoroutine);

            UpdateCoinTrend(normalSprite);
        }

        UpdateUI();
        AudioManager.Instance.PlayInteractSound(1);
    }

    // ─────────────────────────────────────────────────────
    // UI Helpers
    // ─────────────────────────────────────────────────────

    private void UpdateUI()
    {
        coinValueText.text = $"${Mathf.RoundToInt((float)coinValue)}";
        buyAmountText.text = buyAmount.ToString();
        sellAmountText.text = sellAmount.ToString();
        ownedCoinsText.text = ownedCoins.ToString();

        UIManager.Instance?.UpdateMoney(MoneyManager.Instance.GetMoneys(), true);
    }

    private void UpdateCoinTrend(Sprite sprite)
    {
        if (coinTrendImage == null || sprite == null) return;

        coinTrendImage.sprite = sprite;
        coinTrendImage.enabled = false; // force refresh
        coinTrendImage.enabled = true;
    }

    // ─────────────────────────────────────────────────────
    // Price Fluctuation Logic
    // ─────────────────────────────────────────────────────

    private IEnumerator CoinFluctuationLoop()
    {
        UpdateCoinTrend(normalSprite);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(fluctuationInterval.x, fluctuationInterval.y));

            float random = Random.value;
            float fluctuation = Random.Range(fluctuationPercentRange.x, fluctuationPercentRange.y);

            if (random < 0.8f)
            {
                pendingCoinValue = System.Math.Max(minCoinValue, pendingCoinValue * (1 - fluctuation));
                UpdateCoinTrend(downSprite);
            }

            else
            {
                pendingCoinValue *= (1 + fluctuation);
                UpdateCoinTrend(upSprite);
            }
        }
    }
}
