using UnityEngine;

public class Bed : InteractableBase
{
    public override void OnInteract()
    {
        Debug.Log($"Đã nhấn vào: {GetObjectName()}");
        SetOutline(false);
        SetParticle(true);

        HandleSound(play: true);
    }

    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);

        HandleSound(play: false);
    }
}
