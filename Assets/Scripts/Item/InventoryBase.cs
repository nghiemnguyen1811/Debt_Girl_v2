using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base inventory system that manages slots, items, and selection logic.
/// Derived classes (UI) handle how the inventory is displayed and interacted with.
/// </summary>
public abstract class InventoryBase<T> : SingletonMonobehaviour<T> where T : InventoryBase<T>
{
    [Header("References")]
    protected PlayerControl playerControl;

    [Header("Slot Settings")]
    [SerializeField] private int slotCount = 52;
    [SerializeField] private ItemSlot slotPrefab;
    [SerializeField] private Transform slotContainer;

    protected readonly List<ItemSlot> slots = new();
    protected ItemSlot selectedItem;
    protected int selectedIndex = -1;
    protected int filledSlotCount;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Unity Start – initialize slots on scene start.
    /// </summary>
    protected virtual void Start()
    {
        playerControl = PlayerControl.Instance;

        InitializeSlots();
    }

    /// <summary>
    /// Creates inventory slots and binds selection listeners.
    /// </summary>
    protected void InitializeSlots()
    {
        slots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            var newSlot = Instantiate(slotPrefab, slotContainer);
            newSlot.SetEmpty();

            int itemIndex = i;
            newSlot.GetSelectButton().onClick.AddListener(() => SelectItem(itemIndex, true));
            slots.Add(newSlot);
        }

        DeSelectItem();
        RaiseSlotUsageChanged();
        OnInventoryInitialized();
    }

    // ─────────────────────────────────────────────────────
    // Public API - Item Access & Modification
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds items to the inventory, stacking if possible, otherwise filling empty slots.
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
                    OnSlotContentChanged(stackSlot);
                    continue;
                }
            }

            ItemSlot emptySlot = FindEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.SetItemData(itemData, 1);
                IncrementFilledSlot();
                OnSlotContentChanged(emptySlot);
                continue;
            }

            OnInventoryFull(GetRandomFullMessage());
            break;
        }
    }

    /// <summary>
    /// Checks if the inventory has enough of a required ingredient.
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
    /// Removes one item matching the required ingredient from the inventory.
    /// </summary>
    public void RemoveItem(RequiredIngredient requiredIngredient)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty()) continue;

            if (slots[i].ItemData.ingredientType == requiredIngredient.ingredientType)
            {
                slots[i].DecreaseQuantity();
                OnSlotContentChanged(slots[i]);

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
    /// Gets total amount of items of a given ingredient type.
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
    // Selection
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Selects an item at a given index and triggers UI feedback.
    /// </summary>
    protected void SelectItem(int index, bool playFeedback = false)
    {
        if (slots[index].IsEmpty()) return;

        selectedItem = slots[index];
        selectedIndex = index;

        ClearAllHighlights();
        selectedItem.SetSelected(true);

        OnItemSelected(selectedItem, playFeedback);
    }

    /// <summary>
    /// Deselects the currently selected item.
    /// </summary>
    public void DeSelectItem()
    {
        selectedItem = null;
        selectedIndex = -1;

        ClearAllHighlights();
        OnItemDeselected();
    }

    // ─────────────────────────────────────────────────────
    // Actions on selected item (UI calls these indirectly)
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Uses the selected item (consume only).
    /// </summary>
    protected void UseSelectedItem_ConsumeOnly()
    {
        if (selectedItem == null) return;
        var data = selectedItem.ItemData;
        if (!data.CanBeUsed) return;

        OnUseSelectedItem(data);
        RemoveSelectedItem();
    }

    /// <summary>
    /// Sells the selected item (sellable only).
    /// </summary>
    protected void SellSelectedItem_SellableOnly()
    {
        if (selectedItem == null) return;
        var data = selectedItem.ItemData;
        if (!data.CanBeSold) return;

        OnSellSelectedItem(data);
        RemoveSelectedItem();
    }

    /// <summary>
    /// Drops the selected item (generic drop).
    /// </summary>
    protected void DropSelectedItem_Generic()
    {
        if (selectedItem == null) return;
        OnDropSelectedItem(selectedItem.ItemData);
        RemoveSelectedItem();
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Decreases item quantity, removes slot if empty, updates selection.
    /// </summary>
    protected void RemoveSelectedItem()
    {
        selectedItem.DecreaseQuantity();
        OnSlotContentChanged(selectedItem);

        if (selectedItem.Quantity == 0)
        {
            selectedItem.SetEmpty();
            DeSelectItem();
            DecrementFilledSlot();
            return;
        }

        // Refresh UI for remaining quantity
        SelectItem(selectedIndex);
    }

    /// <summary>
    /// Updates UI visuals of all slots.
    /// </summary>
    protected void UpdateAllSlotsVisual()
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty()) slot.UpdateSlotUI();
            else slot.SetEmpty();
        }
        RaiseSlotUsageChanged();
    }

    /// <summary>
    /// Finds an existing stackable slot for the given item.
    /// </summary>
    protected ItemSlot FindStackableSlot(ItemDataSO itemData)
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
    /// Finds the first empty slot in the inventory.
    /// </summary>
    protected ItemSlot FindEmptySlot()
    {
        foreach (var slot in slots)
            if (slot.IsEmpty()) return slot;
        return null;
    }

    /// <summary>
    /// Clears highlight state from all slots.
    /// </summary>
    protected void ClearAllHighlights()
    {
        foreach (var slot in slots)
            slot.SetSelected(false);
    }

    /// <summary>
    /// Increments the filled slot counter and notifies UI.
    /// </summary>
    protected void IncrementFilledSlot()
    {
        filledSlotCount++;
        RaiseSlotUsageChanged();
    }

    /// <summary>
    /// Decrements the filled slot counter and notifies UI.
    /// </summary>
    protected void DecrementFilledSlot()
    {
        filledSlotCount = Mathf.Max(0, filledSlotCount - 1);
        RaiseSlotUsageChanged();
    }

    /// <summary>
    /// Notifies subclasses when slot usage changes.
    /// </summary>
    protected void RaiseSlotUsageChanged()
    {
        OnSlotUsageChanged(filledSlotCount, slotCount);
    }

    /// <summary>
    /// Returns default message when inventory is full (override for localization).
    /// </summary>
    protected virtual string GetRandomFullMessage()
    {
        return "Inventory full! Please clear some space to add new items.";
    }

    // ─────────────────────────────────────────────────────
    // Hooks for derived UI classes
    // ─────────────────────────────────────────────────────

    protected virtual void OnInventoryInitialized() { }
    protected virtual void OnSlotContentChanged(ItemSlot slot) { }
    protected virtual void OnItemSelected(ItemSlot slot, bool playFeedback) { }
    protected virtual void OnItemDeselected() { }
    protected virtual void OnSlotUsageChanged(int used, int total) { }
    protected virtual void OnInventoryFull(string message) { }

    // Hooks for actions
    protected virtual void OnUseSelectedItem(ItemDataSO data) { }
    protected virtual void OnSellSelectedItem(ItemDataSO data) { }
    protected virtual void OnDropSelectedItem(ItemDataSO data) { }

    // ─────────────────────────────────────────────────────
    // Public API for UI buttons
    // ─────────────────────────────────────────────────────

    public void UI_Use() => UseSelectedItem_ConsumeOnly();
    public void UI_Sell() => SellSelectedItem_SellableOnly();
    public void UI_Drop() => DropSelectedItem_Generic();
    public void UI_Close() => OnCloseRequested();

    /// <summary>
    /// Called when inventory is requested to close (override in UI).
    /// </summary>
    protected virtual void OnCloseRequested() { }
}
