using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Handles the detail panel UI, showing ingredients and cook button.
/// </summary>
public class RecipeDetailPanel : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("Ingredient UI")]
    [SerializeField] private Transform ingredientParent;
    [SerializeField] private GameObject ingredientPrefab;
    [SerializeField] private GameObject plusSignPrefab;

    [Header("Cook Button")]
    [SerializeField] private Button cookButton;
    [SerializeField] private TextMeshProUGUI cookText;
    [SerializeField] private UIColorsConfig colorsConfig;
    // ─────────────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────────────

    private ItemDataSO currentRecipe;

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Displays this panel with the given recipe's ingredients and cook logic.
    /// </summary>
    public void Show(ItemDataSO recipe)
    {
        gameObject.SetActive(true);
        currentRecipe = recipe;

        GenerateIngredientUI(showAsLocked: false);
    }

    public void ShowLocked(ItemDataSO recipe)
    {
        gameObject.SetActive(true);
        currentRecipe = recipe;

        GenerateIngredientUI(showAsLocked: true);
    }

    /// <summary>
    /// Hides the detail panel.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────
    // UI Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Spawns the ingredient icons and updates the cook button interactability.
    /// </summary>
    private void GenerateIngredientUI(bool showAsLocked)
    {
        foreach (Transform child in ingredientParent)
            Destroy(child.gameObject);

        int validCount = 0;
        int total = currentRecipe.requiredIngredients.Count;

        bool isLocked = GameManager.Instance.CurrentLevel < currentRecipe.requiredLevel;

        for (int i = 0; i < total; i++)
        {
            var data = currentRecipe.requiredIngredients[i];

            var ingredientSlotGO = Instantiate(ingredientPrefab, ingredientParent);   
            var ingredientSlot = ingredientSlotGO.GetComponent<IngredientSlot>();
            //ingredientSlot.Setup(data);

            if (showAsLocked)
            {
                ingredientSlot.SetupLocked(data); // Optional lock visuals
            }
            else
            {
                bool isEnough = ingredientSlot.Setup(data);
                if (isEnough) validCount++;

                if (i < total - 1)
                    Instantiate(plusSignPrefab, ingredientParent);
            }     

           
        }

        cookButton.interactable = !showAsLocked && validCount == total;
        cookText.color = cookButton.interactable ? colorsConfig.canCookColor : colorsConfig.cantCookColor;

        cookButton.onClick.RemoveAllListeners();
        if (!showAsLocked)
            cookButton.onClick.AddListener(() => Cook());
    }

    // ─────────────────────────────────────────────────────
    // Cooking Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Executes the cooking process: remove ingredients and add cooked item.
    /// </summary>
    private void Cook()
    {
        Debug.Log($"Cooked {currentRecipe.itemName}");

        foreach (var ingredient in currentRecipe.requiredIngredients)
        {
            for (int i = 0; i < ingredient.amount; i++)
                FoodInventoryUI.Instance.RemoveItem(ingredient);
        }

        FoodInventoryUI.Instance.AddItem(currentRecipe, 1);
        Hide();
        AudioManager.Instance.PlayInteractSound(8);
    }
}
