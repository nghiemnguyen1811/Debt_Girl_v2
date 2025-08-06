using UnityEngine;

public class Fridge : InteractableBase
{
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the fridge.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        base.OnInteract(showProp);

        UIManager.Instance.ToggleInventoryPanel(true);
    }

    /// <summary>
    /// Called when the player stops interacting with the fridge.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract()
    {
        base.OnStopInteract();

        UIManager.Instance.ToggleInventoryPanel(false);
    }

    #endregion
}
