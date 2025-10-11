using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        cachedSaveData = SaveManager.LoadGame();
    }

    private void OnEnable()
    {
        // Subscribe to ready events
        PlayerStats.OnStatsReadyForLoad += HandlePlayerStatsReady;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        PlayerStats.OnStatsReadyForLoad -= HandlePlayerStatsReady;
    }

    private void Start()
    {
        StartCoroutine(ReloadSaveData());
    }

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Reloads SaveData from SaveManager and re-imports data to managers.
    /// </summary>
    private IEnumerator ReloadSaveData()
    {
        yield return null;

        if (cachedSaveData == null) yield break;

        // Load core data
        GameManager.Instance?.ImportSaveData(cachedSaveData);
        MoneyManager.Instance?.ImportSaveData(cachedSaveData);
        StatUpgradeManager.Instance?.ImportSaveData(cachedSaveData);
        BakingManager.Instance?.ImportSaveData(cachedSaveData);
        StatUpgradeManager.Instance.ImportSaveData(cachedSaveData);
        FoodInventoryUI.Instance?.ImportSaveData(cachedSaveData.foodInventory, itemDatabase);
        CakeInventoryUI.Instance?.ImportSaveData(cachedSaveData.cakeInventory, itemDatabase);

        Debug.Log("[DataManager] All game systems imported after delayed reload.");
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

    private void HandlePlayerStatsReady(PlayerStats stats)
    {
        if (cachedSaveData == null) return;

        stats.ImportSaveData(cachedSaveData);
        Debug.Log("[DataManager] PlayerStats save data imported.");
    }
}
