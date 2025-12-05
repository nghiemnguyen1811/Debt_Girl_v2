using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// NPC interactable object.
/// Handles dialogue and smooth rotation toward player during interaction.
/// </summary>
public class NPC : InteractableBase
{
    //────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [BoxGroup("Dialogue")]
    [SerializeField]
    private NPCDialogueData npcData;                  // Dialogue data for this NPC

    [BoxGroup("Look At Player")]
    [SerializeField]
    private bool enableRotateToPlayer = true;         // Toggle: should NPC rotate to face player?

    [BoxGroup("Look At Player")]
    [SerializeField]
    private bool rotateOnlyOnY = true;                // Rotate only around Y axis (no pitch/roll)

    [BoxGroup("Look At Player")]
    [SerializeField]
    private bool restoreRotationOnStop = true;        // Return to original rotation when stop interact

    [BoxGroup("Look At Player")]
    [SerializeField, Min(0.01f)]
    private float rotateDuration = 0.35f;             // Time for smooth rotation

    #endregion

    //────────────────────────────────────────────────────
    #region === Private Fields ===

    private Quaternion initialRotation;               // Cached original rotation of NPC
    private Coroutine rotateCoroutine;                // Current running rotate coroutine (if any)

    #endregion

    //────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    /// <summary>
    /// Unity Start – cache initial rotation and call base Start.
    /// </summary>
    protected override void Start()
    {
        base.Start();
        initialRotation = transform.rotation;
    }

    #endregion

    //────────────────────────────────────────────────────
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with this NPC.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        // Smoothly rotate to face player if enabled
        if (enableRotateToPlayer)
            FacePlayerSmooth();

        Debug.Log($"Interacted with: {GetObjectName()}");

        // Call base interaction logic (outline, particle, prop, sound)
        base.OnInteract(showProp);

        // Start NPC dialogue
        DialogueManager.Instance.StartNpcDialogue(npcData);
    }

    /// <summary>
    /// Called when the interaction ends or is canceled.
    /// </summary>
    public override void OnStopInteract()
    {
        // Call base stop logic (outline, particle, prop, sound, quest event)
        base.OnStopInteract();

        // Smoothly restore original rotation if enabled
        if (enableRotateToPlayer && restoreRotationOnStop)
            ResetRotationSmooth();
    }

    #endregion

    //────────────────────────────────────────────────────
    #region === Rotation Helpers ===

    /// <summary>
    /// Start smooth rotation so NPC faces the player.
    /// </summary>
    private void FacePlayerSmooth()
    {
        var player = PlayerControl.Instance;
        if (player == null)
            return;

        Vector3 npcPos = transform.position;
        Vector3 playerPos = player.transform.position;
        Vector3 dir = playerPos - npcPos;

        if (rotateOnlyOnY)
            dir.y = 0f; // Lock Y to avoid tilting up/down

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        StartSmoothRotation(targetRot);
    }

    /// <summary>
    /// Start smooth rotation back to the cached initial rotation.
    /// </summary>
    private void ResetRotationSmooth()
    {
        StartSmoothRotation(initialRotation);
    }

    /// <summary>
    /// Start or restart the smooth rotation coroutine to target rotation.
    /// </summary>
    private void StartSmoothRotation(Quaternion targetRotation)
    {
        // Stop previous rotation if still running
        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        rotateCoroutine = StartCoroutine(RotateOverTime(targetRotation, rotateDuration));
    }

    /// <summary>
    /// Coroutine: smoothly rotate from current to target rotation over time.
    /// </summary>
    private IEnumerator RotateOverTime(Quaternion targetRotation, float duration)
    {
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Slerp for smooth spherical interpolation
            transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);

            yield return null;
        }

        transform.rotation = targetRotation;
        rotateCoroutine = null;
    }

    #endregion
}
