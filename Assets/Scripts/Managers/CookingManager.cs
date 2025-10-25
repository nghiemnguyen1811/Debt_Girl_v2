using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the overall cooking UI, including spawning recipe slots, ingredient display, and cooking logic.
/// </summary>
public class CookingManager : SingletonMonobehaviour<CookingManager>
{
    public event Action OnDishCooked;

    // ─────────────────────────────────────────────────────
    #region Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Transform dishSlotParent;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button cookButton;

    [Header("Ingredient Display")]
    [SerializeField] private List<IngredientUI> ingredientSlots = new();
    [SerializeField] private List<GameObject> plusSignsBetweenIngredients = new();

    [Header("Dish Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Dish Recipes")]
    [SerializeField] private List<ItemDataSO> allDishRecipes;
    [SerializeField] private DishSlot dishSlotPrefab;

    [Header("Ingredient UI Colors")]
    [SerializeField] private UIColorsConfig uiColorsConfig;

    [Header("Animation Settings")]
    [SerializeField] private float floatingTextFadeDuration = 2f;

    #endregion

    // ─────────────────────────────────────────────────────
    #region Internal Cache
    // ─────────────────────────────────────────────────────

    private readonly List<DishSlot> spawnedDishSlots = new();
    private DishSlot selectedDish;
    private int selectedIndex = -1;

    private ItemDataSO SelectedDishData => selectedDish?.DishData;

    #endregion

