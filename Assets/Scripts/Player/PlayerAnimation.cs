using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header(" Animator Reference ")]
    private Animator animator;

    [Header(" Mood Animation ")]
    private int activeMoodLayerIndex;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    // === Movement ===
    public void SetMovementSpeed(float speed)
    {
        animator.SetFloat("MoveSpeed", speed);
    }

    public void SetBoolParameter(string parameterName, bool value)
    {
        ResetMoodLayerWeight();
        animator.SetBool(parameterName, value);
    }

    public void SetTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    // === Mood Layer ===
    public void SetMoodTrigger(string triggerName, int layerIndex)
    {
        activeMoodLayerIndex = layerIndex;
        animator.SetLayerWeight(layerIndex, 1f);
        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    public void ResetMoodLayerWeight()
    {
        animator.SetLayerWeight(activeMoodLayerIndex, 0f);
    }
}
