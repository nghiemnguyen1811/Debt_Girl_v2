using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Floor Data", menuName = "Building/Floor Data")]
public class FloorDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // 1) Floor Info
    // ─────────────────────────────────────────────────────
    [Title("Floor Info")]
    [LabelWidth(120), Tooltip("Display name shown to players (e.g., '2nd Floor').")]
    public string floorName;

    [LabelWidth(120), Tooltip("Logical enum of the floor used by code / selection.")]
    public FloorType floorType;

    // ─────────────────────────────────────────────────────
    // 2) Rooms
    // ─────────────────────────────────────────────────────
    [Title("Rooms in Floor")]
    [LabelWidth(120), Tooltip("All rooms available on this floor (used to build room buttons).")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
    public RoomType[] roomTypes;

    // ─────────────────────────────────────────────────────
    // 3) UI (Prefab Group)
    // ─────────────────────────────────────────────────────
    [Title("UI")]
    [LabelWidth(120)]
    [Tooltip("Prefab of FloorRoomButtonGroup for this floor. It will be instantiated once and toggled on selection.")]
    [AssetsOnly, Required]
    public FloorRoomButtonGroup floorUIPrefab;
}
