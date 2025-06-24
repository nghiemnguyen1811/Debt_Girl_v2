using EPOOutline;
using UnityEngine;

public class Bed : MonoBehaviour, IInteractable
{
    [Header("Elements")]
    [SerializeField] private Transform interactPoint;

    [Header("Interactable Data")]
    [SerializeField] private InteractableDataSO data;

    [Header("Mood Icon Offset")]
    [SerializeField] private Vector3 moodIconOffset;

    [Header("Visual Effect")]
    [SerializeField] private GameObject interactParticle;

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
        SetOutline(true);
        SetParticle(false);
    }

    public void OnEnter()
    {
        SetOutline(true);
    }

    public void OnExit()
    {
        SetOutline(false);
    }

    public void OnInteract()
    {
        Debug.Log($"Đã nhấn vào: {GetObjectName()}");

        SetOutline(false);
        SetParticle(true);
    }

    public void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);
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
