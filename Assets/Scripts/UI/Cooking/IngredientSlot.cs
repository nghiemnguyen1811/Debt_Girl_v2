using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays a single ingredient slot in the cooking panel.
/// Can show unlocked state (with count and frame) or locked state (overlay icon).
/// </summary>
public class IngredientSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields (set in Inspector)
    // ─────────────────────────────────────────────────────

    [Header("Visual Components")]
    [Tooltip("The main ingredient icon.")]
    [SerializeField] private Image ingredientImage;

    [Tooltip("Shows owned/required count when unlocked.")]
    [SerializeField] private TextMeshProUGUI countText;

    [Tooltip("Frame color changes based on enough/not enough.")]
    [SerializeField] private Image frame;

    [Tooltip("Reference to UI colors config for consistent styling.")]
    [SerializeField] private UIColorsConfig colorsConfig;

    [Tooltip("Overlay lock image shown when this ingredient is locked.")]
    [SerializeField] private GameObject lockOverlayImage;

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Sets data and visuals for an unlocked ingredient.
    /// Shows icon, count text, and frame color.
    /// </summary>
    /// <param name="data">Required ingredient info (icon, type, amount).</param>
    /// <returns>True if player owns enough of this ingredient.</returns>
    public bool Setup(RequiredIngredient data)
    {
        // Set icon
        ingredientImage.sprite = data.icon;

        // Calculate owned count
        int owned = FoodInventoryUI.Instance.GetTotalQuantityOfItem(data.ingredientType);

        // Update count text
        countText.gameObject.SetActive(true);
        countText.text = $"{owned}/{data.amount}";

        // Check if enough
        bool isEnough = owned >= data.amount;

        // Update frame color
        frame.gameObject.SetActive(true);
        frame.color = isEnough ? colorsConfig.frameEnoughColor : colorsConfig.frameNotEnoughColor;

        // Hide lock overlay in unlocked state
        lockOverlayImage.SetActive(false);

        return isEnough;
    }

    /// <summary>
    /// Sets visuals for a locked ingredient.
    /// Hides count text and shows lock overlay.
    /// </summary>
    /// <param name="data">Ingredient data (used for icon only).</param>
    public void SetupLocked(RequiredIngredient data)
    {
        // Set icon but hide text
        ingredientImage.sprite = data.icon;

        countText.gameObject.SetActive(false);   // don't show count
        frame.gameObject.SetActive(false);       // frame hidden in locked state

        // Show lock overlay
        lockOverlayImage.SetActive(true);
    }
}
