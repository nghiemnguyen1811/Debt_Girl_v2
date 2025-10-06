using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a single clickable recipe item in the recipe selection panel.
/// </summary>
public class DishSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("UI Components")]
    [SerializeField] private Image dishImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject lockOverlayImage;

    // ─────────────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────────────

    private ItemDataSO recipeData;
    private CookingManager manager;
    private bool isLocked;
    // ─────────────────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────────────────

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

    private void EvaluateLockState()
    {
        int playerLevel = GameManager.Instance.CurrentLevel;
        isLocked = recipeData.requiredLevel > playerLevel;

        lockOverlayImage?.SetActive(isLocked);
        selectButton.interactable = true;
    }

    public bool IsLocked() => isLocked;
    public ItemDataSO GetRecipe() => recipeData;
}
