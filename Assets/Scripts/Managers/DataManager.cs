using DA_Assets.SVGMeshUnity;
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

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Public method to trigger reloading of save data.
    /// </summary>
    public void ReloadAllData()
    {
        StartCoroutine(ReloadSaveData());
    }

    /// <summary>
    /// Reloads SaveData from SaveManager and re-imports data to managers.
    /// </summary>
    private IEnumerator ReloadSaveData()
    {
        yield return new WaitForEndOfFrame();

        if (cachedSaveData == null) yield break;

        // Load core data
        MainMenu.Instance?.ImportSaveData(cachedSaveData);
        GameManager.Instance?.ImportSaveData(cachedSaveData);
        StatUpgradeManager.Instance?.ImportSaveData(cachedSaveData);
        PlayerControl.Instance?.stats.ImportSaveData(cachedSaveData);
        BakingManager.Instance?.ImportSaveData(cachedSaveData);
        StatUpgradeManager.Instance?.ImportSaveData(cachedSaveData);
        CoinTradeManager.Instance?.ImportSaveData(cachedSaveData);
        MoodManager.Instance?.ImportSaveData(cachedSaveData);
        PostManager.Instance?.ImportSaveData(cachedSaveData);
        DecorationManager.Instance?.ImportSaveData(cachedSaveData);
        OutfitManager.Instance?.ImportSaveData(cachedSaveData);
        MoneyManager.Instance?.ImportSaveData(cachedSaveData);
        DailyQuestManager.Instance?.ImportSaveData(cachedSaveData);
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
}
