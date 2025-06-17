using EPOOutline;
using UnityEngine;

public class Bed : MonoBehaviour, IInteractable
{
    [Header(" Elements ")]
    [SerializeField] private Transform interactPoint;

    [Header(" Interactable Data ")]
    [SerializeField] private InteractableDataSO data;

    [Header("Mood Icon Offset")]
    [SerializeField] private Vector3 moodIconOffset;


    public Outlinable Outlinable => GetComponent<Outlinable>();
    public InteractableDataSO Data => data;
    public Transform GetInteractPoint() => interactPoint;
    public Vector3 MoodIconOffset => moodIconOffset;

    public string GetObjectName() => data != null ? data.objectName : "Unknown";
    public string GetAnimationName() => data != null ? data.animationName : string.Empty;
    public float GetDuration() => data != null ? data.interactionDuration : 0f;

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
    }

    public void OnStopInteract()
    {
        if (Outlinable != null)
            Outlinable.enabled = true;
    }
}
