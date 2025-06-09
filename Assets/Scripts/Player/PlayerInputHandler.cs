using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header(" Elements ")]
    private InputSystem_Actions input;

    [Header(" Settings ")]
    private Vector2 moveInput;

    void Awake()
    {
        input = new InputSystem_Actions();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += _ => moveInput = Vector2.zero;
    }

    void OnEnable() => input.Player.Enable();
    void OnDisable() => input.Player.Disable();

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }
}
