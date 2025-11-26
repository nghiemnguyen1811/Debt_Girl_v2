using DA_Assets.SVGMeshUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central loader: builds item database and applies saved data
/// to all game systems after scene is fully initialized.
/// </summary>
public class DataManager : SingletonMonobehaviour<DataManager>
{
    //────────────────────────────────────────────────────
    // Inspector Fields
    //────────────────────────────────────────────────────
    [Header("Reference to Item Database")]
    [SerializeField] private ItemDatabaseSO itemDatabaseSO;

    //────────────────────────────────────────────────────
    // Runtime Data
    //────────────────────────────────────────────────────
    private Dictionary<IngredientType, ItemDataSO> itemDatabase;
    private SaveData cachedSaveData;

    //────────────────────────────────────────────────────
    // Unity Lifecycle
    //────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        BuildItemDatabase();
    }

    //────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────

    /// <summary>
    /// Reads save data into memory without applying it.
    /// </summary>
    public void LoadCachedSaveData()
    {
        cachedSaveData = SaveManager.LoadGame();
    }

    /// <summary>
    /// Called by scene entry point to confirm scene initialization is finished.
    /// </summary>
    public void NotifySceneReady()
    {
        StartCoroutine(ApplySaveWhenReady());
    }

    //────────────────────────────────────────────────────
    // Save Import Logic
    //────────────────────────────────────────────────────

    /// <summary>
    /// Waits a frame, ensures cachedSaveData is loaded, then imports all systems.
    /// </summary>
    private IEnumerator ApplySaveWhenReady()
    {
        yield return null;

        if (cachedSaveData == null)
            cachedSaveData = SaveManager.LoadGame();

        ImportAllSaveData();
    }

    /// <summary>
    /// Sends save data to every manager that supports loading.
    /// </summary>
    private void ImportAllSaveData()
    {
        // Core managers
        MainMenu.Instance?.ImportSaveData(cachedSaveData);
        GameManager.Instance?.ImportSaveData(cachedSaveData);
        StatUpgradeManager.Instance?.ImportSaveData(cachedSaveData);
        PlayerControl.Instance?.stats.ImportSaveData(cachedSaveData);

        // Gameplay systems
        BakingManager.Instance?.ImportSaveData(cachedSaveData);
        CoinTradeManager.Instance?.ImportSaveData(cachedSaveData);
        MoodManager.Instance?.ImportSaveData(cachedSaveData);
        PostManager.Instance?.ImportSaveData(cachedSaveData);
        DecorationManager.Instance?.ImportSaveData(cachedSaveData);
        OutfitManager.Instance?.ImportSaveData(cachedSaveData);
        MoneyManager.Instance?.ImportSaveData(cachedSaveData);
        DailyQuestManager.Instance?.ImportSaveData(cachedSaveData);

        // Inventories
        FoodInventoryUI.Instance?.ImportSaveData(cachedSaveData.foodInventory, itemDatabase);
        CakeInventoryUI.Instance?.ImportSaveData(cachedSaveData.cakeInventory, itemDatabase);

        Debug.Log("[DataManager] Save imported AFTER scene fully initialized.");
    }

    //────────────────────────────────────────────────────
    // Private Helpers
    //────────────────────────────────────────────────────

    /// <summary>
    /// Builds dictionary IngredientType → ItemDataSO for fast lookups.
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
}
