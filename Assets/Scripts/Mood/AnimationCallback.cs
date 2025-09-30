using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Handles animation event callbacks (e.g. footsteps, mood sounds, animation transitions).
/// Attach this to the character model and call methods from animation events.
/// </summary>
public class AnimationCallback : MonoBehaviour
{
    #region === References ===

    [Header(" Elements ")]
    private PlayerControl control;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        control = GetComponentInParent<PlayerControl>();

        if (control == null)
            Debug.LogWarning("[AnimationCallback] PlayerControl not found in parent.");
    }

    #endregion

    #region === Animation Event Callbacks ===

    /// <summary>
    /// Call this from an animation event (e.g. Yawn animation).
    /// Ends mood-related animation by resetting the mood layer.
    /// </summary>
    public void EndMoodAnimation()
    {
        if (control?.visualizer == null) return;

        control.animationHandler.ResetMoodLayerWeight();
    }

    /// <summary>
    /// Call this from a footstep animation event.
    /// Plays a footstep sound based on which foot is used.
    /// </summary>
    /// <param name="footSide">Index representing left or right foot.</param>
    public void PlayFootstep(int footSide)
    {
        AudioManager.Instance.PlayFootstep(footSide);
    }

    /// <summary>
    /// Call this from a mood animation event.
    /// Plays a mood-specific sound.
    /// </summary>
    /// <param name="moodIndex">Index of the mood sound.</param>
    public void PlayMoodSound(int moodIndex)
    {
        AudioManager.Instance.PlayMoodSound(moodIndex);
    }

    /// <summary>
    /// Called from an animation event when the character takes out the phone.
    /// Opens the phone UI panel.
    /// </summary>
    public void OnPhoneTakenOut()
    {
        UIManager.Instance.TogglePhonePanel(true);
    }

    /// <summary>
    /// Called from an animation event when the character puts the phone away.
    /// Resets the phone active state in the animation handler.
    /// </summary>
    public void OnPhonePutAway()
    {
        control.animationHandler.IsPhoneActive = false;
        control.propSwitcher.SetPropActiveByType(InteractionPropType.Phone, false);
    }

    #endregion
}
