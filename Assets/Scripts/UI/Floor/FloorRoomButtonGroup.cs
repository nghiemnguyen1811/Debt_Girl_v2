using UnityEngine;
using UnityEngine.UI;
using System;

[DisallowMultipleComponent]
public class FloorRoomButtonGroup : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("Room Buttons")]
    [SerializeField] private RoomButtonDisplay[] roomButtons; // Array of custom room buttons

    [Header("Behaviour")]
    [SerializeField] private bool autoBindOnStart = true;

    // ─────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────
    public event Action<RoomType> onRoomSelected;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────
    private RoomLevelData[] mappedRooms;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        if (autoBindOnStart) RebindAllButtons();
    }

    private void OnDestroy()
    {
        UnbindAllButtons();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && autoBindOnStart)
            RebindAllButtons();
    }
#endif

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────
    public void ApplyFloor(FloorDataSO floor)
    {
        mappedRooms = (floor != null && floor.rooms != null)
            ? floor.rooms
            : Array.Empty<RoomLevelData>();

        RebindAllButtons();
    }

    public RoomButtonDisplay[] GetRoomButtons() => roomButtons;

    // ─────────────────────────────────────────────────────
    // Internal Wiring
    // ─────────────────────────────────────────────────────
    private void RebindAllButtons()
    {
        int btnCount = roomButtons?.Length ?? 0;
        int roomCount = mappedRooms?.Length ?? 0;

        if (btnCount == 0) return;

        UnbindAllButtons();

        for (int i = 0; i < btnCount; i++)
        {
            var buttonDisplay = roomButtons[i];
            if (!buttonDisplay) continue;

            bool hasValidRoom = (i < roomCount) && (mappedRooms[i].roomType != RoomType.None);

            if (!hasValidRoom)
            {
                DeactivateButton(buttonDisplay);
                continue;
            }

            ActivateButton(buttonDisplay, mappedRooms[i]);
        }
    }

    private void UnbindAllButtons()
    {
        if (roomButtons == null) return;

        foreach (var buttonDisplay in roomButtons)
        {
            if (!buttonDisplay) continue;
            buttonDisplay.RoomButton.onClick.RemoveAllListeners();
        }
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private void ActivateButton(RoomButtonDisplay buttonDisplay, RoomLevelData roomData)
    {
        var btn = buttonDisplay.RoomButton;
        buttonDisplay.gameObject.SetActive(true);
        btn.onClick.RemoveAllListeners();

        bool isCurrentRoom = RoomManager.Instance && RoomManager.Instance.ActiveRoom == roomData.roomType;
        bool hasEnoughLevel = GameManager.Instance && GameManager.Instance.CurrentLevel >= roomData.level;

        // Update visual state through RoomButtonDisplay
        buttonDisplay.Setup(roomData.level);
        buttonDisplay.Refresh(hasEnoughLevel);

        btn.interactable = !isCurrentRoom && hasEnoughLevel;

        if (!isCurrentRoom && hasEnoughLevel)
            btn.onClick.AddListener(() => onRoomSelected?.Invoke(roomData.roomType));
    }

    private void DeactivateButton(RoomButtonDisplay buttonDisplay)
    {
        var btn = buttonDisplay.RoomButton;
        btn.onClick.RemoveAllListeners();
        buttonDisplay.gameObject.SetActive(false);
    }
}
