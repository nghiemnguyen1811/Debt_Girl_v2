using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : SingletonMonobehaviour<ShopManager>
{
    [Header("References")]
    [SerializeField] private Transform foodContainerParent;

    [Header("Data & Prefab")]
    [SerializeField] private List<ItemDataSO> shopItemList;
    [SerializeField] private ShopItemContainer shopItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button purchaseButton;

    private readonly List<ShopItemContainer> spawnedFoodContainers = new();
    private double tempTotalPrice;

    // ─────────────────────────────────────────────────────
    // Mono
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        InitializeFoodUI();
        SetupListeners();
    }

    private void SetupListeners()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(ApplyPurchase);
    }

    // ─────────────────────────────────────────────────────
    // UI Initialization
    // ─────────────────────────────────────────────────────

    private bool IsFoodItem(ItemDataSO item)
    {
        return item.itemType == ItemType.Material || item.itemType == ItemType.Consumable;
    }

    private void InitializeFoodUI()
    {
        ClearExistingContainers();

        var filteredItems = shopItemList.Where(IsFoodItem);

        foreach (var item in filteredItems)
        {
            var container = Instantiate(shopItemPrefab, foodContainerParent);
            container.Configure(item);
            spawnedFoodContainers.Add(container);
        }

        UpdateAllUI();
    }

    private void ClearExistingContainers()
    {
        foreach (var container in spawnedFoodContainers)
        {
            if (container != null)
                Destroy(container.gameObject);
        }

        spawnedFoodContainers.Clear();
    }

    public void UpdateAllUI()
    {
        UpdateAllItemButtons();
        UpdatePurchaseButtonState();
        UpdateTotalPriceUI();
    }

    private void UpdateAllItemButtons()
    {
        foreach (var container in spawnedFoodContainers)
            container.UpdateButtonStates();
    }

    private void UpdatePurchaseButtonState()
    {
        if (purchaseButton == null) return;

        bool hasItemToBuy = false;

        foreach (var container in spawnedFoodContainers)
        {
            if (container.GetCurrentQuantity() > 0)
            {
                hasItemToBuy = true;
                break;
            }
        }

        purchaseButton.interactable = hasItemToBuy;
    }

    private void UpdateTotalPriceUI()
    {
        UIManager.Instance?.UpdateTotalPriceUI(tempTotalPrice);
    }

    // ─────────────────────────────────────────────────────
    // Purchase Logic
    // ─────────────────────────────────────────────────────

    public void ApplyPurchase()
    {
        foreach (var container in spawnedFoodContainers)
        {
            ItemDataSO itemData = container.GetItemData();
            int quantity = container.GetCurrentQuantity();

            Inventory.Instance.AddItem(itemData, quantity);
            container.ConfirmPurchase();
        }

        DeductTotalPrice();
        UpdateAllUI();
    }

    public void ResetAllSelections()
    {
        foreach (var container in spawnedFoodContainers)
            container.ResetSelection();

        ResetTempTotalPrice();
        UpdateAllUI();
    }

    private void DeductTotalPrice()
    {
        MoneyManager.Instance.ChangeMoneys(-tempTotalPrice);
        tempTotalPrice = 0;

        AudioManager.Instance.PlayInteractSound(1);
    }

    private void ResetTempTotalPrice()
    {
        tempTotalPrice = 0;
    }

    // ─────────────────────────────────────────────────────
    // Temporary Price Control
    // ─────────────────────────────────────────────────────

    public bool TryAddToTempPrice(double price)
    {
        if (MoneyManager.Instance.HasEnoughMoney(tempTotalPrice + price))
        {
            tempTotalPrice += price;
            UpdateTotalPriceUI();
            return true;
        }

        return false;
    }

    public void RefundFromTempPrice(double price)
    {
        if (tempTotalPrice > 0)
        {
            tempTotalPrice -= price;
            UpdateTotalPriceUI();
        }
    }

    public bool HasSufficientFunds(double price)
    {
        return MoneyManager.Instance.GetMoneys() - tempTotalPrice > price;
    }

    // ─────────────────────────────────────────────────────
    // Lookup & Utility
    // ─────────────────────────────────────────────────────

    public ShopItemContainer GetContainerByItem(ItemDataSO itemData)
    {
        return spawnedFoodContainers.Find(c => c.name == itemData.name);
    }

    public List<ShopItemContainer> GetAllContainers() => spawnedFoodContainers;

    public void RefreshShop()
    {
        InitializeFoodUI();
    }

    public void ForceUIRefresh()
    {
        UpdateAllUI();
    }
}
