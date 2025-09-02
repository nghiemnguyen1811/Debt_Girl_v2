using UnityEngine;

/// <summary>
/// Sink interactable — triggers sound and VFX when interacted with.
/// </summary>
public class Sink : InteractableBase
{
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the sink.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        base.OnInteract(showProp);
    }

    /// <summary>
    /// Called when the player stops interacting with the sink.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract()
    {
        base.OnStopInteract();
    }

    #endregion
}
