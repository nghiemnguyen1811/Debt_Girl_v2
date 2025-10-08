using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a single clickable dish slot in the recipe selection panel.
/// Includes visual elements, lock overlay, and click handling.
/// </summary>
public class DishSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("UI Components")]
    [Tooltip("The image icon representing the dish.")]
    [SerializeField] private Image dishImage;

    [Tooltip("Displays the price of the dish.")]
    [SerializeField] private TextMeshProUGUI priceText;

    [Tooltip("The button that triggers recipe selection.")]
    [SerializeField] private Button selectButton;

    [Tooltip("Overlay icon that shows when the dish is locked.")]
    [SerializeField] private GameObject lockOverlayImage;

    // ─────────────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────────────

    private ItemDataSO recipeData;
    private CookingManager manager;
    private bool isLocked;

    // ─────────────────────────────────────────────────────
    // Public Accessors
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the associated recipe data.
    /// </summary>
    public ItemDataSO GetRecipe() => recipeData;

    /// <summary>
    /// Returns whether the dish is currently locked.
    /// </summary>
    public bool IsLocked() => isLocked;

    // ─────────────────────────────────────────────────────
    // Initialization
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes this slot with a recipe and reference to the CookingManager.
    /// </summary>
    /// <param name="data">The recipe data to display.</param>
    /// <param name="mgr">Reference to the CookingManager.</param>
    public void Setup(ItemDataSO data, CookingManager mgr)
    {
        recipeData = data;
        manager = mgr;

        dishImage.sprite = data.icon;
        priceText.text = $"{data.SellPrice}원";

        EvaluateLockState();

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => manager.OnRecipeSelected(recipeData));
    }

    // ─────────────────────────────────────────────────────
    // Lock State Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates if this dish should be locked based on player level.
    /// Updates visual state accordingly.
    /// </summary>
    private void EvaluateLockState()
    {
        int playerLevel = GameManager.Instance.CurrentLevel;
        isLocked = recipeData.requiredLevel > playerLevel;

        // Show or hide the lock overlay
        lockOverlayImage?.SetActive(isLocked);

        // Button remains clickable even if locked (to trigger ShowLocked panel)
        selectButton.interactable = true;
    }
}
