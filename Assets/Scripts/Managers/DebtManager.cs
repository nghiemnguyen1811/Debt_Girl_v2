using UnityEngine;
using System;

/// <summary>
/// Manages the player's debt system: tracking, increasing, and allowing payment based on current funds.
/// </summary>
public class DebtManager : SingletonMonobehaviour<DebtManager>
{
    #region === Configurable Settings ===

    [Header("Debt Settings")]
    [SerializeField] private double initialDebt = 100;
    [SerializeField] private float debtMultiplier = 1.5f;

    private double currentDebt;

    #endregion

    #region === Unity Events ===

    // Recalculate debt on game start based on current level
    private void Start()
    {
        RecalculateDebtFromLevel();
    }

    #endregion

    #region === Public Methods ===

    /// <summary>
    /// Attempt to pay current debt. If successful, increase debt for next level.
    /// </summary>
    public void TryPayDebt()
    {
        if (MoneyManager.Instance.HasEnoughMoney(currentDebt))
        {
            MoneyManager.Instance.ChangeMoneys(-currentDebt);
            StatUpgradeManager.Instance.AddStatPoint();
            IncreaseDebt();
            AudioManager.Instance.PlayInteractSound(1);
        }

        else Debug.Log("Not enough coins to pay the debt!");
    }

    /// <summary>
    /// Toggle the visibility of the pay debt button based on available money.
    /// </summary>
    public void RefreshPayButton()
    {
        UIManager.Instance?.TogglePayDebtButton(MoneyManager.Instance.HasEnoughMoney(currentDebt));
    }

    /// <summary>
    /// Recalculate the debt amount based on the current level.
    /// </summary>
    public void RecalculateDebtFromLevel()
    {
        int level = GameManager.Instance.CurrentLevel;
        currentDebt = Math.Round(initialDebt * Math.Pow(debtMultiplier, level - 1), 2);
        UpdateDebtUI();
    }

    /// <summary>
    /// Return the current debt amount.
    /// </summary>
    public double GetCurrentDebt() => currentDebt;

    #endregion

    #region === Private Methods ===

    /// <summary>
    /// Increase debt after successful payment and update UI.
    /// </summary>
    private void IncreaseDebt()
    {
        GameManager.Instance.IncreaseLevel();
        int level = GameManager.Instance.CurrentLevel;

        currentDebt = Math.Round(initialDebt * Math.Pow(debtMultiplier, level - 1), 2);
        Debug.Log($"Debt increased to {currentDebt}, level: {level}");

        UpdateDebtUI();
    }

    /// <summary>
    /// Update the debt UI and button interactability.
    /// </summary>
    private void UpdateDebtUI()
    {
        UIManager.Instance?.UpdateDebt(currentDebt);
        RefreshPayButton();
    }

    #endregion
}
