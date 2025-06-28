using UnityEngine;
using UnityEngine.Audio;

public class AnimationCallback : MonoBehaviour
{
    [Header(" Elements ")]
    private PlayerControl playerControl;

    void Start()
    {
        playerControl = GetComponentInParent<PlayerControl>();

        if (playerControl == null)
        {
            Debug.LogWarning("[MoodAnimationCallback] PlayerControl not found in parent.");
        }
    }

    // Call this function from an Animation Event in the Yawn animation, and pass in the layerIndex parameter.
    public void EndMoodAnimation()
    {
        if (playerControl?.visualizer == null) return;

        playerControl.animationHandler.ResetMoodLayerWeight();
    }

    // Call this function from an Animation Event in the Run animation
    public void PlayFootstep(int footSide)
    {
        AudioManager.Instance.PlayFootstep(footSide);
    }

    // Call this function from an Animation Event
    public void PlayMoodSound(int moodIndex)
    {
        AudioManager.Instance.PlayMoodSound(moodIndex);
    }
}
