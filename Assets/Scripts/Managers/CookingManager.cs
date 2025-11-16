using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the cooking UI system: recipe list, ingredient display, selection, and cooking actions.
/// Also updates localized dish names dynamically when the language changes.
/// </summary>
public class CookingManager : SingletonMonobehaviour<CookingManager>
{
    public event Action OnDishCooked;

    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private Transform dishSlotParent;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button cookButtonEnabled;
    [SerializeField] private Button cookButtonDisabled;

    [Header("Ingredient Display")]
    [SerializeField] private List<IngredientUI> ingredientSlots = new();
    [SerializeField] private List<GameObject> plusSignsBetweenIngredients = new();

    [Header("Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Recipe Data")]
    [SerializeField] private List<ItemDataSO> allDishRecipes;
    [SerializeField] private DishSlot dishSlotPrefab;

    [Header("Ingredient UI Colors")]
    [SerializeField] private UIColorsConfig uiColorsConfig;

    [Header("Animation Settings")]
    [SerializeField] private float floatingTextFadeDuration = 2f;

    #endregion

    #region Internal Cache

    private readonly List<DishSlot> spawnedDishSlots = new();
    private DishSlot selectedDish;
    private int selectedIndex = -1;

    private ItemDataSO SelectedDishData => selectedDish?.DishData;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        LocalizationManager.Instance.RegisterForGlobalRefresh(RefreshLocalization);
    }

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

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.UnregisterForGlobalRefresh(RefreshLocalization);
    }

    private void Start()
    {
        InitializeUI();
        GenerateDishSelectionList();
    }

    #endregion

    #region Localization Refresh

    /// <summary>
    /// Refreshes all dish names and ingredient labels when the language is changed.
    /// </summary>
    private void RefreshLocalization()
    {
        foreach (var slot in spawnedDishSlots)
        {
            if (slot != null)
                slot.RefreshLocalizedName();
        }

        if (selectedDish != null)
        {
            UpdateIngredientUI(SelectedDishData);
        }
    }

    #endregion

    #region Initialization

    public void RefreshCakeUnlockStates()
    {
        foreach (var slot in spawnedDishSlots)
        {
            if (slot != null)
                slot.EvaluateLockState();
        }

        if (selectedDish != null && selectedDish.IsLocked())
        {
            selectedDish = null;
            selectedIndex = -1;
            ResetUIState();
        }
    }

    private void InitializeUI()
    {
        SetupListeners();
        ResetUIState();
    }

    private void SetupListeners()
    {
        cookButtonEnabled?.onClick.AddListener(TryCookSelectedDish);

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

    private void ResetUIState()
    {
        ingredientSlots.ForEach(slot => slot.Hide());
        plusSignsBetweenIngredients.ForEach(p => p.SetActive(false));

        cookButtonEnabled.gameObject.SetActive(false);
        cookButtonDisabled.gameObject.SetActive(true);
    }

    #endregion

    #region Dish List Generation

    private void GenerateDishSelectionList()
    {
        ClearDishList();

        var valid = allDishRecipes.Where(IsCookableRecipe).ToList();

        for (int i = 0; i < valid.Count; i++)
        {
            var slot = Instantiate(dishSlotPrefab, dishSlotParent);
            slot.SetupCookingContainer(valid[i]);

            int index = i;
            slot.GetButton().onClick.AddListener(() => SelectDishAtIndex(index));

            spawnedDishSlots.Add(slot);
        }

        StartCoroutine(DelayScrollReset());
    }

    private bool IsCookableRecipe(ItemDataSO item)
    {
        return item.itemType == ItemType.CraftedFood && !item.canBeSold;
    }

    private IEnumerator DelayScrollReset()
    {
        yield return null;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    private void ClearDishList()
    {
        foreach (var slot in spawnedDishSlots)
            if (slot != null) Destroy(slot.gameObject);

        spawnedDishSlots.Clear();
    }

    #endregion

    #region Dish Selection

    private void SelectDishAtIndex(int index)
    {
        if (index < 0 || index >= spawnedDishSlots.Count) return;
        if (spawnedDishSlots[index].IsLocked()) return;

        selectedDish = spawnedDishSlots[index];
        selectedIndex = index;

        foreach (var slot in spawnedDishSlots)
            slot.SetSelected(false);

        selectedDish.SetSelected(true);

        UpdateIngredientUI(SelectedDishData);
        UpdatePlusSigns(SelectedDishData);
        UpdateNavigationButtons(index);

        AudioManager.Instance.PlayInteractSound(8);
    }

    private void UpdateNavigationButtons(int index)
    {
        prevButton.interactable = index > 0 && !spawnedDishSlots[index - 1].IsLocked();
        nextButton.interactable = index < spawnedDishSlots.Count - 1 && !spawnedDishSlots[index + 1].IsLocked();
    }

    #endregion

    #region UI Updates

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

    private void UpdatePlusSigns(ItemDataSO dishData)
    {
        for (int i = 0; i < plusSignsBetweenIngredients.Count; i++)
            plusSignsBetweenIngredients[i].SetActive(i < dishData.requiredIngredients.Count - 1);
    }

    private void UpdateDishButtonState()
    {
        bool canCook = false;

        if (SelectedDishData != null)
        {
            canCook = SelectedDishData.requiredIngredients
                .All(i => FoodInventoryUI.Instance.HasItems(i));
        }

        cookButtonEnabled.gameObject.SetActive(canCook);
        cookButtonDisabled.gameObject.SetActive(!canCook);
    }

    private void UpdateScrollPositionSmooth(int index)
    {
        if (spawnedDishSlots.Count <= 1) return;

        float target = Mathf.Clamp01((float)index / (spawnedDishSlots.Count - 1));

        DOTween.Kill(scrollRect);
        DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            target,
            0.3f
        ).SetEase(Ease.OutCubic)
         .SetId(scrollRect);
    }

    #endregion

    #region Cooking Logic

    private void TryCookSelectedDish()
    {
        if (SelectedDishData == null) return;

        foreach (var ing in SelectedDishData.requiredIngredients)
            for (int i = 0; i < ing.amount; i++)
                FoodInventoryUI.Instance.RemoveItem(ing);

        SelectDishAtIndex(selectedIndex);

        PlayerControl.Instance.interactDetector.ForceStartInteraction();
        FoodInventoryUI.Instance.AddItem(SelectedDishData, 1);

        UIManager.Instance.ToggleCookingPanel(false);
        AudioManager.Instance.PlayInteractSound(8);

        OnDishCooked?.Invoke();
    }

    #endregion

    #region Public API

    public List<DishSlot> GetAllDishes() => spawnedDishSlots;

    public void SelectCurrentDish() => SelectDishAtIndex(selectedIndex);

    #endregion
}
