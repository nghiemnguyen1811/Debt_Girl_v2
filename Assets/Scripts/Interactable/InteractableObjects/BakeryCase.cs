using UnityEngine;

public class BakeryCase : InteractableBase
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

        UIManager.Instance.ToggleCakeInventoryPanel(true);
    }

    /// <summary>
    /// Called when the player stops interacting with the fridge.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract()
    {
        base.OnStopInteract();

        UIManager.Instance.ToggleCakeInventoryPanel(false);
    }

    #endregion
}