    // ─────────────────────────────────────────────────────
    #region Unity Lifecycle
    // ─────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged += RefreshCakeUnlockStates;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= RefreshCakeUnlockStates;
    }

    private void Start()
    {
        InitializeUI();
        GenerateDishSelectionList();
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region Initialization
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Called when player level changes in GameManager.
    /// Refreshes all cake displays to unlock newly available cakes.
    /// </summary>
    public void RefreshCakeUnlockStates()
    {
        foreach (var cakeDisplay in spawnedDishSlots)
        {
            if (cakeDisplay == null) continue;
            cakeDisplay.EvaluateLockState();
        }

        if (selectedDish != null && selectedDish.IsLocked())
        {
            selectedDish = null;
            selectedIndex = -1;
            ResetUIState();
        }
    }

    /// <summary>
    /// Initializes UI elements, sets up listeners, and resets states.
    /// </summary>
    private void InitializeUI()
    {
        SetupListeners();
        ResetUIState();
    }

    /// <summary>
    /// Sets up listeners for navigation and cook buttons.
    /// </summary>
    private void SetupListeners()
    {
        cookButton?.onClick.AddListener(TryCookSelectedDish);

        prevButton?.onClick.AddListener(() =>
        {
            int newIndex = selectedIndex - 1;
            SelectDishAtIndex(newIndex);
            UpdateScrollPositionSmooth(newIndex);
        });

        nextButton?.onClick.AddListener(() =>
        {
            int newIndex = selectedIndex + 1;
            SelectDishAtIndex(newIndex);
            UpdateScrollPositionSmooth(newIndex);
        });
    }

    /// <summary>
    /// Resets ingredient slots, plus signs, and disables the cook button.
    /// </summary>
    private void ResetUIState()
    {
        ingredientSlots.ForEach(slot => slot.Hide());
        plusSignsBetweenIngredients.ForEach(plus => plus.SetActive(false));

        if (cookButton != null)
            cookButton.interactable = false;
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region Dish List Generation
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Generates the dish selection UI list based on available recipes.
    /// </summary>
    private void GenerateDishSelectionList()
    {
        ClearDishList();

        var validDishes = allDishRecipes.Where(IsCookableRecipe).ToList();
        for (int i = 0; i < validDishes.Count; i++)
        {
            var display = Instantiate(dishSlotPrefab, dishSlotParent);
            display.SetupCookingContainer(validDishes[i]);

            int index = i;
            display.GetButton().onClick.AddListener(() => SelectDishAtIndex(index));
            spawnedDishSlots.Add(display);
        }

        StartCoroutine(DelayScrollReset());
    }

    /// <summary>
    /// Clears existing dish display elements.
    /// </summary>
    private void ClearDishList()
    {
        foreach (var display in spawnedDishSlots)
            if (display != null)
                Destroy(display.gameObject);

        spawnedDishSlots.Clear();
    }

    /// <summary>
    /// Determines if the given recipe is a cookable crafted food.
    /// </summary>
    private bool IsCookableRecipe(ItemDataSO item)
    {
        return item.itemType == ItemType.CraftedFood && !item.canBeSold;
    }

    /// <summary>
    /// Waits one frame before resetting scroll to the start position.
    /// </summary>
    private IEnumerator DelayScrollReset()
    {
        yield return new WaitUntil(() => scrollRect.gameObject.activeInHierarchy);
        yield return null;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region Dish Selection
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Selects a dish at the given index and updates the UI accordingly.
    /// </summary>
    private void SelectDishAtIndex(int index)
    {
        if (index < 0 || index >= spawnedDishSlots.Count) return;
        if (spawnedDishSlots[index].IsLocked()) return;

        selectedDish = spawnedDishSlots[index];
        selectedIndex = index;

        foreach (var dish in spawnedDishSlots)
            dish.SetSelected(false);

        selectedDish.SetSelected(true);

        UpdateIngredientUI(SelectedDishData);
        UpdatePlusSigns(SelectedDishData);
        UpdateNavigationButtons(index);

        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Updates the interactable state of previous and next buttons.
    /// </summary>
    private void UpdateNavigationButtons(int index)
    {
        if (prevButton != null)
            prevButton.interactable = index > 0 && !spawnedDishSlots[index - 1].IsLocked();

        if (nextButton != null)
            nextButton.interactable = index < spawnedDishSlots.Count - 1 && !spawnedDishSlots[index + 1].IsLocked();
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region UI Updates
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Updates ingredient slots for the selected dish.
    /// </summary>
    private void UpdateIngredientUI(ItemDataSO dishData)
    {
        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            if (i < dishData.requiredIngredients.Count)
                ingredientSlots[i].SetData(dishData.requiredIngredients[i], uiColorsConfig);
            else
                ingredientSlots[i].Hide();
        }

        UpdateDishButtonState();
    }

    /// <summary>
    /// Updates visibility of plus signs between ingredients.
    /// </summary>
    private void UpdatePlusSigns(ItemDataSO dishData)
    {
        for (int i = 0; i < plusSignsBetweenIngredients.Count; i++)
            plusSignsBetweenIngredients[i].SetActive(i < dishData.requiredIngredients.Count - 1);
    }

    /// <summary>
    /// Enables or disables the cook button based on ingredient availability.
    /// </summary>
    private void UpdateDishButtonState()
    {
        if (SelectedDishData == null)
        {
            cookButton.interactable = false;
            return;
        }

        cookButton.interactable = SelectedDishData.requiredIngredients
            .All(ingredient => FoodInventoryUI.Instance.HasItems(ingredient));
    }

    /// <summary>
    /// Smoothly scrolls the recipe list to the target index.
    /// </summary>
    private void UpdateScrollPositionSmooth(int index)
    {
        if (spawnedDishSlots.Count <= 1 || scrollRect == null) return;

        float targetPos = Mathf.Clamp01((float)index / (spawnedDishSlots.Count - 1));

        DOTween.Kill(scrollRect);
        DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            targetPos,
            0.3f
        ).SetEase(Ease.OutCubic)
         .SetId(scrollRect);
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region Cooking Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to cook the selected dish and consume required ingredients.
    /// </summary>
    private void TryCookSelectedDish()
    {
        if (SelectedDishData == null) return;

        foreach (var ingredient in SelectedDishData.requiredIngredients)
        {
            for (int i = 0; i < ingredient.amount; i++)
                FoodInventoryUI.Instance.RemoveItem(ingredient);
        }

        SelectDishAtIndex(selectedIndex);
        PlayerControl.Instance.interactDetector.ForceStartInteraction();
        FoodInventoryUI.Instance.AddItem(SelectedDishData, 1);
        UIManager.Instance.ToggleCookingPanel(false);
        AudioManager.Instance.PlayInteractSound(8);

        OnDishCooked?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────────────────
    #region Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns all spawned dish slots.
    /// </summary>
    public List<DishSlot> GetAllDishes() => spawnedDishSlots;

    /// <summary>
    /// Reselects the currently selected dish.
    /// </summary>
    public void SelectCurrentDish() => SelectDishAtIndex(selectedIndex);

    #endregion
}
