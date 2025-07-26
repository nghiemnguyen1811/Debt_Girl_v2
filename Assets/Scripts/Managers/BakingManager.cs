using System.Collections.Generic;
using System.Linq;
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
    // UI References
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

    [Header("Animation Settings")]
    [SerializeField] private float floatingTextFadeDuration = 2f;

    // ─────────────────────────────────────────────────────
    // Cake Data
    // ─────────────────────────────────────────────────────

    [Header("Cake Recipes")]
    [SerializeField] private List<ItemDataSO> allCakeRecipes;
    [SerializeField] private CakeDisplay cakeDisplayPrefab;

    private readonly List<CakeDisplay> spawnedCakeDisplays = new();
    private CakeDisplay selectedCake;
    private int selectedIndex = -1;

    // ─────────────────────────────────────────────────────
    // Warning Messages
    // ─────────────────────────────────────────────────────

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
    // Unity Events
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        SetupListeners();
        GenerateCakeSelectionList();
        HideWarningTextImmediately();
        ClearAllPlatesAtStart();
    }

    private void Update()
    {
        foreach (var plate in plateSlots)
            plate.UpdateTimer(Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────
    // Initialization
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Setup UI listeners like bake button.
    /// </summary>
    private void SetupListeners()
    {
        if (bakeButton != null)
            bakeButton.onClick.AddListener(TryBakeSelectedCake);
    }

    /// <summary>
    /// Populate the cake selection list with valid recipes.
    /// </summary>
    private void GenerateCakeSelectionList()
    {
        ClearCakeList();

        var validCakes = allCakeRecipes.Where(IsValidCakeRecipe).ToList();

        for (int i = 0; i < validCakes.Count; i++)
        {
            var cakeDisplay = Instantiate(cakeDisplayPrefab, cakeListContainer);
            cakeDisplay.Initialize(validCakes[i]);

            int index = i;
            cakeDisplay.GetButton().onClick.AddListener(() => SelectCakeAtIndex(index));
            spawnedCakeDisplays.Add(cakeDisplay);
        }

        ResetScrollToStart();
    }

    /// <summary>
    /// Remove old cake display objects.
    /// </summary>
    private void ClearCakeList()
    {
        foreach (var display in spawnedCakeDisplays)
        {
            if (display != null)
                Destroy(display.gameObject);
        }

        spawnedCakeDisplays.Clear();
    }

    /// <summary>
    /// Check if the item is a valid cake recipe.
    /// </summary>
    private bool IsValidCakeRecipe(ItemDataSO item)
    {
        return item.itemType == ItemType.CraftedFood && item.canBeSold;
    }

    /// <summary>
    /// Reset horizontal scroll view to the beginning.
    /// </summary>
    private void ResetScrollToStart()
    {
        StartCoroutine(DelayScrollReset());
    }

    private System.Collections.IEnumerator DelayScrollReset()
    {
        yield return null;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    /// <summary>
    /// Hide warning text immediately at start.
    /// </summary>
    private void HideWarningTextImmediately()
    {
        if (warningText == null) return;

        warningText.DOKill();
        warningText.gameObject.SetActive(false);
        warningText.text = string.Empty;
    }

    /// <summary>
    /// Clear all plates on game start.
    /// </summary>
    private void ClearAllPlatesAtStart()
    {
        foreach (var plate in plateSlots)
            plate.Clear();
    }

    // ─────────────────────────────────────────────────────
    // Cake Selection Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Handle cake selection UI and ingredient display.
    /// </summary>
    private void SelectCakeAtIndex(int index)
    {
        if (spawnedCakeDisplays[index].IsLocked()) return;

        selectedCake = spawnedCakeDisplays[index];
        selectedIndex = index;

        DeselectAllCakes();
        selectedCake.SetSelected(true);

        var data = selectedCake.CakeData;

        UpdateIngredientUI(data);
        UpdatePlusSigns(data);
        UpdateBakeTime(data.craftingTime);
    }

    /// <summary>
    /// Remove selection highlight from all cakes.
    /// </summary>
    private void DeselectAllCakes()
    {
        foreach (var cake in spawnedCakeDisplays)
            cake.SetSelected(false);
    }

    // ─────────────────────────────────────────────────────
    // UI Updating
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Update the ingredient slots based on the selected cake's recipe.
    /// Shows each required ingredient, hides unused slots.
    /// Also updates the bake button state based on inventory availability.
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
    /// Activate or deactivate the plus signs between ingredients,
    /// depending on how many ingredients are shown.
    /// </summary>
    private void UpdatePlusSigns(ItemDataSO cakeData)
    {
        for (int i = 0; i < plusSignsBetweenIngredients.Count; i++)
            plusSignsBetweenIngredients[i].SetActive(i < cakeData.requiredIngredients.Count - 1);
    }

    /// <summary>
    /// Update the displayed baking time for the selected cake.
    /// Uses a formatted time string from DoubleUtilities.
    /// </summary>
    private void UpdateBakeTime(int totalSeconds)
    {
        bakeTimeText.text = DoubleUtilities.UpdateTime(totalSeconds);
    }

    /// <summary>
    /// Enable the bake button only if the player has all required ingredients.
    /// </summary>
    private void UpdateBakeButtonState()
    {
        bool hasAllIngredients = selectedCake.CakeData.requiredIngredients
            .All(ingredient => Inventory.Instance.HasItems(ingredient));

        bakeButton.interactable = hasAllIngredients;
    }

    // ─────────────────────────────────────────────────────
    // Baking Logic
    // ─────────────────────────────────────────────────────

    private void TryBakeSelectedCake()
    {
        foreach (var plate in plateSlots)
        {
            if (plate.IsEmpty())
            {
                plate.SetData(selectedCake.CakeData);
                warningText.gameObject.SetActive(false);
                return;
            }
        }

        string warning = warningMessages[Random.Range(0, warningMessages.Length)];
        ShowWarningText(warning);
    }

    /// <summary>
    /// Show animated warning text with DOTween.
    /// </summary>
    private void ShowWarningText(string message)
    {
        if (warningText == null) return;

        warningText.text = message;
        warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1f);
        warningText.transform.localScale = Vector3.one * 1.2f;
        warningText.gameObject.SetActive(true);

        DOTween.Kill(warningText);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(warningText.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        sequence.AppendInterval(0.5f);
        sequence.Append(warningText.DOFade(0f, floatingTextFadeDuration).SetEase(Ease.InOutQuad));
        sequence.OnComplete(() =>
        {
            warningText.gameObject.SetActive(false);
            warningText.text = string.Empty;
        });
    }

    // ─────────────────────────────────────────────────────
    // Public Access
    // ─────────────────────────────────────────────────────

    public List<CakeDisplay> GetAllCakeDisplays() => spawnedCakeDisplays;

    // ─────────────────────────────────────────────────────
    // Nested PlateUI Class
    // ─────────────────────────────────────────────────────

    [System.Serializable]
    public class PlateUI
    {
        public Image cakeImage;
        public TextMeshProUGUI waitTimeText;

        private ItemDataSO cakeData;
        private float remainingTime;

        /// <summary>
        /// Assign a cake to this plate and start countdown.
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
        /// Decrease timer, and clear plate when finished.
        /// </summary>
        public void UpdateTimer(float deltaTime)
        {
            if (IsEmpty()) return;

            remainingTime -= deltaTime;

            if (remainingTime <= 0f) Clear();
            else waitTimeText.text = DoubleUtilities.UpdateTime((int)remainingTime);
        }

        /// <summary>
        /// Clear the plate and reset data.
        /// </summary>
        public void Clear()
        {
            cakeImage.gameObject.SetActive(false);
            waitTimeText.text = string.Empty;
            cakeData = null;
            remainingTime = 0f;
        }

        /// <summary>
        /// Check if this plate is currently empty.
        /// </summary>
        public bool IsEmpty() => cakeData == null;
    }
}
