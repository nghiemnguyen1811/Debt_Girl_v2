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
    [SerializeField] private FloorDataSO[] floorDataArray;

    [Header("Room Groups Container")]
    [SerializeField] private Transform roomGroupsRoot;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI floorInfoText;
    [SerializeField] private Button confirmRoomButton;

    [Header("Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Behaviour")]
    [SerializeField] private bool autoSelectFirst = true;

    [Header("Config")]
    [SerializeField] private UIColorsConfig colorsConfig;

    // ─────────────────────────────────────────────────────
    // Runtime State
    // ─────────────────────────────────────────────────────
    private readonly List<FloorSelectButton> spawnedFloorButtons = new();
    private readonly Dictionary<FloorDataSO, FloorRoomButtonGroup> roomGroupsByFloor = new();

    private FloorDataSO currentFloor;
    private int currentIndex;
    private RoomType? pendingRoom;

    // ─────────────────────────────────────────────────────
    // Properties
    // ─────────────────────────────────────────────────────
    public FloorDataSO CurrentFloor => currentFloor;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    
    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
    }
    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
    }
    private void HandleLanguageChanged()
    {
        if (currentFloor != null)
            UpdateFloorDescriptionText(currentFloor);
    }
    private void Start()
    {
        InitFloorButtons();
        InitRoomGroups();
        InitNavigation();
        InitConfirmButton();

        if (autoSelectFirst && floorDataArray != null && floorDataArray.Length > 0)
            SelectFloor(floorDataArray[0]);

        else UpdateFloorDescriptionText(null);

        UpdateNavButtons();

        // Subscribe to level changes
        GameManager.Instance.OnLevelChanged += RefreshAllFloors;
    }
    

    private void OnDestroy()
    {
        CleanupFloorButtons();
        CleanupRoomGroups();
        CleanupNavigation();
        CleanupConfirmButton();

        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= RefreshAllFloors;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && autoSelectFirst == false)
            UpdateFloorDescriptionText(null);
    }
