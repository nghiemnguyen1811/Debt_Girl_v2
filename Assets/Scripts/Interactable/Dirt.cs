using UnityEngine;

/// <summary>
/// Dirt is an interactable that disappears when interacted with.
/// Can be detected within a radius and visualized in the editor.
/// </summary>
public class Dirt : InteractableBase
{
    #region === Detection Settings ===

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 0.5f;

    /// <summary>
    /// Returns the detection radius used for interaction.
    /// </summary>
    public float GetDetectionRadius() => detectionRadius;

    #endregion

    #region === Interaction Events ===

    /// <summary>
    /// Called when the player interacts with the dirt.
    /// Plays VFX and disables outline.
    /// </summary>
    public override void OnInteract()
    {
        Debug.Log($"Interacted with: {GetObjectName()}");
        SetOutline(false);
        SetParticle(true);
    }

    /// <summary>
    /// Called when interaction ends. Turns off visual and disables the dirt object.
    /// </summary>
    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);

        gameObject.SetActive(false);
    }

    #endregion

    #region === Editor Gizmos ===

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif

    #endregion
}
