using System.Collections;
using UnityEngine;

public class ComputerDesk : InteractableBase
{
    [Header("Monitor Visuals")]
    [SerializeField] private Material monitorMaterial;
    [SerializeField] private Color monitorOnColor;
    [SerializeField] private float blinkInterval = 0.5f;

    private Coroutine blinkCoroutine;
    private Color originalMonitorColor;

    private void Start()
    {
        originalMonitorColor = monitorMaterial.color;

        SetOutline(true);
        SetParticle(false);
    }

    public override void OnInteract()
    {
        Debug.Log($"Đã nhấn vào: {GetObjectName()}");
        SetOutline(false);
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkMonitor());
    }

    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(true);
        AudioManager.Instance.PlayInteractSound(0);
    }

    private IEnumerator BlinkMonitor()
    {
        bool useOriginal = false;
        float timer = 0f;
        WaitForSeconds wait = new WaitForSeconds(blinkInterval);

        while (timer < GetDuration())
        {
            monitorMaterial.color = useOriginal ? originalMonitorColor : monitorOnColor;
            useOriginal = !useOriginal;
            yield return wait;
            timer += blinkInterval;
        }

        monitorMaterial.color = originalMonitorColor;
    }
}
