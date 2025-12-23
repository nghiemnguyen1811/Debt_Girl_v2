using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the baking system: selecting recipes, checking ingredients,
/// baking on plates, and handling bake timers.
/// </summary>
public class BakingManager : SingletonMonobehaviour<BakingManager>
{
    public event Action OnCakeBaked;

    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields: UI References ===

    [Header("Main UI")]
    [SerializeField] private Transform cakeListContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI selectedCakeName;
    [SerializeField] private TextMeshProUGUI bakeTimeText;

    [Header("Action Buttons")]
    [SerializeField] private Button bakeButtonEnabled;
    [SerializeField] private Button bakeButtonDisabled;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Ingredients & Plates")]
    [SerializeField] private List<IngredientUI> ingredientSlots = new();
    [SerializeField] private List<GameObject> plusSignsBetweenIngredients = new();
    [SerializeField] private List<PlateUI> plateSlots = new();

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields: Data ===

    [Header("Data & Config")]
    [SerializeField] private List<ItemDataSO> allCakeRecipes;
    [SerializeField] private CakeDisplay cakeDisplayPrefab;
    [SerializeField] private UIColorsConfig uiColorsConfig;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Internal State ===

    private readonly List<CakeDisplay> spawnedCakeDisplays = new();
    private CakeDisplay selectedCake;
    private int selectedIndex = -1;

    /// <summary>Shortcut to get the currently selected cake data.</summary>
    private ItemDataSO SelectedCakeData => selectedCake?.CakeData;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Events ===

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
        // Update timer for all active plates
        foreach (var plate in plateSlots)
            plate.UpdateTimer(Time.deltaTime);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Initialization & Core Logic ===

    /// <summary>
    /// Re-evaluates which cakes are locked/unlocked based on player level.
    /// </summary>
    public void RefreshCakeUnlockStates()
    {
        foreach (var display in spawnedCakeDisplays)
            if (display != null) display.EvaluateLockState();

        // Deselect if current cake becomes locked
        if (selectedCake != null && selectedCake.IsLocked())
        {
            selectedCake = null;
            selectedIndex = -1;
            ResetUIState();
        }
    }

