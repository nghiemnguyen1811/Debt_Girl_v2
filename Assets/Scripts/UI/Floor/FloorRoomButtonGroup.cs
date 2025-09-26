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
    [SerializeField] private Button[] roomButtons;

    [Header("Behaviour")]
    [SerializeField] private bool autoBindOnStart = true;

    // ─────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────
    public event Action<RoomType> onRoomSelected;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────
    private RoomType[] mappedRooms;

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
        mappedRooms = (floor != null && floor.roomTypes != null)
            ? floor.roomTypes
            : Array.Empty<RoomType>();

        RebindAllButtons();
    }

    public RoomType[] GetMappedRooms() => mappedRooms;
    public Button[] GetRoomButtons() => roomButtons;

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
            var button = roomButtons[i];
            if (!button) continue;

            bool hasValidRoom = (i < roomCount) && (mappedRooms[i] != RoomType.None);

            if (!hasValidRoom)
            {
                DeactivateButton(button);
                continue;
            }

            ActivateButton(button, mappedRooms[i]);
        }
    }

    private void UnbindAllButtons()
    {
        if (roomButtons == null) return;

        foreach (var button in roomButtons)
        {
            if (!button) continue;
            button.onClick.RemoveAllListeners();
        }
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    private void ActivateButton(Button button, RoomType room)
    {
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();

        // Disable if this is the currently active room
        bool isCurrentRoom = RoomManager.Instance && RoomManager.Instance.ActiveRoom == room;
        button.interactable = !isCurrentRoom;

        if (!isCurrentRoom)
            button.onClick.AddListener(() => onRoomSelected?.Invoke(room));
    }

    private void DeactivateButton(Button button)
    {
        button.onClick.RemoveAllListeners();
        button.gameObject.SetActive(false);
    }
}
