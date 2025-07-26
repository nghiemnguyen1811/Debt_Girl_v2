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
    [SerializeField] private RectTransform lockStateVisualGroup;

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
        cakeNameLabel.text = itemData.itemName;

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
    private void EvaluateLockState()
    {
        int requiredLevel = itemData.requiredLevel;
        isLocked = GameManager.Instance.CurrentLevel < requiredLevel;
        UpdateLockVisuals(isLocked);
    }

    /// <summary>
    /// Show locked/unlocked UI state.
    /// </summary>
    private void UpdateLockVisuals(bool isLocked)
    {
        if (lockStateVisualGroup == null || lockStateVisualGroup.childCount < 2)
        {
            Debug.LogWarning("Lock visual group is not properly configured.");
            return;
        }

        lockStateVisualGroup.GetChild(0).gameObject.SetActive(!isLocked);
        lockStateVisualGroup.GetChild(1).gameObject.SetActive(isLocked);

        selectButton.interactable = !isLocked;
    }
}
