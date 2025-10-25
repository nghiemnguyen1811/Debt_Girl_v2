using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player's debt system: tracking, increasing, and allowing payment based on current funds,
/// and also handles related Bank UI.
/// </summary>
public class BankManager : SingletonMonobehaviour<BankManager>
{
    public event Action OnDebtPaid;

    // ─────────────────────────────────────────────────────
    // Configurable Settings
    // ─────────────────────────────────────────────────────
    [Header("Debt Settings")]
    [SerializeField] private double initialDebt = 100;
    [SerializeField] private float debtMultiplier = 1.5f;

    [Header("Pay Debt Buttons")]
    [SerializeField] private Button payDebtButtonEnabled;
    [SerializeField] private Button payDebtButtonDisabled;

    [Header("Shop Tabs (Balance / Debt)")]
    [SerializeField] private List<Tab> bankTabs;

    [Header("UI Colors")]
    [SerializeField] private UIColorsConfig uiColorsConfig;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────
    private double currentDebt;
    private Tab currentActiveTab = null;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        SetupBankTabs();
        SetupListeners();

        // Default: activate Balance tab
        if (bankTabs.Count > 0)
            ActivateTab(bankTabs[0]);

        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged += RecalculateDebtFromLevel;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= RecalculateDebtFromLevel;
    }

    private void SetupListeners()
    {
        if (payDebtButtonEnabled != null)
            payDebtButtonEnabled.onClick.AddListener(TryPayDebt);
    }

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to pay the current debt if the player has enough money.
    /// </summary>
    public void TryPayDebt()
    {
        if (MoneyManager.Instance.HasEnoughMoney(currentDebt))
        {
            MoneyManager.Instance.ChangeMoneys(-currentDebt);
            StatUpgradeManager.Instance.AddStatPoint();
            IncreaseDebt();
            AudioManager.Instance.PlayInteractSound(1);

            OnDebtPaid?.Invoke();
        }

        else Debug.Log("Not enough coins to pay the debt!");
    }

    /// <summary>
    /// Refresh the Pay Debt button state based on available money.
    /// </summary>
    public void RefreshPayButton()
    {
        TogglePayDebtButton(MoneyManager.Instance.HasEnoughMoney(currentDebt));
    }

    /// <summary>
    /// Toggle the Pay Debt button (enabled/disabled) depending on condition.
    /// </summary>
    public void TogglePayDebtButton(bool canPay)
    {
        if (payDebtButtonEnabled != null && payDebtButtonDisabled != null)
        {
            payDebtButtonEnabled.gameObject.SetActive(canPay);
            payDebtButtonDisabled.gameObject.SetActive(!canPay);
        }
    }

    /// <summary>
    /// Recalculate debt amount based on current level progression.
    /// </summary>
    public void RecalculateDebtFromLevel()
    {
        int level = GameManager.Instance.CurrentLevel;
        currentDebt = Math.Round(initialDebt * Math.Pow(debtMultiplier, level - 1), 2);
        UpdateDebtUI();
    }

    // ─────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Setup all bank tabs with listeners and default visuals.
    /// </summary>
    private void SetupBankTabs()
    {
        foreach (var tab in bankTabs)
        {
            tab.button.onClick.AddListener(() => ActivateTab(tab));
            SetTabVisual(tab, false);
            if (tab.group != null) tab.group.SetActive(false);
        }
    }

    /// <summary>
    /// Activate a selected bank tab and deactivate the previous one.
    /// </summary>
    private void ActivateTab(Tab tab)
    {
        // Deselect old tab
        if (currentActiveTab != null)
        {
            SetTabVisual(currentActiveTab, false);
            if (currentActiveTab.group != null)
                currentActiveTab.group.SetActive(false);
        }

        // Select new tab
        currentActiveTab = tab;
        SetTabVisual(tab, true);

        if (tab.group != null)
            tab.group.SetActive(true);
    }

    /// <summary>
    /// Update the tab visual state (active/inactive).
    /// </summary>
    private void SetTabVisual(Tab tab, bool isActive)
    {
        if (tab.labelText != null)
            tab.labelText.color = isActive ? uiColorsConfig.tabOn : uiColorsConfig.tabOff;

        if (tab.outline != null)
            tab.outline.SetActive(isActive);
    }

    /// <summary>
    /// Increase the player's level and update the debt UI.
    /// </summary>
    private void IncreaseDebt()
    {
        GameManager.Instance.IncreaseLevel();
        UpdateDebtUI();
    }

    /// <summary>
    /// Update the debt UI elements and refresh pay button state.
    /// </summary>
    private void UpdateDebtUI()
    {
        UIManager.Instance?.UpdateDebt(currentDebt);
        RefreshPayButton();
    }
}
