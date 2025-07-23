using UnityEngine;

public class Fridge : InteractableBase
{
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the fridge.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool playSound = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        SetOutline(false);
        SetParticle(true);
        HandleSound(playSound);

        UIManager.Instance.ToggleInventoryPanel(true);
    }

    /// <summary>
    /// Called when the player stops interacting with the fridge.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);
        HandleSound(play: false);

        UIManager.Instance.ToggleInventoryPanel(false);
    }

    #endregion
}
