using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages cooking UI and container setup for non-sellable crafted food.
/// </summary>
public class CookingManager : SingletonMonobehaviour<CookingManager>
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Transform cookingContainerParent;

    [Header("Data & Prefab")]
    [SerializeField] private List<ItemDataSO> allRecipeList;
    [SerializeField] private CookingContainer cookingContainerPrefab;

    // ─────────────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────────────

    private readonly List<CookingContainer> spawnedCookingContainers = new();

    // ─────────────────────────────────────────────────────
    // MonoBehaviour
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Called on startup to initialize the cooking interface.
    /// </summary>
    private void Start()
    {
        InitializeCookingUI();
    }

    // ─────────────────────────────────────────────────────
    // Initialization Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Checks if a recipe is a cookable (non-sellable) crafted food.
    /// </summary>
    private bool IsCookableRecipe(ItemDataSO item)
    {
        return item.itemType == ItemType.CraftedFood && !item.canBeSold;
    }

    /// <summary>
    /// Instantiates UI containers for each valid cooking recipe.
    /// </summary>
    private void InitializeCookingUI()
    {
        ClearExistingContainers();

        var filteredRecipes = allRecipeList.Where(IsCookableRecipe);

        foreach (var recipe in filteredRecipes)
        {
            var container = Instantiate(cookingContainerPrefab, cookingContainerParent);
            container.SetupCookingContainer(recipe);
            spawnedCookingContainers.Add(container);
        }
    }

    /// <summary>
    /// Clears all existing cooking containers from the UI.
    /// </summary>
    private void ClearExistingContainers()
    {
        foreach (var container in spawnedCookingContainers)
        {
            if (container != null)
                Destroy(container.gameObject);
        }

        spawnedCookingContainers.Clear();
    }

    // ─────────────────────────────────────────────────────
    // UI Refresh & Utility
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the ingredient UI in all active containers.
    /// </summary>
    public void RefreshAllCookingContainers()
    {
        foreach (var container in spawnedCookingContainers)
            container.RefreshIngredientUI();
    }

    /// <summary>
    /// Returns a list of all spawned cooking containers.
    /// </summary>
    public List<CookingContainer> GetAllCookingContainers() => spawnedCookingContainers;
}