    /// <summary>
    /// Attempts to bake the selected cake if ingredients are sufficient and a plate is free.
    /// </summary>
    private void TryBakeSelectedCake()
    {
        if (SelectedCakeData == null) return;
        AudioManager.Instance.PlayInteractSound(8);

        // Check for empty plate
        foreach (var plate in plateSlots)
        {
            if (!plate.IsEmpty()) continue;

            // Consume ingredients
            foreach (var ing in SelectedCakeData.requiredIngredients)
                for (int i = 0; i < ing.amount; i++)
                    FoodInventoryUI.Instance.RemoveItem(ing);

            // Assign to plate and start baking
            plate.SetData(SelectedCakeData);
            AutoSave();

            // Refresh UI
            SelectCakeAtIndex(selectedIndex);
            OnCakeBaked?.Invoke();
            return;
        }

        // No plates available -> Show Warning via GameManager
        GameManager.Instance.ShowPlateFullWarning();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === UI Setup & Helpers ===

    private void InitializeUI()
    {
        bakeButtonEnabled?.onClick.AddListener(TryBakeSelectedCake);

        prevButton?.onClick.AddListener(() => {
            SelectCakeAtIndex(selectedIndex - 1);
            UpdateScrollPositionSmooth(selectedIndex);
        });

        nextButton?.onClick.AddListener(() => {
            SelectCakeAtIndex(selectedIndex + 1);
            UpdateScrollPositionSmooth(selectedIndex);
        });

        ResetUIState();
    }

    private void ResetUIState()
    {
        selectedCakeName?.SetText("");
        bakeTimeText?.SetText("00:00");
        ingredientSlots.ForEach(slot => slot.Hide());
        plusSignsBetweenIngredients.ForEach(plus => plus.SetActive(false));

        if (bakeButtonEnabled) bakeButtonEnabled.gameObject.SetActive(false);
        if (bakeButtonDisabled) bakeButtonDisabled.gameObject.SetActive(true);
    }

    private void InitializePlates()
    {
        foreach (var p in plateSlots) p.Initialize(uiColorsConfig);
    }

    /// <summary>Creates the scrollable list of cake recipes.</summary>
    private void GenerateCakeSelectionList()
    {
        // Cleanup old
        foreach (var d in spawnedCakeDisplays) if (d) Destroy(d.gameObject);
        spawnedCakeDisplays.Clear();

        // Filter valid recipes
        var validCakes = allCakeRecipes.Where(x => x.itemType == ItemType.CraftedFood && x.canBeSold).ToList();

        // Create new
        for (int i = 0; i < validCakes.Count; i++)
        {
            var display = Instantiate(cakeDisplayPrefab, cakeListContainer);
            display.Initialize(validCakes[i]);

            int idx = i;
            display.GetButton().onClick.AddListener(() => SelectCakeAtIndex(idx));
            spawnedCakeDisplays.Add(display);
        }

        StartCoroutine(DelayScrollReset());
    }

    private IEnumerator DelayScrollReset()
    {
        yield return new WaitUntil(() => scrollRect.gameObject.activeInHierarchy);
        yield return null;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Selection Logic ===

    private void SelectCakeAtIndex(int index)
    {
        if (index < 0 || index >= spawnedCakeDisplays.Count) return;
        if (spawnedCakeDisplays[index].IsLocked()) return;

        // Update Selection Highlighting
        selectedCake = spawnedCakeDisplays[index];
        selectedIndex = index;
        foreach (var c in spawnedCakeDisplays) c.SetSelected(false);
        selectedCake.SetSelected(true);

        // Update Info UI
        UpdateIngredientUI(SelectedCakeData);
        UpdatePlusSigns(SelectedCakeData);
        UpdateBakeTime(SelectedCakeData.craftingTime);

        if (selectedCakeName)
            LocalizationManager.Instance.SetLocalizedText(selectedCakeName, "Cake Labels", SelectedCakeData.itemNameKey);

        // Update Nav Buttons
        if (prevButton) prevButton.interactable = index > 0 && !spawnedCakeDisplays[index - 1].IsLocked();
        if (nextButton) nextButton.interactable = index < spawnedCakeDisplays.Count - 1 && !spawnedCakeDisplays[index + 1].IsLocked();

        AudioManager.Instance.PlayInteractSound(8);
    }

    private void UpdateScrollPositionSmooth(int index)
    {
        if (spawnedCakeDisplays.Count <= 1 || !scrollRect) return;

        float target = Mathf.Clamp01((float)index / (spawnedCakeDisplays.Count - 1));

        DOTween.Kill(scrollRect);
        DOTween.To(() => scrollRect.horizontalNormalizedPosition, x => scrollRect.horizontalNormalizedPosition = x, target, 0.3f)
            .SetEase(Ease.OutCubic)
            .SetId(scrollRect);
    }

    private void UpdateIngredientUI(ItemDataSO data)
    {
        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            if (i < data.requiredIngredients.Count)
                ingredientSlots[i].SetData(data.requiredIngredients[i], uiColorsConfig);
            else
                ingredientSlots[i].Hide();
        }
        UpdateBakeButtonState();
    }

    private void UpdatePlusSigns(ItemDataSO data)
    {
        for (int i = 0; i < plusSignsBetweenIngredients.Count; i++)
            plusSignsBetweenIngredients[i].SetActive(i < data.requiredIngredients.Count - 1);
    }

    private void UpdateBakeTime(int sec) => bakeTimeText.text = DoubleUtilities.UpdateTime(sec);

    private void UpdateBakeButtonState()
    {
        bool canBake = SelectedCakeData != null &&
                       SelectedCakeData.requiredIngredients.All(ing => FoodInventoryUI.Instance.HasItems(ing));

        bakeButtonEnabled.gameObject.SetActive(canBake);
        bakeButtonDisabled.gameObject.SetActive(!canBake);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Save / Load System ===

    public void ImportSaveData(SaveData data)
    {
        if (data?.plates == null) return;

        foreach (var pData in data.plates)
        {
            var cake = allCakeRecipes.Find(c => c.itemType == pData.itemType);
            if (cake == null) continue;

            var emptyPlate = plateSlots.Find(p => p.IsEmpty());
            if (emptyPlate == null) break;

            float elapsed = (float)(DateTime.UtcNow.Ticks - pData.lastBakeTimestamp) / TimeSpan.TicksPerSecond;
            float remaining = pData.remainingTime - elapsed;

            if (remaining <= 0)
                CakeInventoryUI.Instance.AddItem(cake, 1);
            else
                emptyPlate.SetData(cake, remaining);
        }
    }

    public void AutoSave()
    {
        if (SaveManager.Data == null) return;

        SaveManager.Data.plates.Clear();
        foreach (var p in plateSlots)
        {
            if (!p.IsEmpty())
            {
                SaveManager.Data.plates.Add(new PlateSaveData
                {
                    itemType = p.GetCakeData().itemType,
                    remainingTime = p.GetRemainingTime(),
                    lastBakeTimestamp = DateTime.UtcNow.Ticks
                });
            }
        }
        SaveManager.SaveGame();
    }

    private void OnApplicationQuit() => AutoSave();

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Public Accessors ===

    /// <summary>
    /// Returns all spawned cake displays.
    /// </summary>
    public List<CakeDisplay> GetAllCakeDisplays() => spawnedCakeDisplays;

    /// <summary>
    /// Reselects current cake by index. Called by UIManager or other systems to refresh UI.
    /// </summary>
    public void SelectCurrentCake() => SelectCakeAtIndex(selectedIndex);

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === PlateUI Class Definition ===

    [System.Serializable]
    public class PlateUI
    {
        public Image cakeImage;
        public TextMeshProUGUI waitTimeText;
        public Image timerFrame;

        private UIColorsConfig colors;
        private ItemDataSO cakeData;
        private float remainingTime;

        public void Initialize(UIColorsConfig c) { colors = c; Clear(); }

        public void SetData(ItemDataSO d, float time = -1)
        {
            ApplyData(d, time < 0 ? d.craftingTime : time);
        }

        private void ApplyData(ItemDataSO d, float t)
        {
            cakeData = d;
            remainingTime = t;
            cakeImage.sprite = d.icon;
            cakeImage.gameObject.SetActive(true);
            waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);

            if (timerFrame) timerFrame.color = colors.plateOccupiedColor;
        }

        public void UpdateTimer(float dt)
        {
            if (IsEmpty()) return;

            remainingTime -= dt;
            if (remainingTime <= 0)
            {
                CakeInventoryUI.Instance.AddItem(cakeData, 1);
                Clear();
                BakingManager.Instance.AutoSave();
            }
            else
            {
                waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);
            }
        }

        private void Clear()
        {
            cakeImage.gameObject.SetActive(false);
            waitTimeText.text = "-";
            cakeData = null;
            remainingTime = 0;

            if (timerFrame) timerFrame.color = colors.plateEmptyColor;
        }

        public bool IsEmpty() => cakeData == null;
        public ItemDataSO GetCakeData() => cakeData;
        public float GetRemainingTime() => remainingTime;
    }
    #endregion
}