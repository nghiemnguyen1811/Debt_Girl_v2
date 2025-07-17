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
    [SerializeField] private int slotCount = 50;
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
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI moodText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Transform statGroupRoot;
    [SerializeField] private GameObject infoDisplayGroup;
    [SerializeField] private Button useButton;
    [SerializeField] private Button dropButton;

    // ─────────────────────────────────────────────────────
    // Messages
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

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        useButton.onClick.AddListener(() => UseSelectedItem());
        dropButton.onClick.AddListener(() => DropSelectedItem());

        InitializeSlots();
    }

    /// <summary>
    /// Instantiate all inventory slots and reset selection UI.
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
    }

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Add item to inventory, stacking if possible, otherwise using empty slot.
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
                    UpdateUI();
                    return;
                }
            }

            ItemSlot emptySlot = FindEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.SetItemData(itemData, 1);
                UpdateUI();
                return;
            }

            // Inventory full
            string warning = inventoryFullMessages[Random.Range(0, inventoryFullMessages.Length)];
            UIManager.Instance.ShowWarningText(warning);
        }
    }

    /// <summary>
    /// Called when a slot is selected — display item info, enable buttons.
    /// </summary>
    public void SelectItem(int index)
    {
        if (slots[index].IsEmpty())
            return;

        selectedItem = slots[index];
        selectedIndex = index;

        ClearAllHighlights();
        selectedItem.SetSelected(true);

        itemIconImage.sprite = selectedItem.ItemData.icon;
        itemNameText.text = selectedItem.ItemData.itemName;
        itemDescriptionText.text = selectedItem.ItemData.description;

        DisplayStatUI(selectedItem.ItemData);

        useButton.gameObject.SetActive(selectedItem.ItemData.itemType == ItemType.Consumable);
        dropButton.gameObject.SetActive(true);

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

    /// <summary>
    /// Uses the selected consumable item and applies its stat effects to the player.
    /// This method is called by the "Use" button in the inventory UI.
    /// </summary>
    public void UseSelectedItem()
    {
        if (selectedItem.ItemData.itemType != ItemType.Consumable) return;

        var data = selectedItem.ItemData;
        var stats = playerControl.stats;

        stats.ApplyStatChange(StatType.Productivity, data.energy);
        stats.ApplyStatChange(StatType.Mood, data.mood);

        RemoveSelectedItem();
    }

    /// <summary>
    /// Decreases the quantity of the selected item.
    /// /// This method is indirectly triggered by the inventory UI when an item is used.
    /// </summary>
    public void DropSelectedItem()
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
        }
    }

    // ─────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────

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

        // Force layout update to fix spacing issues
        StartCoroutine(RebuildLayoutNextFrame(itemInfoContainer));
    }

    private IEnumerator RebuildLayoutNextFrame(Transform layoutRoot)
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot as RectTransform);
    }
}
