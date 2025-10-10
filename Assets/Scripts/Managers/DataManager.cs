using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DataManager handles building item database and importing saved data 
/// for inventories, stats, player, and baking system.
/// </summary>
public class DataManager : SingletonMonobehaviour<DataManager>
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("Reference to Item Database")]
    [SerializeField] private ItemDatabaseSO itemDatabaseSO;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────
    private Dictionary<IngredientType, ItemDataSO> itemDatabase;
    private SaveData cachedSaveData;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        BuildItemDatabase();
    }

    private void OnEnable()
    {
        // Subscribe to ready events
        InventoryBase<FoodInventoryUI>.OnInventoryReady += HandleInventoryReady;
        InventoryBase<CakeInventoryUI>.OnInventoryReady += HandleInventoryReady;
        StatUpgradeManager.OnStatManagerReady += HandleStatManagerReady;
        PlayerStats.OnStatsReadyForLoad += HandlePlayerStatsReady;
        BakingManager.OnBakingManagerReady += HandleBakingManagerReady;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        InventoryBase<FoodInventoryUI>.OnInventoryReady -= HandleInventoryReady;
        InventoryBase<CakeInventoryUI>.OnInventoryReady -= HandleInventoryReady;
        StatUpgradeManager.OnStatManagerReady -= HandleStatManagerReady;
        PlayerStats.OnStatsReadyForLoad -= HandlePlayerStatsReady;
        BakingManager.OnBakingManagerReady -= HandleBakingManagerReady;
    }

    private void Start()
    {
        ReloadSaveData();
    }

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Reloads SaveData from SaveManager and re-imports data to managers.
    /// </summary>
    public void ReloadSaveData()
    {
        cachedSaveData = SaveManager.LoadGame();

        if (cachedSaveData == null)
        {
            Debug.LogWarning("[DataManager] No save data found.");
            return;
        }

        GameManager.Instance?.ImportSaveData(cachedSaveData);
        MoneyManager.Instance?.ImportSaveData(cachedSaveData);

        Debug.Log("[DataManager] Save data reloaded and applied.");
    }

    // ─────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Build dictionary mapping IngredientType -> ItemDataSO.
    /// </summary>
    private void BuildItemDatabase()
    {
        itemDatabase = new Dictionary<IngredientType, ItemDataSO>();

        foreach (var item in itemDatabaseSO.allItems)
        {
            if (item == null) continue;

            if (!itemDatabase.ContainsKey(item.ingredientType))
                itemDatabase.Add(item.ingredientType, item);

            else Debug.LogWarning($"[DataManager] Duplicate IngredientType: {item.ingredientType}");
        }

        Debug.Log($"[DataManager] Item database built with {itemDatabase.Count} entries");
    }

    // ─────────────────────────────────────────────────────
    // Event Handlers
    // ─────────────────────────────────────────────────────

    private void HandleInventoryReady(IInventoryBase inv)
    {
        if (cachedSaveData == null) return;

        switch (inv)
        {
            case FoodInventoryUI foodInv:
                foodInv.ImportSaveData(cachedSaveData.foodInventory, itemDatabase);
                break;

            case CakeInventoryUI cakeInv:
                cakeInv.ImportSaveData(cachedSaveData.cakeInventory, itemDatabase);
                break;
        }

        Debug.Log("[DataManager] Inventory loaded.");
    }

    private void HandleStatManagerReady()
    {
        if (cachedSaveData == null) return;

        StatUpgradeManager.Instance.ImportSaveData(cachedSaveData);
        Debug.Log("[DataManager] Stat points & stat levels loaded.");
    }

    private void HandlePlayerStatsReady(PlayerStats stats)
    {
        if (cachedSaveData == null) return;

        stats.ImportSaveData(cachedSaveData);
        Debug.Log("[DataManager] PlayerStats save data imported.");
    }

    private void HandleBakingManagerReady()
    {
        if (cachedSaveData == null) return;

        BakingManager.Instance?.ImportSaveData(cachedSaveData);
        Debug.Log("[DataManager] BakingManager data imported.");
    }
}
