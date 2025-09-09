using UnityEngine;

/// <summary>
/// Shower Enclosure interactable — triggers sound and VFX when interacted with.
/// </summary>
public class ShowerEnclosure : InteractableBase
{
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the shower enclosure.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        base.OnInteract(showProp);
    }

    /// <summary>
    /// Called when the player stops interacting with the shower enclosure.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract()
    {
        base.OnStopInteract();
    }

    #endregion
}
