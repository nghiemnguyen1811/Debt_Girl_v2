using UnityEngine;
using Sirenix.OdinInspector;
using System;

[CreateAssetMenu(fileName = "New Floor Data", menuName = "Building/Floor Data")]
public class FloorDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // Floor Info
    // ─────────────────────────────────────────────────────
    [Title("Floor Info")]
    public string floorName;
    public FloorType floorType;
    [TextArea(2, 4)]
    public string floorDescription;

    // ─────────────────────────────────────────────────────
    // Rooms
    // ─────────────────────────────────────────────────────
    [Title("Rooms in Floor")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
    public RoomLevelData[] rooms;

    // ─────────────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────────────
    [Title("UI")]
    public FloorRoomButtonGroup floorUIPrefab;
}


[Serializable]
public class RoomLevelData
{
    public RoomType roomType;
    [MinValue(1)]
    public int level = 1;
}