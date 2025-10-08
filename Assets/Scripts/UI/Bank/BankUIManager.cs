using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BankUIManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Toggle Button")]
    [SerializeField] private Button toggleTabButton;

    [Header("UI Groups")]
    [SerializeField] private GameObject[] balanceGroup; // All UI elements for Balance tab
    [SerializeField] private GameObject[] debtGroup;    // All UI elements for Debt tab

    [Header("Amount Texts")]
    [SerializeField] private TMP_Text balanceAmountText;
    [SerializeField] private TMP_Text debtAmountText;

    [Header("Pay Debt Buttons")]
    [SerializeField] private GameObject payDebtButtonEnabled;   // Clickable pay button
    [SerializeField] private GameObject payDebtButtonDisabled;  // Grayed-out pay button

    #endregion

    // ─────────────────────────────────────────────────────
    // Runtime Variables
    // ─────────────────────────────────────────────────────
    #region === Runtime ===

    private bool showingBalance = true;

    #endregion

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    #region === Unity Events ===

    private void OnEnable()
    {
        toggleTabButton.onClick.AddListener(ToggleTab);

        // Subscribe to money and debt changes
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;

        if (DebtManager.Instance != null)
            DebtManager.Instance.OnDebtChanged += OnDebtChanged;

        RefreshUI(); // Set initial UI
    }

    private void OnDisable()
    {
        toggleTabButton.onClick.RemoveListener(ToggleTab);

        // Unsubscribe from events
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;

        if (DebtManager.Instance != null)
            DebtManager.Instance.OnDebtChanged -= OnDebtChanged;
    }

    #endregion

    // ─────────────────────────────────────────────────────
    // UI Logic
    // ─────────────────────────────────────────────────────
    #region === UI Logic ===

    /// <summary>
    /// Toggles between Balance and Debt tabs.
    /// </summary>
    private void ToggleTab()
    {
        showingBalance = !showingBalance;
        RefreshUI();
    }

    /// <summary>
    /// Triggered when money changes. Updates balance text or debt button.
    /// </summary>
    private void OnMoneyChanged(double newAmount)
    {
        if (showingBalance)
        {
            balanceAmountText.text = DoubleUtilities.ToIdleNotation(newAmount);
        }
        else
        {
            UpdateDebtButtonUI();
        }
    }

    /// <summary>
    /// Triggered when debt changes. Updates debt text and debt button.
    /// </summary>
    private void OnDebtChanged(double newDebt)
    {
        if (!showingBalance)
        {
            debtAmountText.text = DoubleUtilities.ToIdleNotation(newDebt);
            UpdateDebtButtonUI();
        }
    }

    /// <summary>
    /// Refreshes the UI based on the current tab selection.
    /// </summary>
    public void RefreshUI()
    {
        // Show/hide groups
        foreach (GameObject go in balanceGroup)
            go.SetActive(showingBalance);

        foreach (GameObject go in debtGroup)
            go.SetActive(!showingBalance);

        // Manually call update methods to sync UI
        OnMoneyChanged(MoneyManager.Instance.GetMoneys());
        OnDebtChanged(DebtManager.Instance.GetCurrentDebt());
    }

    /// <summary>
    /// Updates the "Pay Debt" button based on current money vs debt.
    /// </summary>
    private void UpdateDebtButtonUI()
    {
        if (!showingBalance)
        {
            double balance = MoneyManager.Instance.GetMoneys();
            double debt = DebtManager.Instance.GetCurrentDebt();

            bool canPay = balance >= debt;
            payDebtButtonEnabled.SetActive(canPay);
            payDebtButtonDisabled.SetActive(!canPay);
        }
        else
        {
            payDebtButtonEnabled.SetActive(false);
            payDebtButtonDisabled.SetActive(false);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────
    // Button Actions
    // ─────────────────────────────────────────────────────
    #region === Button Interaction ===

    /// <summary>
    /// Called when the "Pay Debt" button is clicked.
    /// </summary>
    public void OnClickPayDebt()
    {
        DebtManager.Instance.TryPayDebt();

        // Hide buttons and refresh UI
        payDebtButtonEnabled.SetActive(false);
        payDebtButtonDisabled.SetActive(false);

        RefreshUI();
    }

    #endregion
}
