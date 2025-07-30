using UnityEngine;

public class BakingStation : InteractableBase
{
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the Baking Station.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool playSound = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        SetOutline(false);
        SetParticle(true);
        HandleSound(playSound);

        UIManager.Instance.ToggleBakingPanel(true);
    }

    /// <summary>
    /// Called when the player stops interacting with the Baking Station.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);
        HandleSound(play: false);

        UIManager.Instance.ToggleBakingPanel(false);
    }

    #endregion
}
