using UnityEngine;

public class Dirt : InteractableBase
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 0.5f;

    public float GetDetectionRadius() => detectionRadius;

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
    }

    public override void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);

        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}
