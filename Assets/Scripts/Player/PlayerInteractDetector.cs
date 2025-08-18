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

    private PlayerControl playerControl;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private static readonly Vector3 HeightOffset = Vector3.up * 0.25f;

    #endregion

    #region === Public Properties ===

    public InteractableBase CurrentInteractable { get; private set; }
    public bool IsInteracting { get; set; }

    #endregion

    #region === Unity Events ===


    private void Start()
    {
        playerControl = GetComponent<PlayerControl>();

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

    #region === Public Interaction API ===

    /// <summary>
    /// Called when player activates interaction (e.g., presses a key).
    /// Checks energy and starts interaction if valid.
    /// </summary>
    public void InteractIndicator()
    {
        if (CurrentInteractable == null || IsInteracting) return;

        if (playerControl.stats.energy.current < -CurrentInteractable.GetEnergyAmount())
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
            playerControl.visualizer?.OffsetMoodIcon(CurrentInteractable.MoodIconOffset);

        var data = CurrentInteractable.Data;
        if (data != null && data.requiredActionType != MoodConditionType.None)
            playerControl.visualizer?.ApplyFaceTextures(data.requiredActionType);


        switch (CurrentInteractable.GetInteractionMode())
        {
            case InteractionPlayMode.Instant:
                PlayInteractionIfValid();
                break;

            case InteractionPlayMode.WaitForConfirm:
                CurrentInteractable.OnInteract(false);
                break;

            case InteractionPlayMode.SoundOnly:
                CurrentInteractable.OnInteract();
                break;
        }
    }

    /// <summary>
    /// Forcibly begins interaction animation and coroutine without checking energy.
    /// Useful for scripted triggers or debug purposes.
    /// </summary>
    public void ForceStartInteraction()
    {
        if (CurrentInteractable == null) return;

        PlayInteractionIfValid();
    }

    /// <summary>
    /// Immediately cancels the current interaction and resets state.
    /// </summary>
    public void StopCurrentInteraction()
    {
        if (CurrentInteractable != null)
            CurrentInteractable.OnStopInteract();

        IsInteracting = false;
    }

    #endregion

    #region === Private Interaction Logic ===

    /// <summary>
    /// Plays the animation and starts the interaction coroutine
    /// only if the interactable is set to play animation immediately.
    /// </summary>
    private void PlayInteractionIfValid()
    {
        CurrentInteractable.OnInteract();

        string anim = CurrentInteractable.GetAnimationName();

        if (!string.IsNullOrEmpty(anim))
            playerControl.animationHandler.SetBoolParameter(anim, true);

        StartCoroutine(HandleInteraction(anim, CurrentInteractable.GetDuration()));
    }

    /// <summary>
    /// Runs the interaction duration, shows slider, applies effects, resets state.
    /// </summary>
    /// <param name="animName">The animation name to stop at the end.</param>
    /// <param name="duration">How long the interaction lasts.</param>
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

        if (!string.IsNullOrEmpty(animName))
            playerControl.animationHandler.SetBoolParameter(animName, false);

        transform.SetPositionAndRotation(originalPosition, originalRotation);
        CurrentInteractable.OnStopInteract();

        var data = CurrentInteractable.Data;

        if (data != null)
        {
            playerControl.stats.ApplyStatChange(StatType.Mood, data.moodAmount);
            playerControl.stats.ApplyStatChange(StatType.Productivity, data.energyAmount);

            if (data.earnsMoney)
                MoneyManager.Instance.ChangeMoneys(data.moneyEarned);

            MoodManager.Instance.SetCurrentMoodVisual();

            if (data.clearsMoodType != MoodConditionType.None)
                MoodManager.Instance.ClearMood(data.clearsMoodType);
        }

        playerControl.visualizer?.ResetMoodIconPosition();

        yield return new WaitForSeconds(0.5f);
        IsInteracting = false;
    }

    #endregion

    #region === Detection ===

    /// <summary>
    /// Scans for nearby interactable objects using overlap sphere.
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
    /// Smoothly fades in/out the interact UI.
    /// </summary>
    /// <param name="visible">Whether the UI should be visible.</param>
    /// <param name="instant">Skip fade animation if true.</param>
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

    #region === Debug ===

    /// <summary>
    /// Draws detection radius in editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + HeightOffset, detectionRadius);
    }

    #endregion
}