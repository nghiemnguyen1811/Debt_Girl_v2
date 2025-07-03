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
    private PlayerControl playerControl;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        playerControl = GetComponentInParent<PlayerControl>();

        if (playerControl == null)
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
        if (playerControl?.visualizer == null) return;

        playerControl.animationHandler.ResetMoodLayerWeight();
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

    #endregion
}
