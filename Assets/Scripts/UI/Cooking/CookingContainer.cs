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

    [Header("Plus Icons Between Ingredients")]
    [SerializeField] private List<GameObject> plusIcons = new List<GameObject>();

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
                ingredients[i].SetData(data);
            }

            else ingredients[i].Hide();
        }

        for (int i = 0; i < plusIcons.Count; i++)
            plusIcons[i].SetActive(i < shownCount - 1);

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
            ingredients[i].SetData(data);
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
            if (!Inventory.Instance.HasItems(ingredient))
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
                Inventory.Instance.RemoveItem(ingredient);
        }

        //PlayerControl.Instance
        PlayerControl.Instance.interactDetector.ForceStartInteraction();
        Inventory.Instance.AddItem(itemData, 1);
        UIManager.Instance.ToggleCookingPanel(false);
        AudioManager.Instance.PlayInteractSound(6);
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
    public TextMeshProUGUI quantityText;

    public void SetData(RequiredIngredient data)
    {
        ingredientImage.sprite = data.icon;
        int owned = Inventory.Instance.GetTotalQuantityOfItem(data.ingredientType);
        quantityText.text = $"{owned}/{data.amount}";
        quantityText.color = owned >= data.amount ? Color.green : Color.red;
        ingredientsContainer.SetActive(true);
    }

    public void Hide()
    {
        ingredientsContainer.SetActive(false);
    }
}
