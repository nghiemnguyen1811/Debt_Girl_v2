using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(PlayerControl))]
public class PlayerInteractDetector : MonoBehaviour
{
    [Header(" Spawn Particle ")]
    [SerializeField] private Transform moneyVFXPoint;

    [Header("Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float fadeSpeed = 3f;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup interactableButton;
    [SerializeField] private Slider durationSlider;

    private PlayerControl control;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    public IInteractable CurrentInteractable { get; private set; }
    public bool IsInteracting { get; private set; }

    private static readonly Vector3 HeightOffset = Vector3.up * 0.25f;

    private void Start()
    {
        control = GetComponent<PlayerControl>();
        ToggleUI(false, true);
        durationSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsInteracting) return;

        DetectInteractable();
        ToggleUI(CurrentInteractable != null);
    }

    private void DetectInteractable()
    {
        var hits = Physics.OverlapSphere(transform.position + HeightOffset, detectionRadius, interactableLayer);

        if (hits.Length <= 0)
        {
            if (CurrentInteractable != null)
            {
                CurrentInteractable.OnExit();
                CurrentInteractable = null;
            }
            return;
        }

        if (CurrentInteractable == null)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out IInteractable interactable))
                {
                    CurrentInteractable = interactable;
                    interactable.OnEnter();
                    break;
                }
            }
        }
    }

    private void ToggleUI(bool visible, bool instant = false)
    {
        float targetAlpha = visible ? 1f : 0f;
        float fadeStep = instant ? 1000f : Time.deltaTime * fadeSpeed;

        interactableButton.alpha = Mathf.MoveTowards(interactableButton.alpha, targetAlpha, fadeStep);
        bool shouldShow = interactableButton.alpha > 0.01f;
        if (interactableButton.gameObject.activeSelf != shouldShow)
            interactableButton.gameObject.SetActive(shouldShow);
    }

    public void InteractIndicator()
    {
        if (CurrentInteractable == null || IsInteracting) return;

        IsInteracting = true;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        Transform point = CurrentInteractable.GetInteractPoint();

        if (point != null)
            transform.SetPositionAndRotation(point.position, point.rotation);

        // Set mood icon offset position if defined
        control.visualizer?.OffsetMoodIcon(CurrentInteractable.MoodIconOffset);

        string anim = CurrentInteractable.GetAnimationName();
        control.animationHandler.SetBoolParameter(anim, true);
        CurrentInteractable.OnInteract();
        StartCoroutine(HandleInteraction(anim, CurrentInteractable.GetDuration()));
    }

    private IEnumerator HandleInteraction(string animName, float duration)
    {
        durationSlider.gameObject.SetActive(true);
        durationSlider.value = 0f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            durationSlider.value = t / duration;
            yield return null;
        }

        durationSlider.value = 1f;
        durationSlider.gameObject.SetActive(false);

        control.animationHandler.SetBoolParameter(animName, false);
        transform.SetPositionAndRotation(originalPosition, originalRotation);

        CurrentInteractable.OnStopInteract();

        var data = CurrentInteractable.Data;

        if (data != null)
        {
            if (data.experienceAmount > 0)
                control.stats.GainExperience(data.experienceAmount);

            switch (data.affectType)
            {
                case AffectType.Mood:
                    control.stats.ApplyMoodChange(data.moodAmount);
                    break;

                case AffectType.Energy:
                    control.stats.ApplyEnergyChange(data.energyAmount);
                    break;

                case AffectType.Both:
                    control.stats.ApplyMoodChange(data.moodAmount);
                    control.stats.ApplyEnergyChange(data.energyAmount);
                    break;
            }
        }

        if (data.earnsMoney)
            MoneyManager.Instance.AddCoins(data.moneyEarned, moneyVFXPoint.position);

        var currentMood = MoodManager.Instance.GetActiveMood();

        if (currentMood != null && currentMood.conditionType == data.conditionType)
            MoodManager.Instance.ClearMood();

        // Reset mood icon position
        control.visualizer?.ResetMoodIconPosition();

        yield return new WaitForSeconds(0.5f);
        IsInteracting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + HeightOffset, detectionRadius);
    }
}
