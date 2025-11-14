using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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

    private readonly List<FoodItemContainer> spawnedFoodContainers = new();
    private readonly List<DecorationItemContainer> spawnedDecorContainers = new();
    private readonly List<CharacterTabButton> spawnedCharacterTabs = new();

    private CharacterTabButton currentSelectedTab;
    private Tab currentActiveTab;
    private double tempTotalPrice;

    //─────────────────────────────────────────────────────
    // Unity Lifecycle
    //─────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.RegisterForGlobalRefresh(RefreshAllLocalizedTexts);
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.UnregisterForGlobalRefresh(RefreshAllLocalizedTexts);

        CharacterTabButton.OnTabSelected -= HandleTabSelected;
    }

    private void Start()
    {
        InitializeFoodUI();
        InitializeDecorUI();
        InitializeCharacterTabs();
        SetupListeners();
        SetupShopTabs();

        if (shopTabs.Count > 0)
            ActivateTab(shopTabs[0]); // Default to Food tab
    }

    private void SetupListeners()
    {
        if (purchaseButtonEnabled != null)
            purchaseButtonEnabled.onClick.AddListener(ApplyPurchase);

        CharacterTabButton.OnTabSelected += HandleTabSelected;
    }

    //─────────────────────────────────────────────────────
    // Localization Auto-Update
    //─────────────────────────────────────────────────────
    private void RefreshAllLocalizedTexts()
    {
        // Refresh all food items
        foreach (var container in spawnedFoodContainers)
        {
            if (container == null) continue;
            container.RefreshLocalizedTexts();
            container.RefreshLocalizedPrice();
        }

        // Refresh all decoration items
        foreach (var container in spawnedDecorContainers)
        {
            if (container == null) continue;
            container.RefreshLocalizedTexts();
            container.RefreshLocalizedPrice();
        }

        // Update total price text with new symbol
        UpdateTotalPriceUI();

        Debug.Log("[ShopManager] Localization refreshed for all shop containers.");
    }

    //─────────────────────────────────────────────────────
    // Character Tabs
    //─────────────────────────────────────────────────────
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
        if (currentSelectedTab != null && currentSelectedTab.CharacterType == selectedType)
        {
            currentSelectedTab.SetSelected(false);
            currentSelectedTab = null;
            ShowAllDecorItems();
            return;
        }

        if (currentSelectedTab != null)
            currentSelectedTab.SetSelected(false);

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

    //─────────────────────────────────────────────────────
    // Food UI
    //─────────────────────────────────────────────────────
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

    //─────────────────────────────────────────────────────
    // Decor UI
    //─────────────────────────────────────────────────────
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

    //─────────────────────────────────────────────────────
    // Shop Tabs
    //─────────────────────────────────────────────────────
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
        if (currentActiveTab != null)
        {
            SetTabVisual(currentActiveTab, false);
            if (currentActiveTab.group != null)
                currentActiveTab.group.SetActive(false);
        }

        currentActiveTab = tab;
        SetTabVisual(tab, true);

        if (tab.group != null)
            tab.group.SetActive(true);

        // Show character tabs only for certain categories
        characterSelectionPanel.SetActive(tab.tabName == "Decoration" || tab.tabName == "Fashion");
    }

    private void SetTabVisual(Tab tab, bool isActive)
    {
        if (tab.labelText != null)
            tab.labelText.color = isActive ? uiColorsConfig.tabOn : uiColorsConfig.tabOff;

        if (tab.outline != null)
            tab.outline.SetActive(isActive);
    }

    //─────────────────────────────────────────────────────
    // UI Updates
    //─────────────────────────────────────────────────────
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

        bool hasItemToBuy =
            spawnedFoodContainers.Any(c => c.GetCurrentQuantity() > 0) ||
            spawnedDecorContainers.Any(c => c.GetCount() > 0);

        purchaseButtonEnabled.gameObject.SetActive(hasItemToBuy);
        purchaseButtonDisabled.gameObject.SetActive(!hasItemToBuy);
    }

    private void UpdateTotalPriceUI()
    {
        if (totalPriceText == null) return;
        string formattedValue = DoubleUtilities.ToIdleNotation(tempTotalPrice);
        string localizedSymbol = LocalizationManager.Instance.GetCurrencySymbol();
        totalPriceText.text = $"{formattedValue}{localizedSymbol}";
    }

    //─────────────────────────────────────────────────────
    // Purchase Logic
    //─────────────────────────────────────────────────────
    public void ApplyPurchase()
    {
        foreach (var container in spawnedFoodContainers)
        {
            var itemData = container.GetItemData();
            int quantity = container.GetCurrentQuantity();

            if (quantity > 0)
                FoodInventoryUI.Instance.AddItem(itemData, quantity);

            container.ConfirmPurchase();
        }

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

    //─────────────────────────────────────────────────────
    // Temporary Price Control
    //─────────────────────────────────────────────────────
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
    public string tabName;

    [Header("Main")]
    public Button button;
    public GameObject outline;
    public GameObject group;

    public enum TabDisplayMode { Text, Icon, Both }

    [LabelText("Display Mode")]
    public TabDisplayMode displayMode;

    // Text
    [ShowIf("@displayMode == TabDisplayMode.Text || displayMode == TabDisplayMode.Both")]
    public TMP_Text labelText;

    // Icon
    [ShowIf("@displayMode == TabDisplayMode.Icon || displayMode == TabDisplayMode.Both")]
    public Image icon;
}
