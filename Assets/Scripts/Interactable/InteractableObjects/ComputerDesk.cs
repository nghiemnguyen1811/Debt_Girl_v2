using System.Collections;
using UnityEngine;

public class ComputerDesk : InteractableBase
{
    #region === Monitor Visuals ===

    [Header("Monitor Visuals")]
    [SerializeField] private Material monitorMaterial;
    [SerializeField] private Color monitorOnColor;
    [SerializeField] private float blinkInterval = 0.5f;

    private Coroutine blinkCoroutine;
    private Color originalMonitorColor;

    #endregion

    #region === Unity Events ===

    // Cache original monitor color
    protected override void Start()
    {
        base.Start();
        originalMonitorColor = monitorMaterial.color;
    }

    #endregion

    #region === Interactable Overrides ===

    // Triggered when player interacts with the desk
    public override void OnInteract()
    {
        Debug.Log($"Interacted with: {GetObjectName()}");

        SetOutline(false);

        // Restart blinking if already running
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkMonitor());
    }

    // Triggered when player stops interacting
    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(true);
        HandleSound(play: true);
    }

    #endregion

    #region === Monitor Logic ===

    // Coroutine to blink monitor color between on/off
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

        // Reset to original color after blinking ends
        monitorMaterial.color = originalMonitorColor;
    }

    #endregion
}
