using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : SingletonMonobehaviour<ShopManager>
{
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
    [SerializeField] private List<CharacterTabSO> characterTabList;

    [Header("Shop Tabs (Food / Decor / Fashion)")]
    [SerializeField] private List<ShopTab> shopTabs;
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private Color tabColor;

    [Header("Buttons")]
    [SerializeField] private Button purchaseButton;

    private readonly List<FoodItemContainer> spawnedFoodContainers = new();
    private readonly List<DecorationItemContainer> spawnedDecorContainers = new();
    private readonly List<CharacterTabButton> spawnedCharacterTabs = new();

    private CharacterTabButton currentSelectedTab = null;
    private ShopTab currentActiveTab = null;
    private double tempTotalPrice;

    // ─────────────────────────────────────────────────────
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
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(ApplyPurchase);

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
            tab.Configure(tabData.avatarIcon, tabData.character);
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
            if (tab.scrollRectGO != null) tab.scrollRectGO.SetActive(false);
        }
    }

    private void ActivateTab(ShopTab tab)
    {
        // Deselect old tab
        if (currentActiveTab != null)
        {
            SetTabVisual(currentActiveTab, false);
            if (currentActiveTab.scrollRectGO != null)
                currentActiveTab.scrollRectGO.SetActive(false);
        }

        // Select new tab
        currentActiveTab = tab;
        SetTabVisual(tab, true);

        if (tab.scrollRectGO != null)
            tab.scrollRectGO.SetActive(true);

        // Only for Decoration and Fashion → enable CharacterSelection Panel
        if (tab.tabName == "Decoration" || tab.tabName == "Fashion")
            characterSelectionPanel.SetActive(true);
        else
            characterSelectionPanel.SetActive(false);
    }

    private void SetTabVisual(ShopTab tab, bool isActive)
    {
        if (tab.labelText != null)
            tab.labelText.color = isActive ? Color.white : tabColor;

        if (tab.backgroundImage != null)
        {
            var color = tab.backgroundImage.color;
            color.a = isActive ? 1f : 0f;
            tab.backgroundImage.color = color;
        }
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
            {
                container.ConfirmPurchase();
            }
        }

        DeductTotalPrice();
        UpdateAllUI();
    }

    public void ResetAllSelections()
    {
        foreach (var container in spawnedFoodContainers)
            container.ResetSelection();

        foreach (var container in spawnedDecorContainers)
            container.Configure(container.GetItemData());

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
public class ShopTab
{
    public string tabName;              // "Food" / "Decoration" / "Fashion"
    public Button button;               // Button reference
    public Image backgroundImage;       // Background image of the tab
    public TMP_Text labelText;          // Text label of the tab
    public GameObject scrollRectGO;     // Corresponding ScrollRect
}
