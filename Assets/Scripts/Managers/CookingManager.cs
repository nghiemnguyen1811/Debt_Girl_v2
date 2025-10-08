using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the overall cooking UI, including spawning recipe slots and showing details.
/// </summary>
public class CookingManager : SingletonMonobehaviour<CookingManager>
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Transform dishSlotParent;
    [SerializeField] private DishSlot dishSlotPrefab;
    [SerializeField] private RecipeDetailPanel detailPanel;

    [Header("Data")]
    [SerializeField] private List<ItemDataSO> allRecipes;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        InitializeRecipeSlots();
        detailPanel.Hide();
    }

    // ─────────────────────────────────────────────────────
    // Initialization Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Spawns all available recipe slots at the top panel.
    /// </summary>
    private void InitializeRecipeSlots()
    {
        foreach (Transform child in dishSlotParent)
            Destroy(child.gameObject);

        foreach (var recipe in allRecipes)
        {
            var slot = Instantiate(dishSlotPrefab, dishSlotParent);
            slot.Setup(recipe, this);
        }
    }

    // ─────────────────────────────────────────────────────
    // External Events
    // ─────────────────────────────────────────────────────
    public void SelectFirstUnlockedRecipe()
    {
        var firstUnlocked = allRecipes.FirstOrDefault(r => r.requiredLevel <= GameManager.Instance.CurrentLevel);
        if (firstUnlocked != null)
        {
            OnRecipeSelected(firstUnlocked);
        }
        else if (allRecipes.Count > 0)
        {
            // Không có món nào unlocked, vẫn chọn món đầu tiên (dù là khóa)
            OnRecipeSelected(allRecipes[0]);
        }
    }

    /// <summary>
    /// Called by a recipe slot when selected by the player.
    /// </summary>
    public void OnRecipeSelected(ItemDataSO recipe)
    {
        if (recipe.requiredLevel > GameManager.Instance.CurrentLevel)
        {
            detailPanel.ShowLocked(recipe);
            return;
        }

        detailPanel.Show(recipe);
    }

  
    /// <summary>
    /// Returns the list of all available recipes.
    /// </summary>
    public List<ItemDataSO> GetAllRecipes() => allRecipes;
}
