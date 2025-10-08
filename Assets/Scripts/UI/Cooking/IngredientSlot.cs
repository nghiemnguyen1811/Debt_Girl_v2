using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays a single ingredient icon, quantity, and color status.
/// </summary>
public class IngredientSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("Visual Components")]
    [SerializeField] private Image ingredientImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image frame;
    [SerializeField] private UIColorsConfig colorsConfig;
    [SerializeField] private GameObject lockOverlayImage;


    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Sets data and visuals based on required ingredient data.
    /// </summary>
    /// <param name="data">Ingredient to display.</param>
    /// <returns>True if player has enough of this ingredient.</returns>
    public bool Setup(RequiredIngredient data)
    {
        ingredientImage.sprite = data.icon;
        int owned = FoodInventoryUI.Instance.GetTotalQuantityOfItem(data.ingredientType);
        
        countText.text = $"{owned}/{data.amount}";
        bool isEnough = owned >= data.amount;

        //countText.color = isEnough ? Color.black : Color.red;
        frame.color = isEnough ? colorsConfig.frameEnoughColor : colorsConfig.frameNotEnoughColor;
        lockOverlayImage.SetActive(false);

        return isEnough;
    }

    public void SetupLocked(RequiredIngredient data)
    {
        ingredientImage.sprite = data.icon;
   
        frame.gameObject.SetActive(false);
        frame.color = Color.gray;

        lockOverlayImage.SetActive(true);
    }
}
