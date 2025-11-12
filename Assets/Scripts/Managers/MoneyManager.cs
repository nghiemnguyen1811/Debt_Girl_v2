 using UnityEngine;

/// <summary>
/// Handles player money and diamond balance, UI updates, and saving.
/// </summary>
public class MoneyManager : SingletonMonobehaviour<MoneyManager>
{
    // ==================================================
    // ▶ INSPECTOR FIELDS
    // ==================================================
    [Header("Spawn Particle")]
    [SerializeField] private Transform moneyVFXPoint;

    [Header("Particle Effect")]
    [SerializeField] private GameObject moneyParticlePrefab;

    [Header("Coin Settings")]
    private double totalMoneys = 0;
    private double totalDiamonds = 0;

    // ==================================================
    // ▶ UNITY EVENTS
    // ==================================================
    private void Update()
    {
        // Debug key for adding money quickly
        if (Input.GetKeyDown(KeyCode.M))
        {
            ChangeMoneys(100000);
            ChangeDiamonds(10);
        }
    }


    // ==================================================
    // ▶ PUBLIC MONEY METHODS
    // ==================================================
    /// <summary>
    /// Adds or subtracts money and spawns a visual particle if applicable.
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
    /// Sets money to a specific value and updates UI immediately.
    /// </summary>
    public void SetMoneys(double value)
    {
        totalMoneys = value;
        UpdateMoneyUI(immediate: true);
        AutoSave();
    }

    /// <summary>Returns the current total money.</summary>
    public double GetMoneys() => totalMoneys;

    /// <summary>Returns true if the player has enough money.</summary>
    public bool HasEnoughMoney(double amount) => totalMoneys >= amount;


    // ==================================================
    // ▶ PUBLIC DIAMOND METHODS
    // ==================================================
    /// <summary>Adds or subtracts diamonds and updates UI.</summary>
    public void ChangeDiamonds(double amount)
    {
        totalDiamonds += amount;
        UpdateDiamondUI();

        OutfitManager.Instance.RefreshUnlockButtons();

        AutoSave();
    }

    /// <summary>Sets diamond to a specific value and updates UI immediately.</summary>
    public void SetDiamonds(double value)
    {
        totalDiamonds = value;
        UpdateDiamondUI(immediate: true);
        AutoSave();
    }

    /// <summary>Returns the current total diamonds.</summary>
    public double GetDiamonds() => totalDiamonds;

    /// <summary>Returns true if the player has enough diamonds.</summary>
    public bool HasEnoughDiamond(double amount) => totalDiamonds >= amount;


    // ==================================================
    // ▶ PRIVATE UI HELPERS
    // ==================================================
    /// <summary>Updates the money display in UI.</summary>
    private void UpdateMoneyUI(bool immediate = false)
    {
        UIManager.Instance?.UpdateMoney(totalMoneys, !immediate);
    }

    /// <summary>Updates the diamond display in UI.</summary>
    private void UpdateDiamondUI(bool immediate = false)
    {
        UIManager.Instance?.UpdateDiamond(totalDiamonds, !immediate);
    }


    // ==================================================
    // ▶ SAVE / LOAD API
    // ==================================================
    /// <summary>Automatically saves current money and diamond data.</summary>
    protected void AutoSave()
    {
        SaveManager.Data.playerMoney = totalMoneys;
        SaveManager.Data.playerDiamond = totalDiamonds;
        SaveManager.SaveGame();
    }

    /// <summary>Loads money and diamond data from save.</summary>
    public void ImportSaveData(SaveData saveData)
    {
        SetMoneys(saveData.playerMoney);
        SetDiamonds(saveData.playerDiamond);
        BankManager.Instance.RefreshPayButton();
        OutfitManager.Instance.RefreshUnlockButtons();
    }
}
