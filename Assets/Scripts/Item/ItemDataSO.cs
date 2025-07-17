using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // Item Display Info
    // ─────────────────────────────────────────────────────
    [Title("Item Info")]
    [HorizontalGroup("Top")]
    [PreviewField(60), HideLabel, GUIColor(0.9f, 0.9f, 1f)]
    public Sprite icon;

    [VerticalGroup("Top/Right"), LabelWidth(100)]
    public string itemName;

    [VerticalGroup("Top/Right"), LabelWidth(100)]
    [TextArea(2, 4)]
    public string description;

    // ─────────────────────────────────────────────────────
    // Item Properties
    // ─────────────────────────────────────────────────────
    [Title("Item Properties")]
    [LabelText("Item Type")]
    public ItemType itemType;

    [ShowIf("@this.itemType == ItemType.Material || itemType == ItemType.Consumable")]
    [LabelText("Ingredient Type")]
    public IngredientType ingredientType = IngredientType.None;

    [ShowIf("@this.itemType == ItemType.CraftedFood")]
    [LabelText("Ingredients Needed")]
    public List<IngredientAmount> requiredIngredients = new List<IngredientAmount>();

    [ShowIf("@itemType == ItemType.Consumable || itemType == ItemType.CraftedFood")]
    [LabelText("Energy Restored")]
    [Range(0, 100)]
    [SuffixLabel("pts", true)]
    public int energy = 0;

    [ShowIf("@itemType == ItemType.Consumable || itemType == ItemType.CraftedFood")]
    [LabelText("Mood Boost")]
    [Range(0, 100)]
    [SuffixLabel("pts", true)]
    public int mood = 0;

    [LabelText("Price"), SuffixLabel("$", true), MinValue(0)]
    public double price = 0;

    [ToggleLeft]
    [LabelText("Stackable")]
    public bool canStackItem = false;

    [EnableIf("canStackItem")]
    [LabelText("Max Stack"), Range(1, 999)]
    public int maxStackAmount = 1;
}

[System.Serializable]
public class IngredientAmount
{
    public IngredientType ingredientType;
    [Min(1)]
    public int amount = 1;
}