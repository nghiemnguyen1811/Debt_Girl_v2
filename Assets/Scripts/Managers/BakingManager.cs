using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using URandom = UnityEngine.Random;

/// <summary>
/// Handles cake selection, ingredient display, baking logic, and plate cooldown.
/// </summary>
public class BakingManager : SingletonMonobehaviour<BakingManager>
{
    public event Action OnCakeBaked;

    // ─────────────────────────────────────────────────────
    // UI REFERENCES
    // ─────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private Transform cakeListContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI selectedCakeName;
    [SerializeField] private TextMeshProUGUI bakeTimeText;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private Button bakeButtonEnabled;
    [SerializeField] private Button bakeButtonDisabled;

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
        InitializePlates();
        GenerateCakeSelectionList();
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
    /// Called when player level changes in GameManager.
    /// Refreshes all cake displays to unlock newly available cakes.
    /// </summary>
    public void RefreshCakeUnlockStates()
    {
        foreach (var cakeDisplay in spawnedCakeDisplays)
        {
            if (cakeDisplay == null) continue;
            cakeDisplay.EvaluateLockState();
        }

        if (selectedCake != null && selectedCake.IsLocked())
        {
            selectedCake = null;
            selectedIndex = -1;
            ResetUIState();
        }
    }

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
    /// Sets up listeners for UI buttons.
    /// </summary>
    private void SetupListeners()
    {
        bakeButtonEnabled?.onClick.AddListener(TryBakeSelectedCake);

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

        if (bakeButtonEnabled != null && bakeButtonDisabled != null)
        {
            bakeButtonEnabled.gameObject.SetActive(false);
            bakeButtonDisabled.gameObject.SetActive(true);
        }
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
            LocalizationManager.Instance.SetLocalizedText(selectedCakeName, "Cake Labels", SelectedCakeData.itemNameKey);

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
    /// Enables or disables the bake button depending on ingredient availability.
    /// </summary>
    private void UpdateBakeButtonState()
    {
        bool canBake = false;

        if (SelectedCakeData != null)
        {
            canBake = SelectedCakeData.requiredIngredients
                .All(ingredient => FoodInventoryUI.Instance.HasItems(ingredient));
        }

        // Set UI visibility in one place
        bakeButtonEnabled.gameObject.SetActive(canBake);
        bakeButtonDisabled.gameObject.SetActive(!canBake);
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

        AudioManager.Instance.PlayInteractSound(8);

        foreach (var plate in plateSlots)
        {
            if (!plate.IsEmpty()) continue;

            foreach (var ingredient in SelectedCakeData.requiredIngredients)
                for (int i = 0; i < ingredient.amount; i++)
                    FoodInventoryUI.Instance.RemoveItem(ingredient);

            plate.SetData(SelectedCakeData);
            warningText.gameObject.SetActive(false);

            AutoSave();
            SelectCakeAtIndex(selectedIndex);

            OnCakeBaked?.Invoke();
            return;
        }

        ShowWarningText(warningMessages[URandom.Range(0, warningMessages.Length)]);
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
    // Save/Load API
    // ─────────────────────────────────────────────────────

    public void ImportSaveData(SaveData data)
    {
        // If no data or no plate records exist → skip
        if (data == null || data.plates == null) return;

        foreach (var plateData in data.plates)
        {
            // Find the cake recipe that matches the saved item type
            var cake = allCakeRecipes.Find(c => c.itemType == plateData.itemType);
            if (cake == null) continue;

            // Find an empty plate slot to restore the saved cake
            var emptyPlate = plateSlots.Find(p => p.IsEmpty());
            if (emptyPlate == null) break;

            // Calculate elapsed time since last save
            var elapsed = (float)(DateTime.UtcNow.Ticks - plateData.lastBakeTimestamp) / TimeSpan.TicksPerSecond;

            // Remaining time after subtracting elapsed offline time
            float adjustedRemaining = plateData.remainingTime - elapsed;

            // Cake finished while offline → add directly to CakeInventory
            if (adjustedRemaining <= 0f)
                CakeInventoryUI.Instance.AddItem(cake, 1);

            // Cake still baking → restore plate with updated remaining time
            else emptyPlate.SetData(cake, adjustedRemaining);
        }
    }


    public void AutoSave()
    {
        if (SaveManager.Data == null) return;

        SaveManager.Data.plates.Clear();

        foreach (var plate in plateSlots)
        {
            if (!plate.IsEmpty())
            {
                SaveManager.Data.plates.Add(new PlateSaveData
                {
                    itemType = plate.GetCakeData().itemType,
                    remainingTime = plate.GetRemainingTime(),
                    lastBakeTimestamp = DateTime.UtcNow.Ticks
                });
            }
        }

        SaveManager.SaveGame();
    }

    private void OnApplicationQuit()
    {
        AutoSave();
    }


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
        /// Set plate data when baking a new cake (start with default crafting time).
        /// </summary>
        public void SetData(ItemDataSO data)
        {
            ApplyData(data, data.craftingTime);
        }

        /// <summary>
        /// Set plate data when loading from SaveData (use remaining time from save).
        /// </summary>
        public void SetData(ItemDataSO data, float customTime)
        {
            ApplyData(data, customTime);
        }

        /// <summary>
        /// Internal shared logic for setting plate data and updating UI.
        /// </summary>
        private void ApplyData(ItemDataSO data, float time)
        {
            cakeData = data;
            remainingTime = time;

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

                BakingManager.Instance.AutoSave();
            }

            else waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);
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

        /// <summary>
        /// Get current cake data on this plate.
        /// </summary>
        public ItemDataSO GetCakeData() => cakeData;

        /// <summary>
        /// Get remaining baking time for this plate.
        /// </summary>
        public float GetRemainingTime() => remainingTime;
    }
}
