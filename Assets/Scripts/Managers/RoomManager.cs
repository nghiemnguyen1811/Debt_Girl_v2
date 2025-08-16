using System.Collections;
using UnityEngine;

/// <summary>
/// Manages room transitions, including fade effects and camera/player relocation.
/// </summary>
public class RoomManager : SingletonMonobehaviour<RoomManager>
{
    // ─────────────────────────────────────────────────────
    // References
    // ─────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Fader fader;
    [SerializeField] private Transform[] mainCameras;

    private PlayerControl player;

    // ─────────────────────────────────────────────────────
    // Transition Settings
    // ─────────────────────────────────────────────────────
    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float delayDuringBlack = 0.5f;

    private bool isTransitioning = false;

    // ─────────────────────────────────────────────────────
    // Room Data
    // ─────────────────────────────────────────────────────
    [Header("Room Data")]
    [SerializeField] private Room[] rooms;

    public RoomType currentRoom = RoomType.None;

    // ─────────────────────────────────────────────────────
    // Unity Methods
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        player = PlayerControl.Instance;
        InitRoom(RoomType.Danbi);
    }

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Instantly moves player and cameras to the specified room, without fade transition.
    /// </summary>
    public void InitRoom(RoomType targetRoom)
    {
        ApplyRoomState(targetRoom);
    }

    /// <summary>
    /// Triggers a transition to the specified room with fade in/out.
    /// </summary>
    public void SetActiveRoom(RoomType targetRoom)
    {
        if (isTransitioning) return;

        fader.gameObject.SetActive(true);
        StartCoroutine(SetActiveRoomRoutine(targetRoom));
    }

    /// <summary>
    /// Gets the Room object by its type.
    /// </summary>
    public Room GetRoom(RoomType type)
    {
        foreach (Room room in rooms)
        {
            if (room != null && room.RoomType == type)
                return room;
        }

        Debug.LogWarning($"Room of type {type} not found.");
        return null;
    }

    /// <summary>
    /// Updates the current room tracker.
    /// </summary>
    public void SetCurrentRoom(RoomType newRoom)
    {
        currentRoom = newRoom;
    }

    // ─────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Coroutine that fades out, moves to target room, then fades in.
    /// </summary>
    private IEnumerator SetActiveRoomRoutine(RoomType targetRoom)
    {
        isTransitioning = true;

        AudioManager.Instance.PlayInteractSound(6);

        yield return fader.FadeOutCo(fadeDuration);

        ApplyRoomState(targetRoom);

        yield return new WaitForSeconds(delayDuringBlack);

        AudioManager.Instance.PlayInteractSound(7);

        yield return fader.FadeInCo(fadeDuration);

        PlayerControl.Instance.interactDetector.StopCurrentInteraction();

        isTransitioning = false;
    }

    /// <summary>
    /// Applies the player/camera position and room activation for a given room type.
    /// Used by both Init and fade transitions.
    /// </summary>
    private void ApplyRoomState(RoomType targetRoom)
    {
        Room target = GetRoom(targetRoom);
        if (target == null) return;

        // Move player
        player.transform.position = target.PlayerSpawnPoint.position;
        player.transform.rotation = target.PlayerSpawnPoint.rotation;

        // Reset Rotation
        player.movementHandler.ResetModelRotation();

        // Move cameras
        foreach (Transform cam in mainCameras)
        {
            if (cam == null) continue;
            Vector3 camPos = cam.localPosition;
            camPos.x = target.CameraSpawnPosition;
            cam.localPosition = camPos;
        }

        // Activate correct room, deactivate others
        foreach (Room room in rooms)
        {
            if (room == null) continue;
            room.gameObject.SetActive(room.RoomType == targetRoom);
        }

        DirtManager.Instance.SetFloorCollider(target.RoomBounds);
        SetCurrentRoom(targetRoom);
    }
}
