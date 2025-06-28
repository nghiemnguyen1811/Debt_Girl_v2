using UnityEngine;

public class Bed : InteractableBase
{
    private void Start()
    {
        SetOutline(true);
        SetParticle(false);
    }

    public override void OnInteract()
    {
        Debug.Log($"Đã nhấn vào: {GetObjectName()}");
        SetOutline(false);
        SetParticle(true);
        AudioManager.Instance.PlayInteractSound(1);
    }

    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);
        AudioManager.Instance.StopSound(1);
    }
}
