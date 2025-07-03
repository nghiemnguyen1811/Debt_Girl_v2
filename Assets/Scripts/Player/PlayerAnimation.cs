using UnityEngine;

/// <summary>
/// Handles player animations including movement and mood-specific layers.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    #region === Animator Setup ===

    [Header(" Animator Reference ")]
    private Animator animator;

    [Header(" Mood Animation ")]
    private int activeMoodLayerIndex;

    private void Start()
    {
        // Get the Animator component from child (e.g., model or rig root)
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
}
