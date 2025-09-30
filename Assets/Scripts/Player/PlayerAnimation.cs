using UnityEngine;

/// <summary>
/// Handles player animations including movement and mood-specific layers.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    #region === References ===

    private PlayerControl control;
    private Animator animator;

    #endregion

    #region === Values ===

    private int activeMoodLayerIndex;
    public bool IsPhoneActive { get; set; }

    #endregion

    #region === Animator Setup ===

    private void Start()
    {
        control = GetComponent<PlayerControl>();
        animator = GetComponentInChildren<Animator>();
    }

    #endregion

    #region === Movement Control ===

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
        animator.SetTrigger(triggerName);
    }

    #endregion

    #region === Mood Animation Layer ===

    /// <summary>
    /// Plays a mood-specific animation on a separate layer.
    /// </summary>
    /// <param name="triggerName">Trigger parameter name</param>
    /// <param name="layerIndex">Animator layer index for mood</param>
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

    #region === Phone Animation Layer ===

    /// <summary>
    /// Show phone animation and UI.
    /// </summary>
    public void ShowPhone()
    {
        if (control.interactDetector.IsInteracting) return;

        IsPhoneActive = true;
        ResetMoodLayerWeight();
        control.propSwitcher.SetPropActiveByType(InteractionPropType.Phone, true);
        SetPhoneAnimationState(4, true);
    }

    /// <summary>
    /// Hide phone animation and UI.
    /// </summary>
    public void HidePhone()
    {
        if (control.interactDetector.IsInteracting) return;

        UIManager.Instance.TogglePhonePanel(false);
        SetPhoneAnimationState(4, false);
    }

    /// <summary>
    /// Set phone layer and bool state.
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
