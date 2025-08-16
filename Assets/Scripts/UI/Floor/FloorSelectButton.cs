using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class FloorSelectButton : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("Data")]
    [Tooltip("ScriptableObject describing this floor.")]
    private FloorDataSO floorAsset;

    [Header("UI")]
    [Tooltip("Text label showing the floor's display name.")]
    [SerializeField] private TextMeshProUGUI floorNameLabel;

    [Header("Runtime Cache")]
    [Tooltip("Button reference. Drag in manually or it will auto-assign.")]
    [SerializeField] private Button cachedButton;

    [Header("Behaviour")]
    [Tooltip("If true, the label will refresh on Start/Validate.")]
    [SerializeField] private bool autoRefreshOnStart = true;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        if (autoRefreshOnStart) RefreshUI();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep references and label preview in sync while editing
        if (!cachedButton) cachedButton = GetComponent<Button>();
        if (autoRefreshOnStart) RefreshUI();
    }
#endif

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────
    /// <summary>Returns the underlying Unity Button.</summary>
    public Button GetButton()
    {
        if (!cachedButton) cachedButton = GetComponent<Button>();
        return cachedButton;
    }

    /// <summary>Returns the currently assigned floor asset.</summary>
    public FloorDataSO GetFloorAsset() => floorAsset;

    /// <summary>Assigns the floor asset and refreshes visuals.</summary>
    public void SetFloor(FloorDataSO asset)
    {
        floorAsset = asset;
        RefreshUI();
    }

    /// <summary>Enables/disables user interaction.</summary>
    public void SetInteractable(bool interactable)
    {
        var btn = GetButton();
        if (btn) btn.interactable = interactable;
    }

    // ─────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────
    private void RefreshUI()
    {
        if (!floorNameLabel) return;

        if (!floorAsset)
        {
            floorNameLabel.text = "(No Floor)";
            return;
        }

        floorNameLabel.text = BuildDisplayName(floorAsset);
    }

    private static string BuildDisplayName(FloorDataSO asset)
    {
        if (!asset) return "(No Floor)";
        return string.IsNullOrWhiteSpace(asset.floorName)
            ? asset.floorType.ToString()
            : asset.floorName;
    }
}
