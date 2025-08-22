using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all UI updates and panel visibility.
/// </summary>
public class UIManager : SingletonMonobehaviour<UIManager>
{
    [Header("Money Texts (0 = HUD, 1 = Coin UI)")]
    [SerializeField] private TextMeshProUGUI[] moneyText;

    [Header("Other UI Texts")]
    [SerializeField] private TextMeshProUGUI debtText;
    [SerializeField] private TextMeshProUGUI statPointText;
    [SerializeField] private TextMeshProUGUI totalPriceText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("UI Panels")]
    [SerializeField] private GameObject postPanel;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject coinTradePanel;
    [SerializeField] private GameObject shoppingPanel;
    [SerializeField] private GameObject foodInventoryPanel;
    [SerializeField] private GameObject cakeInventoryPanel;
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private GameObject bakingPanel;
    [SerializeField] private GameObject selectRoomPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("UI Buttons")]
    [SerializeField] private GameObject payDebtButton;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private float floatingTextFadeDuration = 2f;

    private Tween[] moneyTweens = new Tween[2];
    private Tween debtTween;

    private bool hasInitialized = false; // Chỉ phát âm sau khi Start()

    // ─────────────────────────────────────────────────────

    private void Start()
    {
        InitializeUI();
        hasInitialized = true;
    }

    private void InitializeUI()
    {
        HideAllPanels();
    }

    // ─────────────────────────────────────────────────────

    #region UI Updates

    public void UpdateMoney(double totalCoins, bool animate = true)
    {
        for (int i = 0; i < moneyText.Length; i++)
        {
            if (moneyText[i] != null)
                UpdateText(moneyText[i], totalCoins, animate, ref moneyTweens[i]);
        }
    }

    public void UpdateDebt(double debtAmount, bool animate = true)
    {
        UpdateText(debtText, debtAmount, animate, ref debtTween);
    }

    public void UpdateStatPoints(int total)
    {
        if (statPointText != null)
            statPointText.text = $"Stat Points: {total}";
    }

    public void UpdateTotalPriceUI(double total)
    {
        if (totalPriceText != null)
            totalPriceText.text = DoubleUtilities.ToIdleNotation(total) + "$";
    }

    #endregion

    // ─────────────────────────────────────────────────────

    #region Panel Controls

    private void HideAllPanels()
    {
        TogglePausePanelFromButton(false);
        TogglePostPanel(false);
        ToggleUpgradePanel(false);
        ToggleCoinTradePanel(false);
        ToggleBakingPanel(false);
        ToggleShoppingPanel(false);
        ToggleCookingPanel(false);
        ToggleFoodInventoryPanel(false);
        ToggleCakeInventoryPanel(false);
        ToggleSelectRoomPanel(false);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public void TogglePausePanelFromButton(bool show) => TogglePausePanel(show);

    public void TogglePausePanel(bool show, bool isPausedBySettings = false)
    {
        bool shouldPauseGame = show || isPausedBySettings;
        Time.timeScale = shouldPauseGame ? 0 : 1;

        pausePanel.SetActive(show);

        if (hasInitialized)
            AudioManager.Instance.PlayInteractSound(8);
    }

    public void TogglePostPanel(bool show)
    {
        postPanel?.SetActive(show);
        if (hasInitialized)
            AudioManager.Instance.PlayInteractSound(8);
    }

    public void ToggleUpgradePanel(bool show)
    {
        TogglePanel(upgradePanel, show, () => StatUpgradeManager.Instance.ResetAll());
    }

    public void ToggleCoinTradePanel(bool show)
    {
        TogglePanel(coinTradePanel, show, () => CoinTradeManager.Instance.ResetAll());
    }

    public void ToggleShoppingPanel(bool show)
    {
        TogglePanel(shoppingPanel, show, () => ShopManager.Instance.ResetAllSelections());
    }

    public void ToggleFoodInventoryPanel(bool show)
    {
        TogglePanel(foodInventoryPanel, show, () => FoodInventoryUI.Instance.DeSelectItem(), false);
    }

    public void ToggleCakeInventoryPanel(bool show)
    {
        TogglePanel(cakeInventoryPanel, show, () => CakeInventoryUI.Instance.DeSelectItem(), false);
    }

    public void ToggleBakingPanel(bool show)
    {
        TogglePanelShow(bakingPanel, show, () => BakingManager.Instance.SelectCurrentCake());
    }

    public void ToggleCookingPanel(bool show)
    {
        TogglePanelShow(cookingPanel, show, () => CookingManager.Instance.RefreshAllCookingContainers());
    }

    public void ToggleSettingsPanel(bool show)
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(show);

        TogglePausePanel(!show, show);

        if (hasInitialized)
            AudioManager.Instance.PlayInteractSound(8);
    }

    public void ToggleSelectRoomPanel(bool show)
    {
        if (selectRoomPanel == null) return;

        selectRoomPanel.SetActive(show);

        if (hasInitialized)
            AudioManager.Instance.PlayInteractSound(8);
    }

    public void TogglePayDebtButton(bool show) => payDebtButton?.SetActive(show);

    /// <summary>
    /// Generic panel toggler with optional callback when hiding.
    /// </summary>
    private void TogglePanel(GameObject panel, bool show, System.Action onHideCallback = null, bool playSound = true)
    {
        if (panel == null) return;

        panel.SetActive(show);

        if (!show)
            onHideCallback?.Invoke();

        if ((playSound || (!show && !playSound)) && hasInitialized)
            AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Toggle a panel and optionally run a callback when the panel is shown.
    /// </summary>
    private void TogglePanelShow(GameObject panel, bool show, System.Action onShowCallback = null)
    {
        if (panel == null) return;

        panel.SetActive(show);

        if (show)
            onShowCallback?.Invoke();

        if (hasInitialized)
            AudioManager.Instance.PlayInteractSound(8);
    }

    #endregion

    // ─────────────────────────────────────────────────────

    #region Warning Text

    public void ShowWarningText(string message)
    {
        if (warningText == null) return;

        warningText.text = message;
        warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1f);
        warningText.transform.localScale = Vector3.one * 1.2f;

        DOTween.Kill(warningText);
        warningText.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        warningText.DOFade(0f, floatingTextFadeDuration).SetEase(Ease.InOutQuad);
    }

    #endregion

    // ─────────────────────────────────────────────────────

    #region Internal Helpers

    private void UpdateText(TextMeshProUGUI textMesh, double value, bool animate, ref Tween tween)
    {
        if (textMesh == null) return;

        textMesh.text = DoubleUtilities.ToIdleNotation(value);

        if (!animate) return;

        tween?.Kill();
        textMesh.transform.localScale = Vector3.one;

        tween = textMesh.transform
            .DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 0.8f)
            .SetEase(Ease.OutBack);
    }

    #endregion
}
