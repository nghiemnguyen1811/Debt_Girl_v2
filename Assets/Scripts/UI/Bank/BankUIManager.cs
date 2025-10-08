using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BankUIManager : MonoBehaviour
{
    [Header("Toggle Button")]
    [SerializeField] private Button toggleTabButton;

    [Header("UI Groups")]
    [SerializeField] private GameObject[] balanceGroup; // All UI elements related to Balance tab
    [SerializeField] private GameObject[] debtGroup;    // All UI elements related to Debt tab

    [Header("Amount Texts")]
    [SerializeField] private TMP_Text balanceAmountText;
    [SerializeField] private TMP_Text debtAmountText;

    [Header("Pay Debt Buttons")]
    [SerializeField] private GameObject payDebtButtonEnabled;   // Active (clickable) button
    [SerializeField] private GameObject payDebtButtonDisabled;  // Grayed-out (non-clickable) button

    private bool showingBalance = true;

    private void OnEnable()
    {
        toggleTabButton.onClick.AddListener(ToggleTab);

        // Subscribe to events
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;

        if (DebtManager.Instance != null)
            DebtManager.Instance.OnDebtChanged += OnDebtChanged;

        RefreshUI(); // Start on Balance tab
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

    /// <summary>
    /// Toggle between Balance and Debt tabs
    /// </summary>
    private void ToggleTab()
    {
        showingBalance = !showingBalance;
        RefreshUI();
    }

    /// <summary>
    /// Triggered when money changes, update balance text or pay button
    /// </summary>
    private void OnMoneyChanged(double newAmount)
    {
        if (showingBalance)
        {
            balanceAmountText.text = $"{newAmount:0.##}K";
        }
        else
        {
            UpdateDebtButtonUI();
        }
    }

    /// <summary>
    /// Triggered when debt changes, update debt text and pay button
    /// </summary>
    private void OnDebtChanged(double newDebt)
    {
        if (!showingBalance)
        {
            debtAmountText.text = $"{newDebt:0.##}K";
            UpdateDebtButtonUI();
        }
    }

    /// <summary>
    /// Refresh UI visibility and values based on current tab
    /// </summary>
    public void RefreshUI()
    {
        // Enable/Disable groups
        foreach (GameObject go in balanceGroup)
            go.SetActive(showingBalance);

        foreach (GameObject go in debtGroup)
            go.SetActive(!showingBalance);

        // Update balance and debt text using idle notation
        balanceAmountText.text = DoubleUtilities.ToIdleNotation(MoneyManager.Instance.GetMoneys());
        debtAmountText.text = DoubleUtilities.ToIdleNotation(DebtManager.Instance.GetCurrentDebt());

        UpdateDebtButtonUI();
    }

    /// <summary>
    /// Show correct Pay Debt button based on current money vs debt
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

    /// <summary>
    /// Called when Pay Debt button is clicked
    /// </summary>
    public void OnClickPayDebt()
    {
        DebtManager.Instance.TryPayDebt();

        // After paying, hide both buttons and refresh UI
        payDebtButtonEnabled.SetActive(false);
        payDebtButtonDisabled.SetActive(false);

        RefreshUI();
    }
}
