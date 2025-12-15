using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class DishSlot : MonoBehaviour
{
    [Header("Main Dish UI")]
    [SerializeField] private TextMeshProUGUI dishNameText;
    [SerializeField] private Image dishImage;
    [SerializeField] private Image selectionOutline;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject[] statusGroup;

    [Header("Shared UI Colors")]
    [SerializeField] private UIColorsConfig colorConfig;

    private ItemDataSO itemData;
    private bool isLocked;

    public ItemDataSO DishData => itemData;
    public Button GetButton() => selectButton;
    public bool IsLocked() => isLocked;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle (Event Subscription)
    // ─────────────────────────────────────────────────────

    // [FIX] Use OnEnable to ensure state updates every time the panel opens
    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged += EvaluateLockState;

        // Force check immediately in case level changed while this object was disabled
        EvaluateLockState();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= EvaluateLockState;
    }

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the UI slot using dish data.
    /// </summary>
    public void SetupCookingContainer(ItemDataSO newItemData)
    {
        itemData = newItemData;
        RefreshLocalizedName();

        dishImage.sprite = itemData.icon;
        EvaluateLockState();
        SetSelected(false);
    }

    /// <summary>
    /// Refreshes the localized name text for this dish.
    /// </summary>
    public void RefreshLocalizedName()
    {
        LocalizationManager.Instance.SetLocalizedText(
            dishNameText,
            "Food Labels",
            itemData.itemNameKey
        );
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionOutline != null)
            selectionOutline.color = isSelected ? Color.red : colorConfig.plateEmptyColor;
    }

    public void EvaluateLockState()
    {
        // Safety check to prevent errors if event fires before data setup
        if (itemData == null) return;

        isLocked = GameManager.Instance.CurrentLevel < itemData.requiredLevel;
        UpdateLockVisuals(isLocked);
    }

    private void UpdateLockVisuals(bool locked)
    {
        if (statusGroup.Length >= 2)
        {
            statusGroup[0].SetActive(!locked);
            statusGroup[1].SetActive(locked);
        }

        selectButton.interactable = !locked;
    }
}

// ─────────────────────────────────────────────────────
// Helper Class: IngredientUI
// ─────────────────────────────────────────────────────

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