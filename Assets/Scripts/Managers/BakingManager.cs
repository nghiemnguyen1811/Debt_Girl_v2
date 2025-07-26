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
    [SerializeField] private TextMeshProUGUI bakeTimeText;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Ingredient Display")]
    [SerializeField] private List<IngredientUI> ingredientSlots = new();
    [SerializeField] private List<GameObject> plusSignsBetweenIngredients = new();

    [Header("Plate Slots")]
    [SerializeField] private List<PlateUI> plateSlots = new();

    // ─────────────────────────────────────────────────────
    // DATA
    // ─────────────────────────────────────────────────────
    [Header("Cake Recipes")]
    [SerializeField] private List<ItemDataSO> allCakeRecipes;
    [SerializeField] private CakeDisplay cakeDisplayPrefab;

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
        "All plates are full!",
        "No empty plate available!",
        "Can't bake now — all trays are used.",
        "You need a free plate!",
        "Oops! No room for that cake.",
        "Clear a plate before baking.",
        "No space left for more cakes.",
        "Your plates are packed!",
        "No slot available!"
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
    /// Initializes UI elements, listeners, and resets initial states.
    /// </summary>
    private void InitializeUI()
    {
        SetupListeners();
        ResetUIState();
    }

    /// <summary>
    /// Clears all plate slots at the beginning of the game.
    /// </summary>
    private void InitializePlates()
    {
        foreach (var plate in plateSlots)
            plate.Clear();
    }

    /// <summary>
    /// Generates the cake selection UI list from valid recipes.
    /// </summary>
    private void InitializeCakeSelection()
    {
        GenerateCakeSelectionList();
    }

    /// <summary>
    /// Set up listeners for UI buttons.
    /// </summary>
    private void SetupListeners()
    {
        bakeButton?.onClick.AddListener(TryBakeSelectedCake);
    }

    // ─────────────────────────────────────────────────────
    // UI STATE MANAGEMENT
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Resets warning, ingredient slots, and plus signs visibility.
    /// </summary>
    private void ResetUIState()
    {
        HideWarningImmediately();

        foreach (var slot in ingredientSlots)
            slot.Hide();

        foreach (var plus in plusSignsBetweenIngredients)
            plus.SetActive(false);
    }

    /// <summary>
    /// Hides the warning text immediately on game start.
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
    /// Generates the cake selection UI list from valid recipes.
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
    /// Destroys existing cake display objects.
    /// </summary>
    private void ClearCakeList()
    {
        foreach (var display in spawnedCakeDisplays)
            if (display != null) Destroy(display.gameObject);

        spawnedCakeDisplays.Clear();
    }

    /// <summary>
    /// Checks whether the item is a valid crafted cake recipe.
    /// </summary>
    private bool IsValidCakeRecipe(ItemDataSO item)
    {
        return item.itemType == ItemType.CraftedFood && item.canBeSold;
    }

    /// <summary>
    /// Resets scroll view position after frame delay.
    /// </summary>
    private IEnumerator DelayScrollReset()
    {
        yield return null;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    /// <summary>
    /// Selects a cake from the list and updates UI.
    /// </summary>
    private void SelectCakeAtIndex(int index)
    {
        if (spawnedCakeDisplays[index].IsLocked()) return;

        selectedCake = spawnedCakeDisplays[index];
        selectedIndex = index;

        foreach (var cake in spawnedCakeDisplays)
            cake.SetSelected(false);

        selectedCake.SetSelected(true);

        UpdateIngredientUI(SelectedCakeData);
        UpdatePlusSigns(SelectedCakeData);
        UpdateBakeTime(SelectedCakeData.craftingTime);
    }

    /// <summary>
    /// Updates ingredient UI based on selected cake.
    /// </summary>
    private void UpdateIngredientUI(ItemDataSO cakeData)
    {
        for (int i = 0; i < ingredientSlots.Count; i++)
        {
            if (i < cakeData.requiredIngredients.Count)
                ingredientSlots[i].SetData(cakeData.requiredIngredients[i]);
            else
                ingredientSlots[i].Hide();
        }

        UpdateBakeButtonState();
    }

    /// <summary>
    /// Updates the plus signs visibility between ingredients.
    /// </summary>
    private void UpdatePlusSigns(ItemDataSO cakeData)
    {
        for (int i = 0; i < plusSignsBetweenIngredients.Count; i++)
            plusSignsBetweenIngredients[i].SetActive(i < cakeData.requiredIngredients.Count - 1);
    }

    /// <summary>
    /// Updates the bake time UI based on the selected recipe.
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
        if (SelectedCakeData == null)
        {
            bakeButton.interactable = false;
            return;
        }

        bakeButton.interactable = SelectedCakeData.requiredIngredients
            .All(ingredient => Inventory.Instance.HasItems(ingredient));
    }

    // ─────────────────────────────────────────────────────
    // BAKING LOGIC
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Tries to bake the selected cake and assign it to the first available plate.
    /// </summary>
    private void TryBakeSelectedCake()
    {
        if (SelectedCakeData == null) return;

        foreach (var plate in plateSlots)
        {
            if (!plate.IsEmpty()) continue;

            foreach (var ingredient in SelectedCakeData.requiredIngredients)
                for (int i = 0; i < ingredient.amount; i++)
                    Inventory.Instance.RemoveItem(ingredient);

            plate.SetData(SelectedCakeData);
            warningText.gameObject.SetActive(false);

            Inventory.Instance.AddItem(SelectedCakeData, 1);
            SelectCakeAtIndex(selectedIndex);
            return;
        }

        ShowWarningText(warningMessages[Random.Range(0, warningMessages.Length)]);
    }

    // ─────────────────────────────────────────────────────
    // WARNING SYSTEM
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Shows animated warning message with fade and scale.
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
    /// Returns a list of all spawned cake displays.
    /// </summary>
    public List<CakeDisplay> GetAllCakeDisplays() => spawnedCakeDisplays;

    // ─────────────────────────────────────────────────────
    // PLATE UI CLASS
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Plate UI representation and countdown logic.
    /// </summary>
    [System.Serializable]
    public class PlateUI
    {
        public Image cakeImage;
        public TextMeshProUGUI waitTimeText;

        private ItemDataSO cakeData;
        private float remainingTime;

        /// <summary>
        /// Assigns data and starts countdown for plate.
        /// </summary>
        public void SetData(ItemDataSO data)
        {
            cakeData = data;
            remainingTime = cakeData.craftingTime;
            cakeImage.sprite = cakeData.icon;
            cakeImage.gameObject.SetActive(true);
            waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);
        }

        /// <summary>
        /// Updates the timer each frame.
        /// </summary>
        public void UpdateTimer(float deltaTime)
        {
            if (IsEmpty()) return;

            remainingTime -= deltaTime;
            if (remainingTime <= 0f) Clear();
            else waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);
        }

        /// <summary>
        /// Clears the plate UI and data.
        /// </summary>
        public void Clear()
        {
            cakeImage.gameObject.SetActive(false);
            waitTimeText.text = "";
            cakeData = null;
            remainingTime = 0f;
        }

        /// <summary>
        /// Checks if the plate has no assigned data.
        /// </summary>
        public bool IsEmpty() => cakeData == null;
    }
}
