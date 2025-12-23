using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using URandom = UnityEngine.Random;

/// <summary>
/// Detects interactable objects around the player using Physics.OverlapSphere
/// and manages the interaction sequence (animation, UI, stat changes).
/// </summary>
[RequireComponent(typeof(PlayerControl))]
public class PlayerInteractDetector : MonoBehaviour
{
    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Interaction Config")]
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float earnMoneyMultiplier = 1.3f;

    [Header("UI References")]
    [SerializeField] private CanvasGroup interactableButton;
    [SerializeField] private Slider durationSlider;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Internal State ===

    private PlayerControl control;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private static readonly Vector3 HeightOffset = Vector3.up * 0.25f;

    public InteractableBase CurrentInteractable { get; private set; }
    public bool IsInteracting { get; set; }

    #endregion

    //─────────────────────────────────────────────────────────────
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + HeightOffset, detectionRadius);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Interaction API ===

    /// <summary>
    /// Called by Input (e.g., F key or button). Validates energy/cooldown
    /// and starts the interaction sequence.
    /// </summary>
    public void InteractIndicator()
    {
        if (CurrentInteractable == null || IsInteracting || control.animationHandler.IsPhoneActive) return;

        // 1. Validate Character Type
        if (CurrentInteractable.AllowedCharacter != CharacterType.All &&
            control.CharacterProfile.characterType != CurrentInteractable.AllowedCharacter)
            return;

        // 2. Validate Cooldown
        if (CurrentInteractable is ICooldownInteractable cooldown && cooldown.IsOnCooldown(out float remain))
        {
            cooldown.ShowCooldownWarning(remain);
            return;
        }

        // 3. Validate Energy
        if (control.stats.energy.current < -CurrentInteractable.GetEnergyAmount())
        {
            GameManager.Instance.ShowEnergyWarning();
            return;
        }

        StartInteractionSequence();
    }

    /// <summary>
    /// Forcibly begins interaction animation without checking energy.
    /// </summary>
    public void ForceStartInteraction()
    {
        if (CurrentInteractable == null) return;
        PlayInteractionIfValid();
    }

    /// <summary>
    /// Cancels interaction immediately.
    /// </summary>
    public void StopCurrentInteraction()
    {
        if (CurrentInteractable != null)
            CurrentInteractable.OnStopInteract();

        IsInteracting = false;
    }

    public void ClearCurrentInteractable()
    {
        IsInteracting = true;
        CurrentInteractable = null;
        ToggleUI(false, true);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Interaction Logic Flow ===

    private void StartInteractionSequence()
    {
        IsInteracting = true;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Snap to position
        Transform point = CurrentInteractable.GetInteractPoint();
        if (point != null)
            transform.SetPositionAndRotation(point.position, point.rotation);

        // Visual FX
        if (CurrentInteractable.MoodIconOffset != Vector3.zero)
            control.visualizer?.OffsetMoodIcon(CurrentInteractable.MoodIconOffset);

        var data = CurrentInteractable.Data;
        if (data != null && data.requiredActionType != MoodConditionType.None)
            control.visualizer?.ApplyFaceTextures(data.requiredActionType);

        // Execute Mode
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

    private void PlayInteractionIfValid()
    {
        CurrentInteractable.OnInteract();

        string anim = CurrentInteractable.GetAnimationName();
        if (!string.IsNullOrEmpty(anim))
            control.animationHandler.SetBoolParameter(anim, true);

        StartCoroutine(HandleInteraction(anim, CurrentInteractable.GetDuration()));
    }

    private IEnumerator HandleInteraction(string animName, float duration)
    {
        // Show progress bar
        durationSlider.gameObject.SetActive(true);
        durationSlider.value = 0f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            durationSlider.value = t / duration;
            yield return null;
        }

        durationSlider.value = 1f;
        durationSlider.gameObject.SetActive(false);

        // End Animation
        if (!string.IsNullOrEmpty(animName))
            control.animationHandler.SetBoolParameter(animName, false);

        transform.SetPositionAndRotation(originalPosition, originalRotation);
        CurrentInteractable.OnStopInteract();

        ApplyInteractionResults();

        control.visualizer?.ResetMoodIconPosition();
        yield return new WaitForSeconds(0.5f);
        IsInteracting = false;
    }

    private void ApplyInteractionResults()
    {
        var data = CurrentInteractable.Data;
        if (data != null)
        {
            control.stats.ApplyStatChange(StatType.Mood, data.moodAmount);
            control.stats.ApplyStatChange(StatType.Productivity, data.energyAmount);

            if (data.earnsMoney)
            {
                int level = StatUpgradeManager.Instance.GetLevelOf(StatType.IncomeRate);
                double moneyEarned = Math.Round(data.moneyEarned * Math.Pow(earnMoneyMultiplier, level - 1), 2);
                MoneyManager.Instance.ChangeMoneys(moneyEarned);
            }

            MoodManager.Instance.SetCurrentMoodVisual();

            if (data.clearsMoodType != MoodConditionType.None)
                MoodManager.Instance.ClearMood(data.clearsMoodType);
        }
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Detection & UI ===

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
}