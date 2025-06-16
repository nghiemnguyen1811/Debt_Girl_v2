using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void SetMovementSpeed(float speed)
    {
        animator.SetFloat("MoveSpeed", speed);
    }

    public void SetBoolParameter(string parameterName, bool value)
    {
        animator.SetBool(parameterName, value);
    }

    public void SetTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }
}
