using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : SingletonMonobehaviour<Inventory>
{
    // ─────────────────────────────────────────────────────
    // References
    // ─────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private PlayerControl playerControl;
    [SerializeField] private Transform itemInfoContainer;

    // ─────────────────────────────────────────────────────
    // Slot Settings
    // ─────────────────────────────────────────────────────
    [Header("Slot Settings")]
    [SerializeField] private int slotCount = 52;
    [SerializeField] private ItemSlot slotPrefab;
    [SerializeField] private Transform slotContainer;
    private readonly List<ItemSlot> slots = new();

    // ─────────────────────────────────────────────────────
    // UI Elements
    // ─────────────────────────────────────────────────────
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemQuantityText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI moodText;
    [SerializeField] private TextMeshProUGUI slotUsageText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Transform statGroupRoot;
    [SerializeField] private GameObject infoDisplayGroup;
    [SerializeField] private GameObject sellPriceGroup;
    [SerializeField] private Button useButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button closeButton;

    // ─────────────────────────────────────────────────────
    // Full Inventory Messages
    // ─────────────────────────────────────────────────────
    [Header("Full Inventory Messages")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] inventoryFullMessages = {
        "Inventory full! Please clear some space to add new items.",
        "Cannot add item. Your inventory is full!",
        "No space left in your inventory.",
        "Failed to add item: Inventory full.",
        "Bag full! Clear out some items first."
    };

    // ─────────────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────────────
    private ItemSlot selectedItem;
    private int selectedIndex = -1;
    private int filledSlotCount;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        useButton.onClick.AddListener(() => UseSelectedItem());
        sellButton.onClick.AddListener(() => SellSelectedItem());
        dropButton.onClick.AddListener(() => DropSelectedItem());
        closeButton.onClick.AddListener(() => playerControl.interactDetector.StopCurrentInteraction());

        InitializeSlots();
    }

    /// <summary>
    /// Create all inventory slots and initialize UI.
    /// </summary>
    private void InitializeSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            ItemSlot newSlot = Instantiate(slotPrefab, slotContainer);
            newSlot.SetEmpty();

            int itemIndex = i;
            newSlot.GetSelectButton().onClick.AddListener(() => SelectItem(itemIndex));
            slots.Add(newSlot);
        }

        DeSelectItem();
        UpdateUsedSlotCount();
    }

    // ─────────────────────────────────────────────────────
    // Public API - Item Access & Modification
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Add an item to inventory, stack if possible, or use an empty slot.
    /// </summary>
    public void AddItem(ItemDataSO itemData, int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            if (itemData.canStackItem)
            {
                ItemSlot stackSlot = FindStackableSlot(itemData);
                if (stackSlot != null)
                {
                    stackSlot.IncreaseQuantity();
                    continue;
                }
            }

            ItemSlot emptySlot = FindEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.SetItemData(itemData, 1);
                IncrementFilledSlot();
                continue;
            }

            string warning = inventoryFullMessages[Random.Range(0, inventoryFullMessages.Length)];
            UIManager.Instance.ShowWarningText(warning);
            break;
        }
    }


    /// <summary>
    /// Check whether the inventory has enough of a specific ingredient.
    /// </summary>
    public bool HasItems(RequiredIngredient requiredIngredient)
    {
        int amount = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty()) continue;

            if (slots[i].ItemData.ingredientType == requiredIngredient.ingredientType)
                amount += slots[i].Quantity;

            if (amount >= requiredIngredient.amount)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Remove one unit of a specific ingredient from inventory.
    /// </summary>
    public void RemoveItem(RequiredIngredient requiredIngredient)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty()) continue;

            if (slots[i].ItemData.ingredientType == requiredIngredient.ingredientType)
            {
                slots[i].DecreaseQuantity();

                if (slots[i].Quantity == 0)
                {
                    slots[i].SetEmpty();
                    DeSelectItem();
                    DecrementFilledSlot();
                }

                return;
            }
        }
    }

    /// <summary>
    /// Get the total quantity of a specific ingredient type in inventory.
    /// </summary>
    public int GetTotalQuantityOfItem(IngredientType ingredientType)
    {
        int total = 0;

        foreach (var slot in slots)
        {
            if (slot.IsEmpty()) continue;

            if (slot.ItemData.ingredientType == ingredientType)
                total += slot.Quantity;
        }

        return total;
    }

    // ─────────────────────────────────────────────────────
    // Public API - Selection & Usage
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Called when a slot is selected — display item info, enable buttons.
    /// </summary>
    public void SelectItem(int index)
    {
        if (slots[index].IsEmpty()) return;

        selectedItem = slots[index];
        selectedIndex = index;

        var data = selectedItem.ItemData;

        ClearAllHighlights();
        selectedItem.SetSelected(true);

        itemIconImage.sprite = data.icon;
        itemNameText.text = data.itemName;
        itemQuantityText.text = selectedItem.Quantity.ToString();
        itemDescriptionText.text = data.description;
        sellPriceText.text = data.SellPrice + "$";

        DisplayStatUI(data);

        useButton.gameObject.SetActive(data.CanBeUsed);
        sellButton.gameObject.SetActive(data.CanBeSold);
        dropButton.gameObject.SetActive(true);

        sellPriceGroup.SetActive(data.CanBeSold);
        infoDisplayGroup.SetActive(true);
    }

    /// <summary>
    /// Reset item detail panel and deselect all items.
    /// </summary>
    public void DeSelectItem()
    {
        selectedItem = null;
        selectedIndex = -1;

        itemNameText.text = string.Empty;
        itemQuantityText.text = string.Empty;
        itemDescriptionText.text = string.Empty;

        infoDisplayGroup.SetActive(false);
        useButton.gameObject.SetActive(false);
        dropButton.gameObject.SetActive(false);

        ClearAllHighlights();
    }

    // ─────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Uses the selected consumable item and applies its stat effects to the player.
    /// This method is called by the "Use" button in the inventory UI.
    /// </summary>
    private void UseSelectedItem()
    {
        var data = selectedItem.ItemData;
        if (!data.CanBeUsed) return;

        var stats = playerControl.stats;

        stats.ApplyStatChange(StatType.Productivity, data.energy);
        stats.ApplyStatChange(StatType.Mood, data.mood);

        RemoveSelectedItem();
    }

    /// <summary>
    /// Sells the selected item and adds its sell value to the player's money.
    /// This method is called by the "Sell" button in the inventory UI.
    /// </summary>
    private void SellSelectedItem()
    {
        var data = selectedItem.ItemData;
        if (!data.CanBeSold) return;

        MoneyManager.Instance.ChangeMoneys(data.SellPrice);
        AudioManager.Instance.PlayInteractSound(1);

        RemoveSelectedItem();
    }

    /// <summary>
    /// Decreases the quantity of the selected item.
    /// /// This method is indirectly triggered by the inventory UI when an item is used.
    /// </summary>
    private void DropSelectedItem()
    {
        RemoveSelectedItem();
    }

    /// <summary>
    /// Decreases the quantity of the selected item. 
    /// Clears the slot if no items remain.
    /// </summary>
    private void RemoveSelectedItem()
    {
        selectedItem.DecreaseQuantity();

        if (selectedItem.Quantity == 0)
        {
            selectedItem.SetEmpty();
            DeSelectItem();
            DecrementFilledSlot();
            return;
        }

        SelectItem(selectedIndex);
    }

    /// <summary>
    /// Updates the inventory slot usage text UI to display the current number 
    /// </summary>
    private void UpdateUsedSlotCount()
    {
        slotUsageText.text = $"{filledSlotCount} / {slotCount}";
    }

    /// <summary>
    /// Increments the filled slot count and updates the UI.
    /// </summary>
    private void IncrementFilledSlot()
    {
        filledSlotCount++;
        UpdateUsedSlotCount();
    }

    /// <summary>
    /// Decrements the filled slot count (not below 0) and updates the UI.
    /// </summary>
    private void DecrementFilledSlot()
    {
        filledSlotCount = Mathf.Max(0, filledSlotCount - 1);
        UpdateUsedSlotCount();
    }

    /// <summary>
    /// Refresh all item slot visuals.
    /// </summary>
    private void UpdateUI()
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty())
                slot.UpdateSlotUI();

            else slot.SetEmpty();
        }

        UpdateUsedSlotCount();
    }

    /// <summary>
    /// Search for an existing slot where this item can be stacked.
    /// </summary>
    private ItemSlot FindStackableSlot(ItemDataSO itemData)
    {
        foreach (var slot in slots)
        {
            if (slot.ItemData == itemData &&
                slot.Quantity < itemData.maxStackAmount)
                return slot;
        }

        return null;
    }

    /// <summary>
    /// Find the first empty slot available in the inventory.
    /// </summary>
    private ItemSlot FindEmptySlot()
    {
        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
                return slot;
        }

        return null;
    }

    /// <summary>
    /// Disable all selection outlines in item slots.
    /// </summary>
    private void ClearAllHighlights()
    {
        foreach (var slot in slots)
            slot.SetSelected(false);
    }

    /// <summary>
    /// Display energy and mood values from item data.
    /// </summary>
    private void DisplayStatUI(ItemDataSO data)
    {
        foreach (Transform stat in statGroupRoot)
            stat.gameObject.SetActive(false);

        bool hasEnergy = data.energy > 0;
        bool hasMood = data.mood > 0;

        if (hasEnergy)
        {
            statGroupRoot.GetChild(0).gameObject.SetActive(true);
            energyText.text = data.energy.ToString();
        }

        if (hasMood)
        {
            statGroupRoot.GetChild(1).gameObject.SetActive(true);
            moodText.text = data.mood.ToString();
        }

        statGroupRoot.gameObject.SetActive(hasEnergy || hasMood);
        StartCoroutine(RebuildLayoutNextFrame(itemInfoContainer));
    }

    private IEnumerator RebuildLayoutNextFrame(Transform layoutRoot)
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot as RectTransform);
    }
}
