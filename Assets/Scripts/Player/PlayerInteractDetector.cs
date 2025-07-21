using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Detects interactable objects around the player and manages interaction logic.
/// </summary>
[RequireComponent(typeof(PlayerControl))]
public class PlayerInteractDetector : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float fadeSpeed = 3f;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup interactableButton;
    [SerializeField] private Slider durationSlider;

    [Header("Energy Warning Messages")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] energyWarningMessages = {
        "Not enough energy.",
        "You're too tired for that.",
        "Better rest first.",
        "This action requires more energy.",
        "Your energy is too low."
    };

    #endregion

    #region === Private Fields ===

    private PlayerControl control;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private static readonly Vector3 HeightOffset = Vector3.up * 0.25f;

    #endregion

    #region === Public Properties ===

    public InteractableBase CurrentInteractable { get; private set; }
    public bool IsInteracting { get; private set; }

    #endregion

    #region === Unity Events ===

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

    #endregion

    #region === Detection ===

    /// <summary>
    /// Scans for nearby interactables within a radius.
    /// </summary>
    private void DetectInteractable()
    {
        var hits = Physics.OverlapSphere(transform.position + HeightOffset, detectionRadius, interactableLayer);

        if (hits.Length == 0)
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
                if (hit.TryGetComponent(out InteractableBase interactable))
                {
                    CurrentInteractable = interactable;
                    interactable.OnEnter();
                    break;
                }
            }
        }
    }

    #endregion

    #region === UI Handling ===

    /// <summary>
    /// Smoothly toggles the visibility of the interact UI.
    /// </summary>
    private void ToggleUI(bool visible, bool instant = false)
    {
        float targetAlpha = visible ? 1f : 0f;
        float fadeStep = instant ? 1000f : Time.deltaTime * fadeSpeed;

        interactableButton.alpha = Mathf.MoveTowards(interactableButton.alpha, targetAlpha, fadeStep);
        bool shouldShow = interactableButton.alpha > 0.01f;

        if (interactableButton.gameObject.activeSelf != shouldShow)
            interactableButton.gameObject.SetActive(shouldShow);
    }

    #endregion

    #region === Interaction Handling ===

    /// <summary>
    /// Begins interaction with the current interactable.
    /// </summary>
    public void InteractIndicator()
    {
        if (CurrentInteractable == null || IsInteracting) return;

        if (control.stats.energy.current < -CurrentInteractable.GetEnergyAmount())
        {
            string warning = energyWarningMessages[Random.Range(0, energyWarningMessages.Length)];
            UIManager.Instance.ShowWarningText(warning);
            return;
        }

        IsInteracting = true;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        Transform point = CurrentInteractable.GetInteractPoint();

        if (point != null)
            transform.SetPositionAndRotation(point.position, point.rotation);

        if (CurrentInteractable.MoodIconOffset != Vector3.zero)
            control.visualizer?.OffsetMoodIcon(CurrentInteractable.MoodIconOffset);

        string anim = CurrentInteractable.GetAnimationName();

        if (!string.IsNullOrEmpty(anim))
            control.animationHandler.SetBoolParameter(anim, true);

        CurrentInteractable.OnInteract();

        if (CurrentInteractable.ShouldPlayAnimationImmediately())
            StartCoroutine(HandleInteraction(anim, CurrentInteractable.GetDuration()));
    }

    /// <summary>
    /// Executes interaction duration, applies effects, and resets state.
    /// </summary>
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
            control.stats.ApplyStatChange(StatType.Mood, data.moodAmount);
            control.stats.ApplyStatChange(StatType.Productivity, data.energyAmount);

            if (data.earnsMoney)
                MoneyManager.Instance.ChangeMoneys(data.moneyEarned);

            if (data.conditionType != MoodConditionType.None)
                MoodManager.Instance.ClearMood(data.conditionType);
        }

        control.visualizer?.ResetMoodIconPosition();

        yield return new WaitForSeconds(0.5f);
        IsInteracting = false;
    }

    /// <summary>
    /// Immediately stops current interaction and resets state.
    /// </summary>
    public void StopCurrentInteraction()
    {
        if (CurrentInteractable != null)
            CurrentInteractable.OnStopInteract();

        IsInteracting = false;
    }

    #endregion

    #region === Debug ===

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + HeightOffset, detectionRadius);
    }

    #endregion
}
