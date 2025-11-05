using System;
using UnityEngine;

/// <summary>
/// Handles player animations including movement, mood layers, and phone usage.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    #region === Inspector References ===

    [Header("Character Models")]
    [SerializeField] private Transform[] modelArray;

    [Header(" Avatar Mask ")]
    [SerializeField] private Avatar[] avatars;

    #endregion

    #region === Runtime References ===

    private PlayerControl playerControl;
    private Animator animator;
    private Animator animatorPreview;

    #endregion

    #region === State Values ===

    private int activeMoodLayerIndex;
    public bool IsPhoneActive { get; set; }

    #endregion

    #region === Unity Lifecycle ===

    private void Start()
    {
        playerControl = GetComponent<PlayerControl>();
        animator = GetComponentInChildren<Animator>();
        animatorPreview = modelArray[1].GetComponent<Animator>();

        // Subscribe to profile change
        if (playerControl != null)
            playerControl.OnCharacterProfileChanged += HandleCharacterProfileChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerControl != null)
            playerControl.OnCharacterProfileChanged -= HandleCharacterProfileChanged;
    }

    #endregion

    #region === Event Handlers ===

    /// <summary>
    /// Called whenever the player's character profile changes.
    /// Updates the active model and animator reference.
    /// </summary>
    private void HandleCharacterProfileChanged(CharacterInfoSO newProfile)
    {
        // Disable all models first
        foreach (Transform models in modelArray)
            foreach (Transform model in models)
                model.gameObject.SetActive(false);

        // Convert enum to int index
        int index = (int)newProfile.characterType;

        if (index >= 0)
        {
            foreach (Transform models in modelArray)
                models.GetChild(index - 1).gameObject.SetActive(true);

            animator.avatar = avatars[index - 1];
            animatorPreview.avatar = avatars[index - 1];
        }

        else Debug.LogWarning($"[PlayerAnimation] Invalid characterType index: {index}");
    }

    #endregion

    #region === Movement Animations ===

    /// <summary>
    /// Sets the movement speed float parameter to drive blend trees.
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        animator.SetFloat("MoveSpeed", speed);
    }

    /// <summary>
    /// Sets a boolean parameter, and resets mood layer if needed.
    /// </summary>
    public void SetBoolParameter(string parameterName, bool value)
    {
        ResetMoodLayerWeight();
        animator.SetBool(parameterName, value);
    }

    /// <summary>
    /// Triggers an animation using a trigger parameter.
    /// </summary>
    public void SetTrigger(string triggerName)
    {
        if (animator == null) return;

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Plays a preview animation (e.g., idle, spin, pose) on the preview outfit animator.
    /// </summary>
    public void PlayPreviewAnimation(string triggerName)
    {
        if (animatorPreview == null) return;

        animatorPreview.ResetTrigger(triggerName);
        animatorPreview.SetTrigger(triggerName);
    }

    #endregion

    #region === Mood Animations ===

    /// <summary>
    /// Plays a mood-specific animation on a separate layer.
    /// </summary>
    public void SetMoodTrigger(string triggerName, int layerIndex)
    {
        if (IsPhoneActive) return;

        activeMoodLayerIndex = layerIndex;
        animator.SetLayerWeight(layerIndex, 1f);
        animator.ResetTrigger(triggerName); // Ensure retrigger
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Resets the weight of the active mood layer to zero.
    /// </summary>
    public void ResetMoodLayerWeight()
    {
        animator.SetLayerWeight(activeMoodLayerIndex, 0f);
    }

    #endregion

    #region === Phone Animations ===

    /// <summary>
    /// Show phone animation and UI.
    /// </summary>
    public void ShowPhone()
    {
        if (playerControl.interactDetector.IsInteracting) return;

        IsPhoneActive = true;
        ResetMoodLayerWeight();

        playerControl.propSwitcher.SetPropActiveByType(InteractionPropType.Phone, true);
        SetPhoneAnimationState(4, true);
    }

    /// <summary>
    /// Hide phone animation and UI.
    /// </summary>
    public void HidePhone()
    {
        if (playerControl.interactDetector.IsInteracting) return;

        UIManager.Instance.TogglePhonePanel(false);
        SetPhoneAnimationState(4, false);
    }

    /// <summary>
    /// Sets the phone animation state on a given layer.
    /// </summary>
    public void SetPhoneAnimationState(int layerIndex, bool isActive)
    {
        activeMoodLayerIndex = layerIndex;
        animator.SetLayerWeight(layerIndex, 1f);
        animator.SetBool("UsePhone", isActive);

        AudioManager.Instance.PlayInteractSound(8);
    }

    #endregion
}
