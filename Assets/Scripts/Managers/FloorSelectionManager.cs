using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the floor selection UI and handles localized floor labels/descriptions.
/// Works with LocalizationManager for dynamic language switching.
/// </summary>
[DisallowMultipleComponent]
public class FloorSelectionManager : SingletonMonobehaviour<FloorSelectionManager>
{
    //─────────────────────────────────────────────────────
    #region === Inspector Fields ===

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

    #endregion

    //─────────────────────────────────────────────────────
    #region === Runtime State ===

    private readonly List<FloorSelectButton> spawnedFloorButtons = new();
    private readonly Dictionary<FloorDataSO, FloorRoomButtonGroup> roomGroupsByFloor = new();

    private FloorDataSO currentFloor;
    private int currentIndex;
    private RoomType? pendingRoom;

    public FloorDataSO CurrentFloor => currentFloor;

    #endregion

    //─────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

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

    private void Start()
    {
        InitFloorButtons();
        InitRoomGroups();
        InitNavigation();
        InitConfirmButton();

        if (autoSelectFirst && floorDataArray != null && floorDataArray.Length > 0)
            SelectFloor(floorDataArray[0]);
        else
            UpdateFloorDescriptionText(null);

        UpdateNavButtons();

        if (GameManager.Instance != null)
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
        if (!Application.isPlaying && !autoSelectFirst)
            UpdateFloorDescriptionText(null);
    }
#endif

    #endregion

    //─────────────────────────────────────────────────────
    #region === Localization Integration ===

    /// <summary>
    /// Called when the game language changes.
    /// Refreshes current floor description and button labels.
    /// </summary>
    private void HandleLanguageChanged()
    {
        // Refresh description
        if (currentFloor != null)
            UpdateFloorDescriptionText(currentFloor);

        // Refresh button labels
        foreach (var btn in spawnedFloorButtons)
        {
            if (btn && btn.GetFloorAsset() != null)
                btn.UpdateLocalizedLabel();
        }
    }

    #endregion

    //─────────────────────────────────────────────────────
    #region === Public API ===

    /// <summary>
    /// Selects a floor by data reference.
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

        Debug.Log($"[FloorSelectionManager] Floor selected: {floor.floorNameKey}");
    }

    /// <summary>
    /// Selects a floor by its enum type.
    /// </summary>
    public void SelectFloorByType(FloorType type)
    {
        var found = floorDataArray?.FirstOrDefault(f => f && f.floorType.Equals(type));
        if (found) SelectFloor(found);
    }

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

    public void HideAllUnlockPanels()
    {
        foreach (var kv in roomGroupsByFloor)
        {
            var group = kv.Value;
            if (!group) continue;

            foreach (var roomBtn in group.GetRoomButtons())
                if (roomBtn) roomBtn.ShowUnlockPanel(false);
        }
    }

    #endregion

    //─────────────────────────────────────────────────────
    #region === Room Selection Logic ===

    private void CachePendingRoom(RoomType room)
    {
        pendingRoom = room;
        if (confirmRoomButton) confirmRoomButton.interactable = true;
    }

    private void ConfirmPendingRoom()
    {
        if (!pendingRoom.HasValue)
        {
            ResetPendingSelection();
            return;
        }

        HandleRoomChosen(pendingRoom.Value);
        ResetPendingSelection();
    }

    private void HandleRoomChosen(RoomType room)
    {
        UIManager.Instance.ToggleSelectRoomPanel(false);
        RoomManager.Instance.SetActiveRoom(room);
    }

    #endregion

    //─────────────────────────────────────────────────────
    #region === Floor Buttons ===

    private void InitFloorButtons() => BuildFloorButtons();

    private void BuildFloorButtons()
    {
        if (!floorButtonPrefab || !floorButtonsRoot)
        {
            Debug.LogWarning("[FloorSelectionManager] Missing floorButtonPrefab or root.");
            return;
        }

        ClearFloorButtons();

        if (floorDataArray == null || floorDataArray.Length == 0) return;

        for (int i = 0; i < floorDataArray.Length; i++)
        {
            var data = floorDataArray[i];
            if (!data) continue;

            var btnInstance = Instantiate(floorButtonPrefab, floorButtonsRoot);
            btnInstance.SetFloor(data);
            btnInstance.UpdateLocalizedLabel();

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

    #endregion

    //─────────────────────────────────────────────────────
    #region === Room Groups ===

    private void InitRoomGroups() => BuildRoomGroups();

    private void BuildRoomGroups()
    {
        if (!roomGroupsRoot || floorDataArray == null || floorDataArray.Length == 0) return;

        foreach (var floor in floorDataArray)
        {
            if (!floor) continue;
            if (roomGroupsByFloor.ContainsKey(floor) && roomGroupsByFloor[floor]) continue;

            var prefabGroup = floor.floorUIPrefab;
            if (!prefabGroup) continue;

            var group = Instantiate(prefabGroup, roomGroupsRoot);
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

    #endregion

    //─────────────────────────────────────────────────────
    #region === Navigation ===

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

    #endregion

    //─────────────────────────────────────────────────────
    #region === Confirm Button ===

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

    #endregion

    //─────────────────────────────────────────────────────
    #region === UI Helpers ===

    private async void UpdateFloorDescriptionText(FloorDataSO floor)
    {
        if (!floorInfoText) return;

        if (floor == null)
        {
            floorInfoText.text = string.Empty;
            return;
        }

        string description = await LocalizationManager.Instance.GetLocalizedString("Move Floor Labels", floor.floorDescriptionKey);
        floorInfoText.text = !string.IsNullOrEmpty(description) ? description : string.Empty;
    }

    #endregion

    //─────────────────────────────────────────────────────
    #region === Utility ===

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

    #endregion
}
