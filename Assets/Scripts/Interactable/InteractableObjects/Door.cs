using UnityEngine;

/// <summary>
/// Interactable door that triggers room change.
/// </summary>
public class Door : InteractableBase
{
    [SerializeField] private RoomType roomType;

    #region === Interaction Events ===

    /// <summary>
    /// Called when the player interacts with the door.
    /// Triggers fade and room transition.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        base.OnInteract(showProp);

        RoomManager.Instance.SetActiveRoom(roomType);
    }

    /// <summary>
    /// Called when the player stops interacting with the door.
    /// </summary>
    public override void OnStopInteract()
    {
        base.OnStopInteract();
    }

    #endregion
}
