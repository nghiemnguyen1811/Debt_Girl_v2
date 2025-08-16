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
    [Tooltip("Assign all room buttons in the exact display order.")]
    [SerializeField] private Button[] roomButtons;

    [Header("Behaviour")]
    [Tooltip("If true, will (re)bind buttons on Start using current mapped rooms.")]
    [SerializeField] private bool autoBindOnStart = true;

    // ─────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────
    /// <summary>Raised when a room button is clicked.</summary>
    public event Action<RoomType> onRoomSelected;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────
    [Tooltip("Rooms mapped by index to roomButtons. Set via ApplyFloor/SetRooms.")]
    private RoomType[] mappedRooms;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        if (autoBindOnStart)
            RebindAllButtons();
    }

    private void OnDestroy()
    {
        UnbindAllButtons();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Preview in editor: rebind immediately when something changes in the inspector.
        if (!Application.isPlaying && autoBindOnStart)
            RebindAllButtons();
    }
#endif

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────
    /// <summary>Populate from a FloorDataSO and re-bind the buttons.</summary>
    public void ApplyFloor(FloorDataSO floor)
    {
        mappedRooms = (floor != null && floor.roomTypes != null)
            ? floor.roomTypes
            : Array.Empty<RoomType>();

        RebindAllButtons();
    }

    /// <summary>Returns the current mapped rooms (index-aligned with roomButtons).</summary>
    public RoomType[] GetMappedRooms() => mappedRooms;

    /// <summary>Returns the current button array.</summary>
    public Button[] GetRoomButtons() => roomButtons;

    // ─────────────────────────────────────────────────────
    // Internal Wiring
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// (Re)wires all buttons according to the current mappedRooms.
    /// Buttons without a corresponding room or with RoomType.None are hidden.
    /// Extra buttons beyond mappedRooms length are hidden.
    /// </summary>
    private void RebindAllButtons()
    {
        int btnCount = roomButtons?.Length ?? 0;
        int roomCount = mappedRooms?.Length ?? 0;

        if (btnCount == 0)
            return;

        // Always clear listeners first
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

    /// <summary>Removes all listeners from every button.</summary>
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
        button.onClick.AddListener(() =>
        {
            onRoomSelected?.Invoke(room);
        });
    }

    private void DeactivateButton(Button button)
    {
        button.onClick.RemoveAllListeners();
        button.gameObject.SetActive(false);
    }
}
