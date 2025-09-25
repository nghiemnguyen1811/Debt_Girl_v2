using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CookingContainer : MonoBehaviour
{
    [Header("Main Dish UI")]
    [SerializeField] private TextMeshProUGUI dishNameText;
    [SerializeField] private Image dishImage;
    [SerializeField] private Button cookActionButton;

    [Header("Ingredients")]
    [SerializeField] private List<IngredientUI> ingredients = new List<IngredientUI>();

    [Header("Shared UI Color Config")]
    [SerializeField] private UIColorsConfig colorConfig;

    // ─────────────────────────────────────────────────────
    // Item Data
    // ─────────────────────────────────────────────────────
    private ItemDataSO itemData;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        cookActionButton.onClick.AddListener(() => ConfirmCooking());
    }

    // ─────────────────────────────────────────────────────
    // UI Setup
    // ─────────────────────────────────────────────────────

    public void SetupCookingContainer(ItemDataSO newItemData)
    {
        itemData = newItemData;

        dishNameText.text = itemData.itemName;
        dishImage.sprite = itemData.icon;

        int shownCount = itemData.requiredIngredients.Count;

        for (int i = 0; i < ingredients.Count; i++)
        {
            if (i < shownCount)
            {
                var data = itemData.requiredIngredients[i];
                ingredients[i].SetData(data, colorConfig);
            }

            else ingredients[i].Hide();
        }

        UpdateCookButtonInteractable();
    }

    /// <summary>
    /// Refresh ingredient UI (image and count), then update cook button state.
    /// </summary>
    public void RefreshIngredientUI()
    {
        int shownCount = itemData.requiredIngredients.Count;

        for (int i = 0; i < shownCount; i++)
        {
            var data = itemData.requiredIngredients[i];
            ingredients[i].SetData(data, colorConfig);
        }

        UpdateCookButtonInteractable();
    }

    // ─────────────────────────────────────────────────────
    // Button State Logic
    // ─────────────────────────────────────────────────────

    public void UpdateCookButtonInteractable()
    {
        bool canCook = true;

        foreach (var ingredient in itemData.requiredIngredients)
        {
            if (!FoodInventoryUI.Instance.HasItems(ingredient))
            {
                canCook = false;
                break;
            }
        }

        cookActionButton.interactable = canCook;
    }

    // ─────────────────────────────────────────────────────
    // Accessors
    // ─────────────────────────────────────────────────────

    public void ConfirmCooking()
    {
        // Optional: Add animation, sound, or UI feedback after cooking
        Debug.Log($"{itemData.itemName} has been cooked!");

        // looping through all of the items we need for crafting
        foreach (var ingredient in itemData.requiredIngredients)
        {
            for (int i = 0; i < ingredient.amount; i++)
                FoodInventoryUI.Instance.RemoveItem(ingredient);
        }

        //PlayerControl.Instance
        PlayerControl.Instance.interactDetector.ForceStartInteraction();
        FoodInventoryUI.Instance.AddItem(itemData, 1);
        UIManager.Instance.ToggleCookingPanel(false);
        AudioManager.Instance.PlayInteractSound(8);
    }

    public ItemDataSO GetItemData() => itemData;

    // ─────────────────────────────────────────────────────
    // Nested Class
    // ─────────────────────────────────────────────────────
}

[System.Serializable]
public class IngredientUI
{
    public GameObject ingredientsContainer;
    public Image ingredientImage;
    public Image frameQuantity;
    public TextMeshProUGUI quantityText;

    public void SetData(RequiredIngredient data, UIColorsConfig colorConfig)
    {
        ingredientImage.sprite = data.icon;

        int owned = FoodInventoryUI.Instance.GetTotalQuantityOfItem(data.ingredientType);
        quantityText.text = $"{owned}/{data.amount}";

        bool isEnough = owned >= data.amount;

        // Change text color
        quantityText.color = isEnough ? colorConfig.textEnoughColor : colorConfig.textNotEnoughColor;

        // Change frame color
        if (frameQuantity != null)
            frameQuantity.color = isEnough ? colorConfig.frameEnoughColor : colorConfig.frameNotEnoughColor;

        ingredientsContainer.SetActive(true);
    }

    public void Hide()
    {
        ingredientsContainer.SetActive(false);
    }
}

