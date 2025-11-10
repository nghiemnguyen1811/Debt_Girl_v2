using UnityEngine;
using Sirenix.OdinInspector;
using System;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "New Floor Data", menuName = "Building/Floor Data")]
public class FloorDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // Floor Info
    // ─────────────────────────────────────────────────────
    [Title("Floor Info")]
    public FloorType floorType;

    [Tooltip("Floor name key")]
    public string floorNameKey;

    [Tooltip("Floor description key")]
    public string floorDescriptionKey;

    // ─────────────────────────────────────────────────────
    // Rooms
    // ─────────────────────────────────────────────────────
    [Title("Rooms in Floor")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
    public RoomData[] rooms;

    // ─────────────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────────────
    [Title("UI")]
    public FloorRoomButtonGroup floorUIPrefab;
}


[Serializable]
public class RoomData
{
    public RoomType roomType;
    public Sprite markerIcon;
    [MinValue(1)]
    public int level = 1;
}