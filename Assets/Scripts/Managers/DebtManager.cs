using UnityEngine;
using System;

public class DebtManager : SingletonMonobehaviour<DebtManager>
{
    [Header("Debt Settings")]
    [SerializeField] private double initialDebt = 100;
    [SerializeField] private float debtMultiplier = 1.5f;

    private double currentDebt;


    // ======================== Unity Methods ========================
    private void Start()
    {
        RecalculateDebtFromLevel();
    }


    // ======================== Public Methods ========================
    public void TryPayDebt()
    {
        if (MoneyManager.Instance.HasEnoughMoney(currentDebt))
        {
            MoneyManager.Instance.ChangeMoneys(-currentDebt);
            IncreaseDebt();

            AudioManager.Instance.PlayInteractSound(1);
        }

        else Debug.Log("Not enough coins to pay the debt!");
    }

    public void RefreshPayButton()
    {
        UIManager.Instance?.TogglePayDebtButton(MoneyManager.Instance.HasEnoughMoney(currentDebt));
    }

    public void RecalculateDebtFromLevel()
    {
        int level = GameManager.Instance.CurrentLevel;
        currentDebt = Math.Round(initialDebt * Math.Pow(debtMultiplier, level - 1), 2);
        UpdateDebtUI();
    }

    public double GetCurrentDebt() => currentDebt;


    // ======================== Private Methods ========================
    private void IncreaseDebt()
    {
        GameManager.Instance.IncreaseLevel();
        int level = GameManager.Instance.CurrentLevel;

        currentDebt = Math.Round(initialDebt * Math.Pow(debtMultiplier, level - 1), 2);
        Debug.Log($"Debt increased to {currentDebt}, level: {level}");

        UpdateDebtUI();
    }

    private void UpdateDebtUI()
    {
        UIManager.Instance?.UpdateDebt(currentDebt);
        RefreshPayButton();
    }
}
