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
    private FloorDataSO floorData;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI floorNameLabel;
    [SerializeField] private GameObject outline;

    [Header("Behaviour")]
    [SerializeField] private bool autoRefreshOnStart = true;

    [Header("Runtime Cache")]
    [SerializeField] private Button cachedButton;

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
    public Button GetButton()
    {
        if (!cachedButton) cachedButton = GetComponent<Button>();
        return cachedButton;
    }

    public FloorDataSO GetFloorAsset() => floorData;

    public void SetFloor(FloorDataSO data)
    {
        floorData = data;
        RefreshUI();
    }

    public void SetInteractable(bool interactable)
    {
        var btn = GetButton();
        if (btn) btn.interactable = interactable;
    }

    public void SetOutlineActive(bool active)
    {
        if (outline) outline.SetActive(active);
    }

    public void SetLabelColor(Color color)
    {
        if (floorNameLabel) floorNameLabel.color = color;
    }

    // ─────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────
    private void RefreshUI()
    {
        if (!floorNameLabel) return;

        if (!floorData)
        {
            floorNameLabel.text = "(No Floor)";
            return;
        }

        floorNameLabel.text = BuildDisplayName(floorData);
    }

    private static string BuildDisplayName(FloorDataSO data)
    {
        if (!data) return "(No Floor)";
        return string.IsNullOrWhiteSpace(data.floorName)
            ? data.floorType.ToString()
            : data.floorName;
    }
}
