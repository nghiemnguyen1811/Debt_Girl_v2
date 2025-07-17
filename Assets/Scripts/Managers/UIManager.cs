using UnityEngine;
using TMPro;
using DG.Tweening;

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
    [SerializeField] private GameObject inventoryPanel;

    [Header("UI Buttons")]
    [SerializeField] private GameObject payDebtButton;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private float floatingTextFadeDuration = 2f;

    private Tween[] moneyTweens = new Tween[2];
    private Tween debtTween;

    // ─────────────────────────────────────────────────────

    private void Start()
    {
        InitializeUI();
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
        TogglePostPanel(false);
        ToggleUpgradePanel(false);
        ToggleCoinTradePanel(false);
    }

    public void TogglePostPanel(bool show) => postPanel?.SetActive(show);

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

    public void ToggleInventoryPanel(bool show)
    {
        TogglePanel(inventoryPanel, show, () => Inventory.Instance.DeSelectItem());
    }

    public void TogglePayDebtButton(bool show) => payDebtButton?.SetActive(show);


    /// <summary>
    /// Generic panel toggler with optional callback when hiding.
    /// </summary>
    private void TogglePanel(GameObject panel, bool show, System.Action onHideCallback = null)
    {
        if (panel == null) return;

        panel.SetActive(show);
        if (!show)
            onHideCallback?.Invoke();
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
        tween = textMesh.transform
            .DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 0.8f)
            .SetEase(Ease.OutBack);
    }

    #endregion
}
