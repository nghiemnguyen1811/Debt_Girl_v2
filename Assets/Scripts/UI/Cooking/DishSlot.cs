using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class DishSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("Main Dish UI")]
    [SerializeField] private TextMeshProUGUI dishNameText;
    [SerializeField] private Image dishImage;
    [SerializeField] private Image selectionOutline;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject[] statusGroup;

    [Header("Shared UI Color Config")]
    [SerializeField] private UIColorsConfig colorConfig;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────
    private ItemDataSO itemData;
    private bool isLocked;

    // ─────────────────────────────────────────────────────
    // Public Accessors
    // ─────────────────────────────────────────────────────
    public ItemDataSO DishData => itemData;
    public Button GetButton() => selectButton;
    public bool IsLocked() => isLocked;
    public ItemDataSO GetItemData() => itemData;

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the slot UI with given dish data.
    /// </summary>
    public void SetupCookingContainer(ItemDataSO newItemData)
    {
        itemData = newItemData;

        LocalizationManager.Instance.SetLocalizedText(dishNameText, "Recipe Labels", itemData.itemNameKey);
        dishImage.sprite = itemData.icon;

        EvaluateLockState();
        SetSelected(false);
    }

    /// <summary>
    /// Highlights or unhighlights the dish slot when selected.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectionOutline == null) return;
        selectionOutline.color = isSelected ? Color.red : colorConfig.plateEmptyColor;
    }

    /// <summary>
    /// Checks if this dish is locked based on player level.
    /// </summary>
    public void EvaluateLockState()
    {
        int requiredLevel = itemData.requiredLevel;
        isLocked = GameManager.Instance.CurrentLevel < requiredLevel;
        UpdateLockVisuals(isLocked);
    }

    // ─────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Updates UI visuals to reflect locked/unlocked state.
    /// </summary>
    private void UpdateLockVisuals(bool isLocked)
    {
        if (statusGroup == null || statusGroup.Length < 2)
        {
            Debug.LogWarning("Lock visual group is not properly configured.");
            return;
        }

        statusGroup[0].SetActive(!isLocked);
        statusGroup[1].SetActive(isLocked);
        selectButton.interactable = !isLocked;
    }
}

[System.Serializable]
public class IngredientUI
{
    [Header("Ingredient UI Elements")]
    public GameObject ingredientsContainer;
    public Image ingredientImage;
    public Image frameQuantity;
    public TextMeshProUGUI quantityText;

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Sets ingredient visuals and quantity colors based on availability.
    /// </summary>
    public void SetData(RequiredIngredient data, UIColorsConfig colorConfig)
    {
        ingredientImage.sprite = data.icon;

        int owned = FoodInventoryUI.Instance.GetTotalQuantityOfItem(data.ingredientType);
        quantityText.text = $"{owned}/{data.amount}";

        bool isEnough = owned >= data.amount;

        // Text color
        quantityText.color = isEnough ? colorConfig.textEnoughColor : colorConfig.textNotEnoughColor;

        // Frame color
        if (frameQuantity != null)
            frameQuantity.color = isEnough ? colorConfig.frameEnoughColor : colorConfig.frameNotEnoughColor;

        ingredientsContainer.SetActive(true);
    }

    /// <summary>
    /// Hides the ingredient container from view.
    /// </summary>
    public void Hide()
    {
        ingredientsContainer.SetActive(false);
    }
}
