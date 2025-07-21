using UnityEngine;

public class Stove : InteractableBase
{
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the fridge.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract()
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        SetOutline(false);
        SetParticle(true);
        HandleSound(play: true);

        UIManager.Instance.ToggleCookingPanel(true);
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

        UIManager.Instance.ToggleCookingPanel(false);
    }

    #endregion
}
