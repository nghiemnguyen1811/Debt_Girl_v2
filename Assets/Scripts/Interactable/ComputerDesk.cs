using UnityEngine;
using System.Collections;
using EPOOutline;

public class ComputerDesk : MonoBehaviour, IInteractable
{
    [Header(" Elements ")]
    [SerializeField] private Transform interactPoint;

    [Header(" Interactable Data ")]
    [SerializeField] private InteractableDataSO data;

    [Header(" Monitor Material ")]
    [SerializeField] private Material monitorMaterial;
    [SerializeField] private float blinkInterval = 0.5f;

    private Coroutine blinkCoroutine;


    public Outlinable Outlinable => GetComponent<Outlinable>();
    public InteractableDataSO Data => data;

    public string GetObjectName() => data != null ? data.objectName : "Unknown";
    public string GetAnimationName() => data != null ? data.animationName : string.Empty;
    public float GetDuration() => data != null ? data.interactionDuration : 0f;
    public Transform GetInteractPoint() => interactPoint;

    private void Start()
    {
        if (Outlinable != null)
            Outlinable.enabled = false;
    }

    public void OnEnter()
    {
        if (Outlinable != null)
            Outlinable.enabled = true;
    }

    public void OnExit()
    {
        if (Outlinable != null)
            Outlinable.enabled = false;
    }

    public void OnInteract()
    {
        Debug.Log($"Đã nhấn vào: {GetObjectName()}");

        if (Outlinable != null)
            Outlinable.enabled = false;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkMonitor());
    }

    private IEnumerator BlinkMonitor()
    {
        Color originalColor = monitorMaterial.color;
        bool useOriginalColor = false;
        float timer = 0f;
        WaitForSeconds wait = new WaitForSeconds(blinkInterval);

        while (timer < GetDuration())
        {
            monitorMaterial.color = useOriginalColor ? originalColor : Color.black;
            useOriginalColor = !useOriginalColor;

            yield return wait;
            timer += blinkInterval;
        }

        monitorMaterial.color = originalColor;
    }

    public void OnStopInteract()
    {
        if (Outlinable != null)
            Outlinable.enabled = true;
    }
}
