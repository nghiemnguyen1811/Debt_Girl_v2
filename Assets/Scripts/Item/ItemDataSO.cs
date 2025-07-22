using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // Display Info
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
    // Classification
    // ─────────────────────────────────────────────────────
    [Title("Item Properties")]
    [LabelText("Item Type")]
    public ItemType itemType;

    [ShowIf("@IsIngredientCategory()")]
    [LabelText("Ingredient Type")]
    public IngredientType ingredientType = IngredientType.None;

    // ─────────────────────────────────────────────────────
    // Crafting
    // ─────────────────────────────────────────────────────
    [ShowIf("@itemType == ItemType.CraftedFood")]
    [LabelText("Ingredients Needed")]
    [OnValueChanged("LimitIngredients")]
    public List<RequiredIngredient> requiredIngredients = new List<RequiredIngredient>();

    [ShowIf("@itemType == ItemType.CraftedFood")]
    [LabelText("Can Be Sold")]
    [ToggleLeft]
    public bool canBeSold = true;

    [ShowIf("@itemType == ItemType.CraftedFood && canBeSold")]
    [LabelText("Sell Price"), SuffixLabel("$", true), MinValue(0)]
    [SerializeField] private double sellPrice = 0;

    // ─────────────────────────────────────────────────────
    // Consumption Effects
    // ─────────────────────────────────────────────────────
    [ShowIf("@itemType == ItemType.Consumable || (itemType == ItemType.CraftedFood && !canBeSold)")]
    [LabelText("Energy Restored")]
    [Range(0, 100)]
    [SuffixLabel("pts", true)]
    public int energy = 0;

    [ShowIf("@itemType == ItemType.Consumable || (itemType == ItemType.CraftedFood && !canBeSold)")]
    [LabelText("Mood Boost")]
    [Range(0, 100)]
    [SuffixLabel("pts", true)]
    public int mood = 0;

    // ─────────────────────────────────────────────────────
    // Pricing & Stackability
    // ─────────────────────────────────────────────────────
    [ShowIf("@itemType == ItemType.Material || itemType == ItemType.Consumable")]
    [LabelText("Purchase Cost"), SuffixLabel("$", true), MinValue(0)]
    public double purchaseCost = 0;

    [ToggleLeft]
    [LabelText("Stackable")]
    public bool canStackItem = false;

    [EnableIf("canStackItem")]
    [LabelText("Max Stack"), Range(1, 999)]
    public int maxStackAmount = 1;

    // ─────────────────────────────────────────────────────
    // Logic Properties
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Determines if the item can be used (consumed).
    /// </summary>
    public bool CanBeUsed =>
        itemType == ItemType.Consumable ||
        (itemType == ItemType.CraftedFood && !canBeSold);

    /// <summary>
    /// Determines if the item can be sold.
    /// </summary>
    public bool CanBeSold =>
        itemType == ItemType.Material ||
        (itemType == ItemType.CraftedFood && canBeSold);

    /// <summary>
    /// Gets the final sell price of the item based on its type.
    /// - CraftedFood: use sellPrice.
    /// - Material: 50% of purchaseCost.
    /// - Others: 0.
    /// </summary>
    public double SellPrice
    {
        get
        {
            if (itemType == ItemType.CraftedFood && canBeSold)
                return sellPrice;

            if (itemType == ItemType.Material)
                return purchaseCost * 0.5;

            return 0;
        }
    }

    // ─────────────────────────────────────────────────────
    // Editor-only logic
    // ─────────────────────────────────────────────────────
#if UNITY_EDITOR
    /// <summary>
    /// Ensures only up to 3 ingredients for CraftedFood.
    /// </summary>
    private void LimitIngredients()
    {
        if (requiredIngredients.Count > 3)
        {
            UnityEditor.EditorUtility.DisplayDialog(
                "Ingredient Limit Exceeded",
                "Only up to 3 ingredients are allowed per dish.",
                "OK"
            );
            requiredIngredients.RemoveRange(3, requiredIngredients.Count - 3);
        }
    }
#endif

    // ─────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────
    private bool IsIngredientCategory()
    {
        return itemType == ItemType.Material ||
               itemType == ItemType.Consumable ||
               itemType == ItemType.CraftedFood;
    }
}

[System.Serializable]
public class RequiredIngredient
{
    public IngredientType ingredientType;
    public Sprite icon;

    [Min(1)]
    public int amount = 1;
}
