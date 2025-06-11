using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerAnimation : MonoBehaviour
{
    [Header(" Elements ")]
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    public void UpdateMovementAnimation(float moveAmount)
    {
        anim.SetFloat("MoveSpeed", moveAmount);
    }
}
