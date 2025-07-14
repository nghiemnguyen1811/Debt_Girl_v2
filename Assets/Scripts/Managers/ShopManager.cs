using UnityEngine;
using System.Collections.Generic;

public class ShopManager : SingletonMonobehaviour<ShopManager>
{
    [Header("References")]
    [SerializeField] private Transform containerParent;

    [Header("Data & Prefab")]
    [SerializeField] private List<ItemData> itemList;
    [SerializeField] private ShopItemContainer shopItemPrefab;

    private readonly List<ShopItemContainer> spawnedContainers = new();

    // ─────────────────────────────────────────────────────
    // Mono
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        InitializeItemUI();
    }

    // ─────────────────────────────────────────────────────
    // UI Initialization
    // ─────────────────────────────────────────────────────

    private void InitializeItemUI()
    {
        ClearExistingItems();

        foreach (ItemData itemData in itemList)
        {
            var container = Instantiate(shopItemPrefab, containerParent);
            container.Configure(itemData);
            spawnedContainers.Add(container);
        }
    }

    private void ClearExistingItems()
    {
        foreach (var container in spawnedContainers)
        {
            if (container != null)
                Destroy(container.gameObject);
        }

        spawnedContainers.Clear();
    }

    // ─────────────────────────────────────────────────────
    // Lookup & Utility
    // ─────────────────────────────────────────────────────

    public ShopItemContainer GetContainerByItem(ItemData itemData)
    {
        return spawnedContainers.Find(c => c.name == itemData.name);
    }

    public List<ShopItemContainer> GetAllContainers()
    {
        return spawnedContainers;
    }

    public void RefreshShop()
    {
        InitializeItemUI();
    }
}
