using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class ShopManager : SingletonMonobehaviour<ShopManager>
{
    public event Action OnItemPurchased;

    [Header("Food References")]
    [SerializeField] private Transform foodContainerParent;
    [SerializeField] private List<ItemDataSO> foodItemList;
    [SerializeField] private FoodItemContainer foodItemPrefab;

    [Header("Decor References")]
    [SerializeField] private Transform decorContainerParent;
    [SerializeField] private List<DecorationItemSO> decorItemList;
    [SerializeField] private DecorationItemContainer decorItemPrefab;

    [Header("Character Tabs")]
    [SerializeField] private Transform characterTabParent;
    [SerializeField] private CharacterTabButton characterTabPrefab;
    [SerializeField] private List<CharacterInfoSO> characterTabList;

    [Header("Shop Tabs (Food / Decor)")]
    [SerializeField] private List<Tab> shopTabs;
    [SerializeField] private GameObject characterSelectionPanel;

    [SerializeField] private UIColorsConfig uiColorsConfig;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI totalPriceText;

    [Header("Buttons")]
    [SerializeField] private Button purchaseButtonEnabled;
    [SerializeField] private Button purchaseButtonDisabled;

    // Localized currency symbol (₩, $, ₫ ...)
    [SerializeField] private LocalizedString currencySymbol = new LocalizedString("Shop Labels", "shop.totalText");

    private readonly List<FoodItemContainer> spawnedFoodContainers = new();
    private readonly List<DecorationItemContainer> spawnedDecorContainers = new();
    private readonly List<CharacterTabButton> spawnedCharacterTabs = new();

    private CharacterTabButton currentSelectedTab = null;
    private Tab currentActiveTab = null;
    private double tempTotalPrice;

    // ─────────────────────────────────────────────────────
    private void OnEnable()
    {
        currencySymbol.StringChanged += UpdateTotalPriceUI;
    }

    private void OnDisable()
    {
        currencySymbol.StringChanged -= UpdateTotalPriceUI;
    }

    private void UpdateTotalPriceUI(string _)
    {
        UpdateTotalPriceUI();
    }

    private void Start()
    {
        InitializeFoodUI();
        InitializeDecorUI();
        InitializeCharacterTabs();
        SetupListeners();
        SetupShopTabs();

        // Default: activate Food tab
        if (shopTabs.Count > 0)
            ActivateTab(shopTabs[0]);
    }

    private void SetupListeners()
    {
        if (purchaseButtonEnabled != null)
            purchaseButtonEnabled.onClick.AddListener(ApplyPurchase);

        CharacterTabButton.OnTabSelected += HandleTabSelected;
    }

    private void OnDestroy()
    {
        CharacterTabButton.OnTabSelected -= HandleTabSelected;
    }

    // ─────────────────────────────────────────────────────
    // Character Tabs
    // ─────────────────────────────────────────────────────
    private void InitializeCharacterTabs()
    {
        foreach (var tabData in characterTabList)
        {
            var tab = Instantiate(characterTabPrefab, characterTabParent);
            tab.Configure(tabData.avatarIcon, tabData.characterType);
            tab.SetSelected(false);
            spawnedCharacterTabs.Add(tab);
        }
    }

    private void HandleTabSelected(CharacterType selectedType)
    {
        // Click again on the same tab → deselect and show all items
        if (currentSelectedTab != null && currentSelectedTab.CharacterType == selectedType)
        {
            currentSelectedTab.SetSelected(false);
            currentSelectedTab = null;
            ShowAllDecorItems();
            return;
        }

        // Deselect the old tab
        if (currentSelectedTab != null)
            currentSelectedTab.SetSelected(false);

        // Select the new tab
        currentSelectedTab = spawnedCharacterTabs.Find(t => t.CharacterType == selectedType);
        if (currentSelectedTab != null)
        {
            currentSelectedTab.SetSelected(true);
            FilterDecorByCharacter(selectedType);
        }
    }

    private void ShowAllDecorItems()
    {
        foreach (var item in spawnedDecorContainers)
            item.gameObject.SetActive(true);
    }

    private void FilterDecorByCharacter(CharacterType type)
    {
        foreach (var item in spawnedDecorContainers)
        {
            var data = item.GetItemData();
            item.gameObject.SetActive(data.owner == type);
        }
    }

    // ─────────────────────────────────────────────────────
    // Food UI
    // ─────────────────────────────────────────────────────
    private bool IsFoodItem(ItemDataSO item)
    {
        return item.itemType == ItemType.Material || item.itemType == ItemType.Consumable;
    }

    private void InitializeFoodUI()
    {
        ClearExistingFoodContainers();

        var filteredItems = foodItemList.Where(IsFoodItem);
        foreach (var item in filteredItems)
        {
            var container = Instantiate(foodItemPrefab, foodContainerParent);
            container.Configure(item);
            spawnedFoodContainers.Add(container);
        }
    }

    private void ClearExistingFoodContainers()
    {
        foreach (var container in spawnedFoodContainers)
        {
            if (container != null)
                Destroy(container.gameObject);
        }
        spawnedFoodContainers.Clear();
    }

    // ─────────────────────────────────────────────────────
    // Decor UI
    // ─────────────────────────────────────────────────────
    private void InitializeDecorUI()
    {
        ClearExistingDecorContainers();

        foreach (var item in decorItemList)
        {
            var container = Instantiate(decorItemPrefab, decorContainerParent);
            container.Configure(item);
            spawnedDecorContainers.Add(container);
        }
    }

    private void ClearExistingDecorContainers()
    {
        foreach (var container in spawnedDecorContainers)
        {
            if (container != null)
                Destroy(container.gameObject);
        }
        spawnedDecorContainers.Clear();
    }

    // ─────────────────────────────────────────────────────
    // Shop Tabs
    // ─────────────────────────────────────────────────────
    private void SetupShopTabs()
    {
        foreach (var tab in shopTabs)
        {
            tab.button.onClick.AddListener(() => ActivateTab(tab));
            SetTabVisual(tab, false);
            if (tab.group != null) tab.group.SetActive(false);
        }
    }

    private void ActivateTab(Tab tab)
    {
        // Deselect old tab
        if (currentActiveTab != null)
        {
            SetTabVisual(currentActiveTab, false);
            if (currentActiveTab.group != null)
                currentActiveTab.group.SetActive(false);
        }

        // Select new tab
        currentActiveTab = tab;
        SetTabVisual(tab, true);

        if (tab.group != null)
            tab.group.SetActive(true);

        // Only for Decoration and Fashion → enable CharacterSelection Panel
        if (tab.tabName == "Decoration" || tab.tabName == "Fashion")
            characterSelectionPanel.SetActive(true);

        else characterSelectionPanel.SetActive(false);
    }

    private void SetTabVisual(Tab tab, bool isActive)
    {
        if (tab.labelText != null)
            tab.labelText.color = isActive ?
                uiColorsConfig.tabOn : uiColorsConfig.tabOff;

        if (tab.outline != null)
            tab.outline.SetActive(isActive);
    }

    // ─────────────────────────────────────────────────────
    // UI Updates
    // ─────────────────────────────────────────────────────
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

    public void RefreshDecorOwnershipUI()
    {
        foreach (var container in spawnedDecorContainers)
        {
            if (container == null) continue;
            container.RefreshOwnershipUI();
        }

        Debug.Log("[ShopManager] Decor containers refreshed after ImportSaveData.");
    }

    private void UpdatePurchaseButtonState()
    {
        if (purchaseButtonEnabled == null || purchaseButtonDisabled == null) return;

        bool hasItemToBuy = false;

        foreach (var container in spawnedFoodContainers)
        {
            if (container.GetCurrentQuantity() > 0)
            {
                hasItemToBuy = true;
                break;
            }
        }

        if (!hasItemToBuy)
        {
            foreach (var container in spawnedDecorContainers)
            {
                if (container.GetCount() > 0)
                {
                    hasItemToBuy = true;
                    break;
                }
            }
        }

        purchaseButtonEnabled.gameObject.SetActive(hasItemToBuy);
        purchaseButtonDisabled.gameObject.SetActive(!hasItemToBuy);
    }

    private void UpdateTotalPriceUI()
    {
        if (totalPriceText == null) return;
        string formattedValue = DoubleUtilities.ToIdleNotation(tempTotalPrice);
        string localizedSymbol = currencySymbol.GetLocalizedString();
        totalPriceText.text = $"{formattedValue}{localizedSymbol}";
    }

    // ─────────────────────────────────────────────────────
    // Purchase Logic
    // ─────────────────────────────────────────────────────
    public void ApplyPurchase()
    {
        // Food purchase
        foreach (var container in spawnedFoodContainers)
        {
            var itemData = container.GetItemData();
            int quantity = container.GetCurrentQuantity();

            if (quantity > 0)
                FoodInventoryUI.Instance.AddItem(itemData, quantity);

            container.ConfirmPurchase();
        }

        // Decor purchase
        foreach (var container in spawnedDecorContainers)
        {
            if (container.GetCount() > 0)
                container.ConfirmPurchase();
        }

        DeductTotalPrice();
        UpdateAllUI();

        OnItemPurchased?.Invoke();
    }

    public void ResetAllSelections()
    {
        foreach (var container in spawnedFoodContainers)
            container.ResetSelection();

        foreach (var container in spawnedDecorContainers)
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
}

[System.Serializable]
public class Tab
{
    public string tabName;              // "Food" / "Decoration"
    public Button button;               // Button reference
    public TMP_Text labelText;          // Text label of the tab
    public GameObject outline;          // Outline image of the tab
    public GameObject group;     // Corresponding ScrollRect
}
