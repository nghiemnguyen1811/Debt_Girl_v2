using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class FloorSelectionManager : SingletonMonobehaviour<FloorSelectionManager>
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields (Serialized configuration and references)
    // ─────────────────────────────────────────────────────
    [Header("Floor Button Prefab & Root")]
    [SerializeField] private FloorSelectButton floorButtonPrefab;
    [SerializeField] private Transform floorButtonsRoot;

    [Header("Floors")]
    [SerializeField] private FloorDataSO[] floorDataArray;

    [Header("Room Groups Container")]
    [SerializeField] private Transform roomGroupsRoot;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI floorInfoText;

    [Header("Behaviour")]
    [SerializeField] private bool autoSelectFirst = true;

    [Header("Config")]
    [SerializeField] private UIColorsConfig colorsConfig;

    // ─────────────────────────────────────────────────────
    // Runtime State (variables maintained during play)
    // ─────────────────────────────────────────────────────
    private readonly List<FloorSelectButton> spawnedFloorButtons = new();
    private readonly Dictionary<FloorDataSO, FloorRoomButtonGroup> roomGroupsByFloor = new();
    private FloorDataSO currentFloor;

    /// <summary>Currently selected floor (null if nothing is selected).</summary>
    public FloorDataSO CurrentFloor => currentFloor;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        BuildFloorButtons();
        BuildRoomGroups();

        if (autoSelectFirst && floorDataArray != null && floorDataArray.Length > 0)
            SelectFloor(floorDataArray[0]);
        else
            UpdateFloorDescriptionText(null);
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

        // Unwire room groups
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
        // Keep inspector previews tidy (no runtime allocation in edit mode)
        if (!Application.isPlaying && autoSelectFirst == false)
            UpdateFloorDescriptionText(null);
    }
#endif

    // ─────────────────────────────────────────────────────
    // Public API (called externally to control floor selection)
    // ─────────────────────────────────────────────────────
    /// <summary>Selects the given floor: updates label, highlights, and toggles the matching room group.</summary>
    public void SelectFloor(FloorDataSO floor)
    {
        if (!floor || floor == currentFloor) return;

        currentFloor = floor;

        UpdateFloorDescriptionText(floor);
        ShowRoomGroupForFloor(floor);

        // Update button visuals
        foreach (var fsb in spawnedFloorButtons)
        {
            if (!fsb) continue;
            bool isActive = fsb.GetFloorAsset() == floor;
            fsb.SetOutlineActive(isActive);

            if (colorsConfig)
                fsb.SetLabelColor(isActive ? colorsConfig.tabOn : colorsConfig.tabOff);
        }

        Debug.Log($"[FloorSelectionManager] Floor selected: {floor.floorName}");
    }

    /// <summary>Selects a floor by its FloorType enum value.</summary>
    public void SelectFloorByType(FloorType type)
    {
        var found = floorDataArray?.FirstOrDefault(f => f && f.floorType.Equals(type));
        if (found) SelectFloor(found);
    }

    /// <summary>Re-applies UI for the currently selected floor (label + visible group).</summary>
    public void RefreshCurrentFloor()
    {
        if (!currentFloor) return;
        UpdateFloorDescriptionText(currentFloor);
        ShowRoomGroupForFloor(currentFloor);
    }

    // ─────────────────────────────────────────────────────
    // Floor Buttons (create, clear, and manage floor button instances)
    // ─────────────────────────────────────────────────────
    private void BuildFloorButtons()
    {
        if (!floorButtonPrefab || !floorButtonsRoot)
        {
            Debug.LogWarning("[FloorSelectionManager] Missing floorButtonPrefab or floorButtonsRoot.");
            return;
        }

        ClearFloorButtons();

        if (floorDataArray == null || floorDataArray.Length == 0) return;

        for (int i = 0; i < floorDataArray.Length; i++)
        {
            var data = floorDataArray[i];
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
                    if (floorDataArray != null && idx < floorDataArray.Length && floorDataArray[idx])
                        SelectFloor(floorDataArray[idx]);
                });
            }

            spawnedFloorButtons.Add(btnInstance);
        }
    }

    private void ClearFloorButtons()
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
    // Room Groups (create and toggle groups for each floor)
    // ─────────────────────────────────────────────────────
    private void BuildRoomGroups()
    {
        if (!roomGroupsRoot || floorDataArray == null || floorDataArray.Length == 0) return;

        foreach (var floor in floorDataArray)
        {
            if (!floor) continue;
            if (roomGroupsByFloor.ContainsKey(floor) && roomGroupsByFloor[floor]) continue;

            var prefabGroup = floor.floorUIPrefab;
            if (!prefabGroup)
            {
                Debug.LogWarning($"[FloorSelectionManager] Missing floorUIPrefab on floor: {floor.floorName}");
                continue;
            }

            var group = Instantiate(prefabGroup, roomGroupsRoot);
            group.name = $"RoomGroup_{floor.floorName}";
            group.ApplyFloor(floor);

            // Subscribe to room selection
            group.onRoomSelected += HandleRoomChosen;

            // Hidden by default; we only show the selected one
            group.gameObject.SetActive(false);

            roomGroupsByFloor[floor] = group;
        }
    }

    private void ShowRoomGroupForFloor(FloorDataSO floor)
    {
        foreach (var kv in roomGroupsByFloor)
        {
            bool shouldBeActive = (floor != null && kv.Key == floor);
            if (kv.Value && kv.Value.gameObject.activeSelf != shouldBeActive)
                kv.Value.gameObject.SetActive(shouldBeActive);
        }
    }

    // ─────────────────────────────────────────────────────
    // Callbacks
    // ─────────────────────────────────────────────────────
    private void HandleRoomChosen(RoomType room)
    {
        Debug.Log($"[FloorSelectionManager] Room chosen: {room}");

        RoomManager.Instance.SetActiveRoom(room);
        UIManager.Instance.ToggleSelectRoomPanel(false);
        UIManager.Instance.TogglePhonePanel(false);
    }

    // ─────────────────────────────────────────────────────
    // UI Helpers
    // ─────────────────────────────────────────────────────
    private void UpdateFloorDescriptionText(FloorDataSO floor)
    {
        if (!floorInfoText) return;
        floorInfoText.text = (floor != null && !string.IsNullOrEmpty(floor.floorDescription))
            ? floor.floorDescription
            : string.Empty;
    }
}
