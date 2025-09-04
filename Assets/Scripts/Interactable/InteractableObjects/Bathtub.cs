using UnityEngine;

/// <summary>
/// Bathtub interactable — triggers sound, VFX, and curtain when interacted with.
/// </summary>
public class Bathtub : InteractableBase
{
    [Header("Bathtub Cover")]
    [Tooltip("Optional screen/cover object that shows or hides when interacting.")]
    [SerializeField] private GameObject coverObject;

    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the bathtub.
    /// Disables outline, enables particles, plays sound, and shows cover.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        if (coverObject != null)
            coverObject.SetActive(true);

        base.OnInteract(showProp);
    }

    /// <summary>
    /// Called when the player stops interacting with the bathtub.
    /// Re-enables outline, disables particles, stops sound, and hides cover.
    /// </summary>
    public override void OnStopInteract()
    {
        if (coverObject != null)
            coverObject.SetActive(false);

        base.OnStopInteract();
    }

    #endregion
}
