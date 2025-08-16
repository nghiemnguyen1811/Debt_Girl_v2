using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class FloorSelectionManager : SingletonMonobehaviour<FloorSelectionManager>
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("Floor Button Prefab & Root")]
    [SerializeField] private FloorSelectButton floorButtonPrefab;
    [SerializeField] private Transform floorButtonsRoot;

    [Header("Floors")]
    [Tooltip("List of floors in the order you want them displayed.")]
    [SerializeField] private FloorDataSO[] floors;

    [Header("Room Groups Container")]
    [Tooltip("All FloorRoomButtonGroup instances (from FloorDataSO.floorUIPrefab) will be parented here.")]
    [SerializeField] private Transform floorUiRoot;

    [Header("UI References")]
    [Tooltip("Shows the current floor name.")]
    [SerializeField] private TextMeshProUGUI floorNameLabel;

    [Header("Behaviour")]
    [Tooltip("If true, the first floor in the list will be auto-selected on Start.")]
    [SerializeField] private bool autoSelectFirst = true;

    // ─────────────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────────────
    private readonly List<FloorSelectButton> spawnedFloorButtons = new();
    private readonly Dictionary<FloorDataSO, FloorRoomButtonGroup> roomGroupsByFloor = new();

    private FloorDataSO currentFloor;

    /// <summary>Currently selected floor (may be null if nothing is selected).</summary>
    public FloorDataSO CurrentFloor => currentFloor;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        RebuildFloorButtons();
        BuildAllRoomGroupsOnce();

        if (autoSelectFirst && floors != null && floors.Length > 0)
            SelectFloor(floors[0]);

        else UpdateFloorNameLabel(null);
    }

    private void OnDestroy()
    {
        // Unwire floor buttons
        foreach (var fsb in spawnedFloorButtons)
        {
            if (!fsb) continue;
            var btn = fsb.GetButton();
            if (btn) btn.onClick.RemoveAllListeners();
        }
        spawnedFloorButtons.Clear();

        // Unwire room groups (we don't have to destroy; scene unload will handle it)
        foreach (var kv in roomGroupsByFloor)
        {
            var grp = kv.Value;
            if (!grp) continue;
            grp.onRoomSelected -= HandleRoomChosen;
        }

        roomGroupsByFloor.Clear();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep inspector previews tidy (no runtime allocation)
        if (!Application.isPlaying)
        {
            // Ensure label reflects no selection when editing
            if (autoSelectFirst == false)
                UpdateFloorNameLabel(null);
        }
    }
#endif

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────
    /// <summary>Selects the given floor: updates label and toggles the matching room group.</summary>
    public void SelectFloor(FloorDataSO floor)
    {
        if (!floor || floor == currentFloor) return;

        currentFloor = floor;

        UpdateFloorNameLabel(floor);
        ShowOnlyGroupFor(floor);

        Debug.Log($"[FloorSelectionManager] Floor selected: {floor.floorName}");
    }

    /// <summary>Selects a floor by its FloorType enum value.</summary>
    public void SelectFloorByType(FloorType type)
    {
        var found = floors?.FirstOrDefault(f => f && f.floorType.Equals(type));
        if (found) SelectFloor(found);
    }

    /// <summary>Re-applies UI for the currently selected floor (label + visible group).</summary>
    public void RefreshCurrentFloor()
    {
        if (!currentFloor) return;
        UpdateFloorNameLabel(currentFloor);
        ShowOnlyGroupFor(currentFloor);
    }

    // ─────────────────────────────────────────────────────
    // Floor Buttons
    // ─────────────────────────────────────────────────────
    /// <summary>Builds the floor buttons from the prefab and wires their clicks.</summary>
    private void RebuildFloorButtons()
    {
        if (!floorButtonPrefab || !floorButtonsRoot)
        {
            Debug.LogWarning("[FloorSelectionManager] Missing floorButtonPrefab or floorButtonsRoot.");
            return;
        }

        ClearFloorButtonsOnly();

        if (floors == null || floors.Length == 0) return;

        for (int i = 0; i < floors.Length; i++)
        {
            var data = floors[i];
            if (!data) continue;

            var btnInstance = Instantiate(floorButtonPrefab, floorButtonsRoot);
            btnInstance.name = $"FloorButton_{data.floorName}";
            btnInstance.SetFloor(data);

            var uiBtn = btnInstance.GetButton();
            if (uiBtn)
            {
                int idx = i; // capture index for closure
                uiBtn.onClick.AddListener(() =>
                {
                    if (floors != null && idx < floors.Length && floors[idx])
                        SelectFloor(floors[idx]);
                });
            }

            spawnedFloorButtons.Add(btnInstance);
        }
    }

    /// <summary>Unwires and destroys all spawned floor buttons.</summary>
    private void ClearFloorButtonsOnly()
    {
        foreach (var fsb in spawnedFloorButtons)
        {
            if (!fsb) continue;
            var btn = fsb.GetButton();
            if (btn) btn.onClick.RemoveAllListeners();
            Destroy(fsb.gameObject);
        }
        spawnedFloorButtons.Clear();
    }

    // ─────────────────────────────────────────────────────
    // Room Groups (build once from FloorDataSO.floorUIPrefab, then toggle)
    // ─────────────────────────────────────────────────────
    /// <summary>Instantiates one FloorRoomButtonGroup per floor (once) and subscribes to selection.</summary>
    private void BuildAllRoomGroupsOnce()
    {
        if (!floorUiRoot || floors == null || floors.Length == 0) return;

        foreach (var floor in floors)
        {
            if (!floor) continue;
            if (roomGroupsByFloor.ContainsKey(floor) && roomGroupsByFloor[floor]) continue;

            var prefabGroup = floor.floorUIPrefab; // FloorRoomButtonGroup prefab from FloorDataSO
            if (!prefabGroup)
            {
                Debug.LogWarning($"[FloorSelectionManager] Missing floorUIPrefab on floor: {floor.floorName}");
                continue;
            }

            var group = Instantiate(prefabGroup, floorUiRoot);
            group.name = $"RoomGroup_{floor.floorName}";

            // Optional: if your group needs it, bind rooms against the SO
            group.ApplyFloor(floor);

            // Subscribe to room selection
            group.onRoomSelected += HandleRoomChosen;

            // Hidden by default; we only show the selected one
            group.gameObject.SetActive(false);

            roomGroupsByFloor[floor] = group;
        }
    }

    /// <summary>Activates only the group belonging to the given floor; deactivates others.</summary>
    private void ShowOnlyGroupFor(FloorDataSO floor)
    {
        foreach (var kv in roomGroupsByFloor)
        {
            bool shouldBeActive = (floor != null && kv.Key == floor);

            if (kv.Value && kv.Value.gameObject.activeSelf != shouldBeActive)
                kv.Value.gameObject.SetActive(shouldBeActive);
        }
    }

    // ─────────────────────────────────────────────────────
    // Room Selection Callback
    // ─────────────────────────────────────────────────────
    private void HandleRoomChosen(RoomType room)
    {
        Debug.Log($"[FloorSelectionManager] Room chosen: {room}");
    }

    // ─────────────────────────────────────────────────────
    // UI Helpers
    // ─────────────────────────────────────────────────────
    /// <summary>Updates the floor name label based on the provided floor.</summary>
    private void UpdateFloorNameLabel(FloorDataSO floor)
    {
        if (!floorNameLabel) return;
        floorNameLabel.text = (floor != null && !string.IsNullOrEmpty(floor.floorName))
            ? floor.floorName
            : string.Empty;
    }
}
