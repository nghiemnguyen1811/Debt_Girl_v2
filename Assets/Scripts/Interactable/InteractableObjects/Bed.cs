using UnityEngine;

/// <summary>
/// Bed interactable — triggers sound and VFX when interacted with.
/// </summary>
public class Bed : InteractableBase
{
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the bed.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        base.OnInteract(showProp);
    }

    /// <summary>
    /// Called when the player stops interacting with the bed.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract()
    {
        base.OnStopInteract();
    }

    #endregion
}
