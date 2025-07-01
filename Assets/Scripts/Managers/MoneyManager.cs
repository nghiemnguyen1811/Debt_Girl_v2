using UnityEngine;
using TMPro;

public class MoneyManager : SingletonMonobehaviour<MoneyManager>
{
    [Header(" Spawn Particle ")]
    [SerializeField] private Transform moneyVFXPoint;

    [Header("Coin Settings")]
    private double totalMoneys = 0;

    [Header("Particle Effect")]
    [SerializeField] private GameObject moneyParticlePrefab;

    // ======================== Unity Methods ========================
    private void Start()
    {
        SetMoneys(0);
    }

    // ======================== Public Methods ========================
    public void ChangeMoneys(double amount)
    {
        totalMoneys += amount;
        UpdateMoneyUI();

        // Chỉ spawn particle khi cộng tiền và prefab có gán
        if (moneyParticlePrefab != null && moneyVFXPoint != null)
        {
            MoneyParticle moneyParticle = (MoneyParticle)PoolManager.Instance
                .ReuseComponent(moneyParticlePrefab, moneyVFXPoint.position, Quaternion.identity);

            moneyParticle.Configure(amount);
            moneyParticle.gameObject.SetActive(true);
        }

        DebtManager.Instance.RefreshPayButton();
    }

    public void SetMoneys(double value)
    {
        totalMoneys = value;
        UpdateMoneyUI(immediate: true);
    }

    public double GetMoneys()
    {
        return totalMoneys;
    }

    public bool HasEnoughMoney(double amount)
    {
        return totalMoneys >= amount;
    }

    // ======================== Private Methods ========================
    private void UpdateMoneyUI(bool immediate = false)
    {
        UIManager.Instance?.UpdateMoney(totalMoneys, !immediate);
    }
}
