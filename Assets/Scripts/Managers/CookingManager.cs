using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class CookingManager : SingletonMonobehaviour<CookingManager>
{
    [Header("References")]
    [SerializeField] private Transform cookingContainerParent;

    [Header("Data & Prefab")]
    [SerializeField] private List<ItemDataSO> allRecipeList;
    [SerializeField] private CookingContainer cookingContainerPrefab;

    private readonly List<CookingContainer> spawnedCookingContainers = new();

    // ─────────────────────────────────────────────────────
    // Mono
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        InitializeCookingUI();
    }

    // ─────────────────────────────────────────────────────
    // UI Initialization
    // ─────────────────────────────────────────────────────

    private bool IsCookableRecipe(ItemDataSO item)
    {
        return item.itemType == ItemType.CraftedFood;
    }

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

    public void RefreshAllCookingContainers()
    {
        foreach (var container in spawnedCookingContainers)
            container.RefreshIngredientUI();
    }

    public List<CookingContainer> GetAllCookingContainers() => spawnedCookingContainers;
}
