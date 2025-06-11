// === PlayerInteractDetector.cs ===
using UnityEngine;

public class PlayerInteractDetector : MonoBehaviour
{
    [Header(" Settings ")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;

    [Header(" Interactable Button ")]
    [SerializeField] private CanvasGroup interactableButton;
    [SerializeField] private float fadeSpeed = 3f;

    public IInteractable CurrentInteractable { get; private set; }
    public bool IsInteracting { get; private set; } = false;

    private void Start()
    {
        interactableButton.alpha = 0f;
        interactableButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        ScanForInteractable();
        UpdateInteractableButton();
    }

    private void ScanForInteractable()
    {
        CurrentInteractable = null;

        Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.25f, detectionRadius, interactableLayer);

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable != null)
            {
                CurrentInteractable = interactable;
                break;
            }
        }
    }

    private void UpdateInteractableButton()
    {
        bool shouldShow = CurrentInteractable != null;
        HandleUIFade(shouldShow);
    }

    private void HandleUIFade(bool shouldShow)
    {
        if (shouldShow)
        {
            if (!interactableButton.gameObject.activeSelf)
                interactableButton.gameObject.SetActive(true);

            interactableButton.alpha = Mathf.MoveTowards(interactableButton.alpha, 1f, Time.deltaTime * fadeSpeed);
        }
        else
        {
            interactableButton.alpha = Mathf.MoveTowards(interactableButton.alpha, 0f, Time.deltaTime * fadeSpeed);

            if (interactableButton.alpha <= 0.01f && interactableButton.gameObject.activeSelf)
                interactableButton.gameObject.SetActive(false);
        }
    }

    public void InteractIndicator()
    {
        if (CurrentInteractable == null) return;

        IsInteracting = true;
        Debug.Log("Tương tác với: " + CurrentInteractable.GetObjectName());
        CurrentInteractable.OnInteract();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.25f, detectionRadius);
    }
}
