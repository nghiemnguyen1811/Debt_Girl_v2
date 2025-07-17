using UnityEngine;
using TMPro;

public class MoneyManager : SingletonMonobehaviour<MoneyManager>
{
    #region === Inspector Fields ===

    [Header("Spawn Particle")]
    [SerializeField] private Transform moneyVFXPoint;

    [Header("Coin Settings")]
    private double totalMoneys = 0;

    [Header("Particle Effect")]
    [SerializeField] private GameObject moneyParticlePrefab;

    #endregion

    #region === Unity Events ===

    // Initialize with 0 money at start
    private void Start()
    {
        SetMoneys(0);
    }

    #endregion

    #region === Public Methods ===

    /// <summary>
    /// Add or subtract money and spawn particle if applicable
    /// </summary>
    public void ChangeMoneys(double amount)
    {
        totalMoneys += amount;
        UpdateMoneyUI();

        // Only spawn particle when gaining money and prefab is assigned
        if (moneyParticlePrefab != null && moneyVFXPoint != null)
        {
            MoneyParticle moneyParticle = (MoneyParticle)PoolManager.Instance
                .ReuseComponent(moneyParticlePrefab, moneyVFXPoint.position, Quaternion.identity);

            moneyParticle.Configure(amount);
            moneyParticle.gameObject.SetActive(true);
        }

        DebtManager.Instance.RefreshPayButton();
        ShopManager.Instance.UpdateAllUI();
    }

    /// <summary>
    /// Set money to a specific value
    /// </summary>
    public void SetMoneys(double value)
    {
        totalMoneys = value;
        UpdateMoneyUI(immediate: true);
    }

    /// <summary>
    /// Get current total money
    /// </summary>
    public double GetMoneys()
    {
        return totalMoneys;
    }

    /// <summary>
    /// Check if the player has enough money
    /// </summary>
    public bool HasEnoughMoney(double amount)
    {
        return totalMoneys >= amount;
    }

    #endregion

    #region === Private Methods ===

    /// <summary>
    /// Update the UI money display
    /// </summary>
    private void UpdateMoneyUI(bool immediate = false)
    {
        UIManager.Instance?.UpdateMoney(totalMoneys, !immediate);
    }

    #endregion
}
