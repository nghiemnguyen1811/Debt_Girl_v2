using UnityEngine;
using System.Collections;
using EPOOutline;

public class ComputerDesk : MonoBehaviour, IInteractable
{
    [Header("Elements")]
    [SerializeField] private Transform interactPoint;

    [Header("Interactable Data")]
    [SerializeField] private InteractableDataSO data;

    [Header("Monitor Visuals")]
    [SerializeField] private Material monitorMaterial;
    [SerializeField] private Color monitorOnColor;
    [SerializeField] private float blinkInterval = 0.5f;

    [Header("Mood Icon Offset")]
    [SerializeField] private Vector3 moodIconOffset;

    [Header("Visual Effect")]
    [SerializeField] private GameObject interactParticle;

    private Coroutine blinkCoroutine;
    private Color originalMonitorColor;

    // === IInteractable Properties ===
    public Outlinable Outlinable => GetComponent<Outlinable>();
    public InteractableDataSO Data => data;
    public Transform GetInteractPoint() => interactPoint;
    public Vector3 MoodIconOffset => moodIconOffset;
    public GameObject InteractParticle => interactParticle;

    public string GetObjectName() => data != null ? data.objectName : "Unknown";
    public string GetAnimationName() => data != null ? data.animationName : string.Empty;
    public float GetDuration() => data != null ? data.interactionDuration : 0f;

    private void Start()
    {
        originalMonitorColor = monitorMaterial.color;
        SetOutline(true);
        SetParticle(false);
    }

    public void OnEnter() => SetOutline(true);
    public void OnExit() => SetOutline(false);

    public void OnInteract()
    {
        Debug.Log($"Đã nhấn vào: {GetObjectName()}");

        SetOutline(false);

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkMonitor());
    }

    public void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(true);
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

    // === Helper Methods ===
    private void SetOutline(bool enabled)
    {
        if (Outlinable != null)
            Outlinable.enabled = enabled;
    }

    private void SetParticle(bool enabled)
    {
        if (interactParticle != null && interactParticle.activeSelf != enabled)
            interactParticle.SetActive(enabled);
    }
}
