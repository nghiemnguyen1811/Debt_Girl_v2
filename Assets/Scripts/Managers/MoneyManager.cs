using UnityEngine;

public class MoneyManager : SingletonMonobehaviour<MoneyManager>, ILoadable
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
    private void Start()
    {
        SetMoneys(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ChangeMoneys(100000);
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

        BankManager.Instance.RefreshPayButton();
        ShopManager.Instance.UpdateAllUI();

        AutoSave();
    }

    /// <summary>
    /// Set money to a specific value
    /// </summary>
    public void SetMoneys(double value)
    {
        totalMoneys = value;
        UpdateMoneyUI(immediate: true);

        AutoSave();
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

    // ─────────────────────────────────────────────────────
    // Save/Load API
    // ─────────────────────────────────────────────────────
    protected void AutoSave()
    {
        SaveManager.Data.playerMoney = totalMoneys;
        SaveManager.SaveGame();
    }

    public void ImportSaveData(SaveData saveData)
    {
        SetMoneys(saveData.playerMoney);
    }
}
