using UnityEngine;
using System.Collections.Generic;

public class DataManager : SingletonMonobehaviour<DataManager>
{
    [Header("Reference to Item Database")]
    [SerializeField] private ItemDatabaseSO itemDatabaseSO;

    private Dictionary<IngredientType, ItemDataSO> itemDatabase;

    private void OnEnable()
    {
        InventoryBase<FoodInventoryUI>.OnInventoryReady += HandleInventoryReady;
        InventoryBase<CakeInventoryUI>.OnInventoryReady += HandleInventoryReady;
        StatUpgradeManager.OnStatManagerReady += HandleStatManagerReady;
        PlayerStats.OnStatsReadyForLoad += HandlePlayerStatsReady;
    }

    private void OnDisable()
    {
        InventoryBase<FoodInventoryUI>.OnInventoryReady -= HandleInventoryReady;
        InventoryBase<CakeInventoryUI>.OnInventoryReady -= HandleInventoryReady;
        StatUpgradeManager.OnStatManagerReady -= HandleStatManagerReady;
        PlayerStats.OnStatsReadyForLoad -= HandlePlayerStatsReady;
    }

    private void Start()
    {
        BuildItemDatabase();

        var saveData = SaveManager.LoadGame();
        if (saveData == null) return;

        GameManager.Instance?.ImportSaveData(saveData);
        MoneyManager.Instance?.ImportSaveData(saveData);
        BakingManager.Instance?.ImportSaveData(saveData);

        Debug.Log("[DataManager] GameManager & MoneyManager loaded.");
    }

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

    private void HandleInventoryReady(IInventoryBase inv)
    {
        var saveData = SaveManager.LoadGame();
        if (saveData == null) return;

        if (inv is FoodInventoryUI foodInv)
            foodInv.ImportSaveData(saveData.foodInventory, itemDatabase);

        if (inv is CakeInventoryUI cakeInv)
            cakeInv.ImportSaveData(saveData.cakeInventory, itemDatabase);

        Debug.Log("[DataManager] Inventory loaded.");
    }

    private void HandleStatManagerReady()
    {
        var saveData = SaveManager.LoadGame();
        if (saveData == null) return;

        StatUpgradeManager.Instance.ImportSaveData(saveData);
        Debug.Log("[DataManager] Stat points & stat levels loaded.");
    }

    private void HandlePlayerStatsReady(PlayerStats stats)
    {
        var saveData = SaveManager.LoadGame();
        if (saveData != null)
        {
            stats.ImportSaveData(saveData);
            Debug.Log("[DataManager] PlayerStats save data imported via OnStatsReadyForLoad.");
        }
    }
}
