using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Handles cake selection, ingredient display, baking logic, and plate cooldown.
/// </summary>
public class BakingManager : SingletonMonobehaviour<BakingManager>
{
    // ─────────────────────────────────────────────────────
    // UI REFERENCES
    // ─────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private Transform cakeListContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button bakeButton;
    [SerializeField] private TextMeshProUGUI selectedCakeName;
    [SerializeField] private TextMeshProUGUI bakeTimeText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Ingredient Display")]
    [SerializeField] private List<IngredientUI> ingredientSlots = new();
    [SerializeField] private List<GameObject> plusSignsBetweenIngredients = new();

    [Header("Plate Slots")]
    [SerializeField] private List<PlateUI> plateSlots = new();

    [Header("Cake Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    // ─────────────────────────────────────────────────────
    // DATA
    // ─────────────────────────────────────────────────────
    [Header("Cake Recipes")]
    [SerializeField] private List<ItemDataSO> allCakeRecipes;
    [SerializeField] private CakeDisplay cakeDisplayPrefab;

    [Header("Ingredient UI Colors")]
    [SerializeField] private UIColorsConfig uiColorsConfig;

    // ─────────────────────────────────────────────────────
    // ANIMATION SETTINGS
    // ─────────────────────────────────────────────────────
    [Header("Animation Settings")]
    [SerializeField] private float floatingTextFadeDuration = 2f;

    // ─────────────────────────────────────────────────────
    // INTERNAL CACHE
    // ─────────────────────────────────────────────────────
    private readonly List<CakeDisplay> spawnedCakeDisplays = new();
    private CakeDisplay selectedCake;
    private int selectedIndex = -1;
    private Sequence warningSequence;

    private ItemDataSO SelectedCakeData => selectedCake?.CakeData;

    private readonly string[] warningMessages = new string[]
    {
        "접시가 모두 가득 찼습니다!",
        "빈 접시가 없습니다!",
        "지금은 굽을 수 없습니다 — 모든 트레이가 사용 중입니다.",
        "빈 접시가 필요합니다!",
        "이런! 그 케이크를 놓을 공간이 없습니다.",
        "굽기 전에 접시를 비우세요.",
        "더 이상 케이크를 놓을 공간이 없습니다.",
        "접시가 꽉 찼습니다!",
        "사용할 슬롯이 없습니다!"
    };

    // ─────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        InitializeUI();
        InitializePlates();
        InitializeCakeSelection();
    }

    private void Update()
    {
        foreach (var plate in plateSlots)
            plate.UpdateTimer(Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────
    // INITIALIZATION
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes all UI, listeners, and resets states.
    /// </summary>
    private void InitializeUI()
    {
        SetupListeners();
        ResetUIState();
    }

    /// <summary>
    /// Initializes plate slots with UIColorsConfig.
    /// </summary>
    private void InitializePlates()
    {
        foreach (var plate in plateSlots)
            plate.Initialize(uiColorsConfig);
    }

    /// <summary>
    /// Initializes cake selection list.
    /// </summary>
    private void InitializeCakeSelection()
    {
        GenerateCakeSelectionList();
    }

    /// <summary>
    /// Sets up listeners for UI buttons.
    /// </summary>
    private void SetupListeners()
    {
        bakeButton?.onClick.AddListener(TryBakeSelectedCake);

        prevButton?.onClick.AddListener(() =>
        {
            int newIndex = selectedIndex - 1;
            SelectCakeAtIndex(newIndex);
            UpdateScrollPositionSmooth(newIndex); // chỉ scroll khi nhấn prev
        });

        nextButton?.onClick.AddListener(() =>
        {
            int newIndex = selectedIndex + 1;
            SelectCakeAtIndex(newIndex);
            UpdateScrollPositionSmooth(newIndex); // chỉ scroll khi nhấn next
        });
    }

    // ─────────────────────────────────────────────────────
    // UI STATE MANAGEMENT
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Resets warning, ingredient slots, plus signs, and bake UI.
    /// </summary>
    private void ResetUIState()
    {
        HideWarningImmediately();

        selectedCakeName?.SetText("");
        bakeTimeText?.SetText("00:00");

        ingredientSlots.ForEach(slot => slot.Hide());
        plusSignsBetweenIngredients.ForEach(plus => plus.SetActive(false));

        if (bakeButton != null) bakeButton.interactable = false;
    }

    /// <summary>
    /// Hides the warning text instantly at start.
    /// </summary>
    private void HideWarningImmediately()
    {
        warningText.DOKill();
        warningText.gameObject.SetActive(false);
        warningText.text = "";
    }

    // ─────────────────────────────────────────────────────
    // CAKE SELECTION LOGIC
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates cake selection list UI.
    /// </summary>
    private void GenerateCakeSelectionList()
    {
        ClearCakeList();

        var validCakes = allCakeRecipes.Where(IsValidCakeRecipe).ToList();
        for (int i = 0; i < validCakes.Count; i++)
        {
            var display = Instantiate(cakeDisplayPrefab, cakeListContainer);
            display.Initialize(validCakes[i]);

            int index = i;
            display.GetButton().onClick.AddListener(() => SelectCakeAtIndex(index));
            spawnedCakeDisplays.Add(display);
        }

        StartCoroutine(DelayScrollReset());
    }

    /// <summary>
    /// Clears existing cake display UI.
    /// </summary>
    private void ClearCakeList()
    {
        foreach (var display in spawnedCakeDisplays)
            if (display != null) Destroy(display.gameObject);

        spawnedCakeDisplays.Clear();
    }

    /// <summary>
    /// Validates cake recipe for display.
    /// </summary>
    private bool IsValidCakeRecipe(ItemDataSO item)
    {
        return item.itemType == ItemType.CraftedFood && item.canBeSold;
    }

    /// <summary>
    /// Resets scroll position after frame delay.
    /// </summary>
    private IEnumerator DelayScrollReset()
    {
        yield return new WaitUntil(() => scrollRect.gameObject.activeInHierarchy);
        yield return null;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    /// <summary>
    /// Selects a cake, updates UI and navigation buttons.
    /// </summary>
    private void SelectCakeAtIndex(int index)
    {
        // 1. Validate index
        if (index < 0 || index >= spawnedCakeDisplays.Count) return;
        if (spawnedCakeDisplays[index].IsLocked()) return;

        // 2. Update selected cake
        selectedCake = spawnedCakeDisplays[index];
        selectedIndex = index;

        foreach (var cake in spawnedCakeDisplays)
            cake.SetSelected(false);

        selectedCake.SetSelected(true);

        // 3. Update UI for ingredients, plus signs, bake time, name
        UpdateIngredientUI(SelectedCakeData);
        UpdatePlusSigns(SelectedCakeData);
        UpdateBakeTime(SelectedCakeData.craftingTime);

        if (selectedCakeName != null)
            selectedCakeName.text = SelectedCakeData.itemName;

        // 4. Update navigation buttons
        if (prevButton != null)
            prevButton.interactable = index > 0 && !spawnedCakeDisplays[index - 1].IsLocked();

        if (nextButton != null)
            nextButton.interactable = index < spawnedCakeDisplays.Count - 1 && !spawnedCakeDisplays[index + 1].IsLocked();

        // 5. Play feedback sound
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Updates scroll smoothly (used when pressing navigation buttons).
    /// </summary>
    private void UpdateScrollPositionSmooth(int index)
    {
        if (spawnedCakeDisplays.Count <= 1 || scrollRect == null) return;

        float targetPos = (float)index / (spawnedCakeDisplays.Count - 1);
        targetPos = Mathf.Clamp01(targetPos);

        // Kill any ongoing tween to avoid overlap
        DOTween.Kill(scrollRect);

        // Tween scroll movement smoothly
        DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            targetPos,
            0.3f // duration in seconds
        ).SetEase(Ease.OutCubic)
         .SetId(scrollRect); // assign tween id for Kill
    }

    /// <summary>
    /// Updates ingredient UI for the selected cake.
    /// </summary>
    private void UpdateIngredientUI(ItemDataSO cakeData)
    {
        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            if (i < cakeData.requiredIngredients.Count)
                ingredientSlots[i].SetData(cakeData.requiredIngredients[i], uiColorsConfig);
            else ingredientSlots[i].Hide();
        }

        UpdateBakeButtonState();
    }

    /// <summary>
    /// Updates plus sign visibility between ingredients.
    /// </summary>
    private void UpdatePlusSigns(ItemDataSO cakeData)
    {
        for (int i = 0; i < plusSignsBetweenIngredients.Count; i++)
            plusSignsBetweenIngredients[i].SetActive(i < cakeData.requiredIngredients.Count - 1);
    }

    /// <summary>
    /// Updates bake time text for selected cake.
    /// </summary>
    private void UpdateBakeTime(int totalSeconds)
    {
        bakeTimeText.text = DoubleUtilities.UpdateTime(totalSeconds);
    }

    /// <summary>
    /// Enables/disables bake button based on ingredient availability.
    /// </summary>
    private void UpdateBakeButtonState()
    {
        if (SelectedCakeData == null)
        {
            bakeButton.interactable = false;
            return;
        }

        bakeButton.interactable = SelectedCakeData.requiredIngredients
            .All(ingredient => FoodInventoryUI.Instance.HasItems(ingredient));
    }

    // ─────────────────────────────────────────────────────
    // BAKING LOGIC
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to bake selected cake and assigns it to a free plate.
    /// </summary>
    private void TryBakeSelectedCake()
    {
        if (SelectedCakeData == null) return;

        foreach (var plate in plateSlots)
        {
            if (!plate.IsEmpty()) continue;

            foreach (var ingredient in SelectedCakeData.requiredIngredients)
                for (int i = 0; i < ingredient.amount; i++)
                    FoodInventoryUI.Instance.RemoveItem(ingredient);

            plate.SetData(SelectedCakeData);
            warningText.gameObject.SetActive(false);

            SelectCakeAtIndex(selectedIndex);
            return;
        }

        ShowWarningText(warningMessages[Random.Range(0, warningMessages.Length)]);
        AudioManager.Instance.PlayInteractSound(8);
    }

    // ─────────────────────────────────────────────────────
    // WARNING SYSTEM
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Shows warning text with animation.
    /// </summary>
    private void ShowWarningText(string message)
    {
        if (warningSequence != null && warningSequence.IsActive())
            warningSequence.Kill();

        warningText.gameObject.SetActive(true);
        warningText.text = message;
        warningText.transform.localScale = Vector3.one;
        warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1f);

        warningSequence = DOTween.Sequence()
            .Append(warningText.transform.DOScale(Vector3.one * 1.2f, 0.1f).SetEase(Ease.OutBack))
            .Append(warningText.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack))
            .AppendInterval(0.5f)
            .Append(warningText.DOFade(0f, floatingTextFadeDuration).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                warningText.gameObject.SetActive(false);
                warningText.text = "";
                warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1f);
            });
    }

    // ─────────────────────────────────────────────────────
    // PUBLIC ACCESSORS
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns all spawned cake displays.
    /// </summary>
    public List<CakeDisplay> GetAllCakeDisplays() => spawnedCakeDisplays;

    /// <summary>
    /// Reselects current cake by index.
    /// </summary>
    public void SelectCurrentCake() => SelectCakeAtIndex(selectedIndex);

    // ─────────────────────────────────────────────────────
    // PLATE UI CLASS
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Plate UI representation and countdown logic.
    /// </summary>
    [System.Serializable]
    public class PlateUI
    {
        // UI REFERENCES
        public Image cakeImage;
        public TextMeshProUGUI waitTimeText;
        public Image timerFrame;

        // COLORS
        private UIColorsConfig colors;

        // RUNTIME DATA
        private ItemDataSO cakeData;
        private float remainingTime;

        /// <summary>
        /// Initializes plate with color config.
        /// </summary>
        public void Initialize(UIColorsConfig config)
        {
            colors = config;
            Clear();
        }

        /// <summary>
        /// Sets cake data and starts countdown.
        /// </summary>
        public void SetData(ItemDataSO data)
        {
            cakeData = data;
            remainingTime = cakeData.craftingTime;

            cakeImage.sprite = cakeData.icon;
            cakeImage.gameObject.SetActive(true);
            waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);

            if (timerFrame != null)
                timerFrame.color = colors.plateOccupiedColor;
        }

        /// <summary>
        /// Updates timer countdown each frame.
        /// </summary>
        public void UpdateTimer(float deltaTime)
        {
            if (IsEmpty()) return;

            remainingTime -= deltaTime;

            if (remainingTime <= 0f)
            {
                CakeInventoryUI.Instance.AddItem(cakeData, 1);
                Clear();
            }
            else
            {
                waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);
            }
        }

        /// <summary>
        /// Clears plate data and resets UI.
        /// </summary>
        private void Clear()
        {
            cakeImage.gameObject.SetActive(false);
            waitTimeText.text = "-";
            cakeData = null;
            remainingTime = 0f;

            if (timerFrame != null)
                timerFrame.color = colors.plateEmptyColor;
        }

        /// <summary>
        /// Checks if plate is empty.
        /// </summary>
        public bool IsEmpty() => cakeData == null;
    }
}
