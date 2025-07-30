using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Stores data for a single room: its type and spawn positions.
/// </summary>
public class Room : MonoBehaviour
{
    [Header("Room Info")]
    [SerializeField] private RoomType roomType;

    [Header("Spawn Positions")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private float cameraSpawnX;

    /// <summary>
    /// The type of the room.
    /// </summary>
    public RoomType RoomType => roomType;

    /// <summary>
    /// The position where the player should spawn in this room.
    /// </summary>
    public Transform PlayerSpawnPoint => playerSpawnPoint;

    /// <summary>
    /// The X position where the camera should be placed.
    /// </summary>
    public float CameraSpawnPosition => cameraSpawnX;
}