#endif

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Select a floor by reference.
    /// </summary>
    public void SelectFloor(FloorDataSO floor)
    {
        if (!floor || floor == currentFloor) return;

        currentFloor = floor;
        currentIndex = System.Array.IndexOf(floorDataArray, floor);

        UpdateFloorDescriptionText(floor);
        ShowRoomGroupForFloor(floor);
        UpdateButtonVisuals(floor);
        UpdateNavButtons();
        ResetPendingSelection();

        var key = LocalizationManager.Instance.GetLocalizedString("Move Floor Labels", floor.floorNameKey);
        Debug.Log($"[FloorSelectionManager] Floor selected: {key}");

    }

    /// <summary>
    /// Select a floor by its type (FloorType enum).
    /// </summary>
    public void SelectFloorByType(FloorType type)
    {
        var found = floorDataArray?.FirstOrDefault(f => f && f.floorType.Equals(type));
        if (found) SelectFloor(found);
    }

    /// <summary>
    /// Refreshes the currently selected floor's UI,
    /// including re-applying room group to update button states.
    /// </summary>
    public void RefreshCurrentFloor()
    {
        if (!currentFloor) return;

        UpdateFloorDescriptionText(currentFloor);
        ShowRoomGroupForFloor(currentFloor);

        if (roomGroupsByFloor.TryGetValue(currentFloor, out var group) && group)
            group.ApplyFloor(currentFloor);
    }

    public void RefreshAllFloors()
    {
        foreach (var kv in roomGroupsByFloor)
            if (kv.Value)
                kv.Value.ApplyFloor(kv.Key);
    }

    /// <summary>
    /// Hide all unlock panels in all room buttons across all floors
    /// </summary>
    public void HideAllUnlockPanels()
    {
        foreach (var kv in roomGroupsByFloor)
        {
            var group = kv.Value;
            if (!group) continue;

            foreach (var roomBtn in group.GetRoomButtons())
            {
                if (!roomBtn) continue;
                roomBtn.ShowUnlockPanel(false);
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // Room Selection Logic
    // ─────────────────────────────────────────────────────
    private void CachePendingRoom(RoomType room)
    {
        pendingRoom = room;
        if (confirmRoomButton) confirmRoomButton.interactable = true;
        Debug.Log($"[FloorSelectionManager] Pending room set: {room}");
    }

    private void ConfirmPendingRoom()
    {
        if (!pendingRoom.HasValue)
        {
            ResetPendingSelection();
            Debug.Log("[FloorSelectionManager] No pending room to confirm.");
            return;
        }

        RoomType chosenRoom = pendingRoom.Value;

        // Thực hiện dịch chuyển
        HandleRoomChosen(chosenRoom);

        // Reset state
        ResetPendingSelection();
    }


    private void HandleRoomChosen(RoomType room)
    {
        Debug.Log($"[FloorSelectionManager] Room confirmed: {room}");

        UIManager.Instance.ToggleSelectRoomPanel(false);
        RoomManager.Instance.SetActiveRoom(room);
    }

    // ─────────────────────────────────────────────────────
    // Floor Buttons
    // ─────────────────────────────────────────────────────
    private void InitFloorButtons() => BuildFloorButtons();

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

            var key = LocalizationManager.Instance.GetLocalizedString("Move Floor Labels", data.floorNameKey);
            btnInstance.name = $"FloorButton_{key}";
            btnInstance.SetFloor(data);

            var uiBtn = btnInstance.GetButton();

            if (uiBtn)
            {
                int idx = i;
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

    private void CleanupFloorButtons()
    {
        foreach (var fsb in spawnedFloorButtons)
        {
            if (!fsb) continue;
            var btn = fsb.GetButton();
            if (btn) btn.onClick.RemoveAllListeners();
        }

        spawnedFloorButtons.Clear();
    }

    private void UpdateButtonVisuals(FloorDataSO selectedFloor)
    {
        foreach (var fsb in spawnedFloorButtons)
        {
            if (!fsb) continue;
            bool isActive = fsb.GetFloorAsset() == selectedFloor;
            fsb.SetOutlineActive(isActive);

            if (colorsConfig)
                fsb.SetLabelColor(isActive ? colorsConfig.tabOn : colorsConfig.tabOff);
        }
    }

    // ─────────────────────────────────────────────────────
    // Room Groups
    // ─────────────────────────────────────────────────────
    private void InitRoomGroups() => BuildRoomGroups();

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
                //var key = LocalizationManager.Instance.GetLocalizedString("Move Room Labels", floor.floorNameKey);
                //Debug.LogWarning($"[FloorSelectionManager] Missing floorUIPrefab on floor: {key}");
                continue;
            }

            var group = Instantiate(prefabGroup, roomGroupsRoot);

            //var key = LocalizationManager.Instance.GetLocalizedString("Move Room Labels", floor.floorNameKey);
            //group.name = $"RoomGroup_{floor.floorNameLocal.GetLocalizedString()}";
            group.ApplyFloor(floor);

            group.onRoomSelected += (room) =>
            {
                HideAllUnlockPanels();

                var buttons = group.GetRoomButtons();

                foreach (var btn in buttons)
                {
                    if (btn && btn.RoomType == room)
                    {
                        btn.ShowUnlockPanel(true);
                        break;
                    }
                }

                CachePendingRoom(room);
            };

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

    private void CleanupRoomGroups()
    {
        foreach (var kv in roomGroupsByFloor)
        {
            var grp = kv.Value;
            if (!grp) continue;
            grp.onRoomSelected -= CachePendingRoom;
        }

        roomGroupsByFloor.Clear();
    }

    // ─────────────────────────────────────────────────────
    // Navigation
    // ─────────────────────────────────────────────────────
    private void InitNavigation()
    {
        BindButton(prevButton, SelectPrevFloor, true);
        BindButton(nextButton, SelectNextFloor, true);
    }

    private void SelectPrevFloor()
    {
        if (floorDataArray == null || floorDataArray.Length == 0) return;
        if (currentIndex <= 0) return;

        currentIndex--;
        SelectFloor(floorDataArray[currentIndex]);

        AudioManager.Instance.PlayInteractSound(8);
    }

    private void SelectNextFloor()
    {
        if (floorDataArray == null || floorDataArray.Length == 0) return;
        if (currentIndex >= floorDataArray.Length - 1) return;

        currentIndex++;
        SelectFloor(floorDataArray[currentIndex]);

        AudioManager.Instance.PlayInteractSound(8);
    }

    private void UpdateNavButtons()
    {
        if (prevButton) prevButton.interactable = (currentIndex > 0);
        if (nextButton) nextButton.interactable = (currentIndex < floorDataArray.Length - 1);
    }

    private void CleanupNavigation()
    {
        BindButton(prevButton, SelectPrevFloor, false);
        BindButton(nextButton, SelectNextFloor, false);
    }

    // ─────────────────────────────────────────────────────
    // Confirm Button
    // ─────────────────────────────────────────────────────
    private void InitConfirmButton()
    {
        if (confirmRoomButton)
        {
            BindButton(confirmRoomButton, ConfirmPendingRoom, true);
            confirmRoomButton.interactable = false;
        }
    }

    private void CleanupConfirmButton()
    {
        BindButton(confirmRoomButton, ConfirmPendingRoom, false);
    }

    // ─────────────────────────────────────────────────────
    // UI Helpers
    // ─────────────────────────────────────────────────────
    private async void UpdateFloorDescriptionText(FloorDataSO floor)
    {
        if (!floorInfoText) return;

        if (floor == null)
        {
            floorInfoText.text = string.Empty;
            return;
        }

        string description = await LocalizationManager.Instance.GetLocalizedString("Move Floor Labels", floor.floorDescriptionKey);
        floorInfoText.text = (floor != null && !string.IsNullOrEmpty(description)
            ? description
            : string.Empty);
    }

    // ─────────────────────────────────────────────────────
    // Utility Helpers
    // ─────────────────────────────────────────────────────
    private void ResetPendingSelection()
    {
        pendingRoom = null;
        if (confirmRoomButton) confirmRoomButton.interactable = false;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action, bool bind)
    {
        if (!button) return;
        if (bind) button.onClick.AddListener(action);
        else button.onClick.RemoveListener(action);
    }
}
