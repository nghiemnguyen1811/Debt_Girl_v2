using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all UI updates and panel visibility.
/// </summary>
public class UIManager : SingletonMonobehaviour<UIManager>
{
    [Header("Texts (0 = HUD, 1 = Bank UI)")]
    [SerializeField] private TextMeshProUGUI[] moneyText;
    [SerializeField] private TextMeshProUGUI[] debtText;
    [SerializeField] private TextMeshProUGUI[] diamondText;

    [Header("Other UI Texts")]
    [SerializeField] private TextMeshProUGUI statPointText;
    [SerializeField] private TextMeshProUGUI totalPriceText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("UI Panels")]
    [SerializeField] private GameObject phonePanel;
    [SerializeField] private GameObject postPanel;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject coinTradePanel;
    [SerializeField] private GameObject shoppingPanel;
    [SerializeField] private GameObject foodInventoryPanel;
    [SerializeField] private GameObject cakeInventoryPanel;
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private GameObject bakingPanel;
    [SerializeField] private GameObject selectRoomPanel;
    [SerializeField] private GameObject bankingPanel;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject dailyquestPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject exitPanel;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private float floatingTextFadeDuration = 2f;

    private Tween[] moneyTweens = new Tween[3];
    private Tween[] debtTweens = new Tween[1];
    private Tween[] diamondTweens = new Tween[2];

    private bool hasInitialized = false;

    // ─────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Start()
    {
        HideAllPanels();
        hasInitialized = true;
    }

    #endregion

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
        for (int i = 0; i < debtText.Length; i++)
        {
            if (debtText[i] != null)
                UpdateText(debtText[i], debtAmount, animate, ref debtTweens[i]);
        }
    }

    public void UpdateDiamond(double diamondAmount, bool animate = true)
    {
        for (int i = 0; i < diamondText.Length; i++)
        {
            if (diamondText[i] != null)
                UpdateText(diamondText[i], diamondAmount, animate, ref diamondTweens[i]);
        }
    }

    public void UpdateStatPoints(int total)
    {
        if (statPointText != null)
            statPointText.text = $"{total}";
    }

    public void UpdateTotalPriceUI(double total)
    {
        if (totalPriceText != null)
            totalPriceText.text = DoubleUtilities.ToIdleNotation(total) + "원";
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region Panel Controls

    /// <summary>
    /// Centralized panel toggle handler.
    /// </summary>
    public void TogglePanelByType(PanelType type, bool show)
    {
        GameObject panel = null;
        System.Action callback = null;
        bool callbackOnShow = false;

        switch (type)
        {
            case PanelType.Phone: panel = phonePanel; break;
            case PanelType.Post: panel = postPanel; break;
            case PanelType.Banking: panel = bankingPanel; break;
            case PanelType.Dialogue: panel = dialoguePanel; break;
            case PanelType.DailyQuest: panel = dailyquestPanel; break;
            case PanelType.Exit: panel = exitPanel; break;

            case PanelType.Upgrade:
                panel = upgradePanel;
                callback = () => StatUpgradeManager.Instance.ResetAll();
                break;

            case PanelType.CoinTrade:
                panel = coinTradePanel;
                callback = () => CoinTradeManager.Instance.ResetAll();
                break;

            case PanelType.Shopping:
                panel = shoppingPanel;
                callback = () => ShopManager.Instance.ResetAllSelections();
                break;

            case PanelType.FoodInventory:
                panel = foodInventoryPanel;
                callback = () => FoodInventoryUI.Instance.DeSelectItem();
                break;

            case PanelType.CakeInventory:
                panel = cakeInventoryPanel;
                callback = () => CakeInventoryUI.Instance.DeSelectItem();
                break;

            case PanelType.Baking:
                panel = bakingPanel;
                callback = () => BakingManager.Instance.SelectCurrentCake();
                callbackOnShow = true;
                break;

            case PanelType.Cooking:
                panel = cookingPanel;
                callback = () => CookingManager.Instance.SelectCurrentDish();
                callbackOnShow = true;
                break;

            case PanelType.SelectRoom: panel = selectRoomPanel; break;

            case PanelType.Settings:
                panel = settingsPanel;
                TogglePausePanel(!show, show);
                break;

            case PanelType.Pause:
                TogglePausePanel(show);
                return;
        }

        if (panel == null) return;

        panel.SetActive(show);

        if (type == PanelType.CoinTrade || type == PanelType.Upgrade ||
            type == PanelType.Shopping || type == PanelType.SelectRoom ||
            type == PanelType.Post || type == PanelType.Banking)
        {
            phonePanel.SetActive(!show);
        }

        if (show && callbackOnShow)
            callback?.Invoke();

        else if (!show && !callbackOnShow)
            callback?.Invoke();

        if (hasInitialized && type != PanelType.Phone)
            AudioManager.Instance.PlayInteractSound(8);
    }


    // Wrappers for Inspector (Unity Buttons only accept basic parameter types)
    public void TogglePhonePanel(bool show) => TogglePanelByType(PanelType.Phone, show);
    public void TogglePostPanel(bool show) => TogglePanelByType(PanelType.Post, show);
    public void ToggleExitPanel(bool show) => TogglePanelByType(PanelType.Exit, show);
    public void ToggleUpgradePanel(bool show) => TogglePanelByType(PanelType.Upgrade, show);
    public void ToggleCoinTradePanel(bool show) => TogglePanelByType(PanelType.CoinTrade, show);
    public void ToggleShoppingPanel(bool show) => TogglePanelByType(PanelType.Shopping, show);
    public void ToggleFoodInventoryPanel(bool show) => TogglePanelByType(PanelType.FoodInventory, show);
    public void ToggleCakeInventoryPanel(bool show) => TogglePanelByType(PanelType.CakeInventory, show);
    public void ToggleBakingPanel(bool show) => TogglePanelByType(PanelType.Baking, show);
    public void ToggleCookingPanel(bool show) => TogglePanelByType(PanelType.Cooking, show);
    public void ToggleSelectRoomPanel(bool show) => TogglePanelByType(PanelType.SelectRoom, show);
    public void ToggleBankingPanel(bool show) => TogglePanelByType(PanelType.Banking, show);
    public void ToggleDialoguePanel(bool show) => TogglePanelByType(PanelType.Dialogue, show);
    public void ToggleDailyQuestPanel(bool show) => TogglePanelByType(PanelType.DailyQuest, show);
    public void ToggleSettingsPanel(bool show) => TogglePanelByType(PanelType.Settings, show);
    public void TogglePausePanelFromButton(bool show) => TogglePanelByType(PanelType.Pause, show);

    /// <summary>
    /// Hide all panels on initialization.
    /// </summary>
    private void HideAllPanels()
    {
        TogglePausePanel(false);
        TogglePostPanel(false);
        ToggleUpgradePanel(false);
        ToggleCoinTradePanel(false);
        ToggleBakingPanel(false);
        ToggleShoppingPanel(false);
        ToggleCookingPanel(false);
        ToggleFoodInventoryPanel(false);
        ToggleCakeInventoryPanel(false);
        ToggleSelectRoomPanel(false);
        ToggleBankingPanel(false);
        ToggleDialoguePanel(false);
        ToggleDailyQuestPanel(false);
        TogglePhonePanel(false);
    }

    /// <summary>
    /// Pause panel has special handling (timeScale).
    /// </summary>
    public void TogglePausePanel(bool show, bool isPausedBySettings = false)
    {
        bool shouldPauseGame = show || isPausedBySettings;
        Time.timeScale = shouldPauseGame ? 0 : 1;

        pausePanel.SetActive(show);

        if (hasInitialized)
            AudioManager.Instance.PlayInteractSound(8);
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region Scene / Game Controls

    public void ReturnToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
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
