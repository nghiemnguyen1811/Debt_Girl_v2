using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CakeDisplay : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Serialized Fields
    // ─────────────────────────────────────────────────────

    [Header("Visual Components")]
    [SerializeField] private Image cakeIconImage;
    [SerializeField] private TextMeshProUGUI cakeNameLabel;
    [SerializeField] private Button selectButton;

    [Header("Lock State Visuals")]
    [SerializeField] private RectTransform cakeDisplayTransform;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────

    private ItemDataSO itemData;
    private bool isLocked;
    private Outline selectionOutline;

    // ─────────────────────────────────────────────────────
    // Public Properties
    // ─────────────────────────────────────────────────────

    public ItemDataSO CakeData => itemData;
    public Button GetButton() => selectButton;
    public bool IsLocked() => isLocked;

    // ─────────────────────────────────────────────────────
    // MonoBehaviour
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        selectionOutline = cakeIconImage.GetComponent<Outline>();
    }

    // [FIX] Use OnEnable to ensure state updates every time the panel opens
    private void OnEnable()
    {
        // Register for level change events
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged += EvaluateLockState;

        // Force check immediately in case level changed while this object was disabled
        EvaluateLockState();
    }

    private void OnDisable()
    {
        // Unregister to prevent memory leaks
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= EvaluateLockState;
    }

    // ─────────────────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Assign data and visuals to this display.
    /// </summary>
    public void Initialize(ItemDataSO newItemData)
    {
        itemData = newItemData;

        cakeIconImage.sprite = itemData.icon;
        LocalizationManager.Instance.SetLocalizedText(cakeNameLabel, "Cake Labels", itemData.itemNameKey);

        EvaluateLockState();
        SetSelected(false);
    }

    /// <summary>
    /// Highlight or unhighlight the display.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = isSelected;
    }

    // ─────────────────────────────────────────────────────
    // Lock State Logic
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Determines if the item is locked based on player level.
    /// </summary>
    public void EvaluateLockState()
    {
        // Safety check
        if (itemData == null) return;

        int requiredLevel = itemData.requiredLevel;
        isLocked = GameManager.Instance.CurrentLevel < requiredLevel;
        UpdateLockVisuals(isLocked);
    }

    /// <summary>
    /// Show locked/unlocked UI state.
    /// </summary>
    private void UpdateLockVisuals(bool isLocked)
    {
        if (cakeDisplayTransform == null || cakeDisplayTransform.childCount < 2)
        {
            Debug.LogWarning("Lock visual group is not properly configured.");
            return;
        }

        cakeDisplayTransform.GetChild(0).gameObject.SetActive(!isLocked);
        cakeDisplayTransform.GetChild(1).gameObject.SetActive(isLocked);

        selectButton.interactable = !isLocked;
    }
}