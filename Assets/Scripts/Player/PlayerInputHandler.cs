using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement input using the Unity Input System.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    #region === Input Variables ===

    private InputSystem_Actions input;
    private Vector2 moveInput;

    #endregion

    #region === Unity Events ===

    private void Awake()
    {
        // Initialize input actions
        input = new InputSystem_Actions();

        // Listen for movement input (WASD/joystick)
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += _ => moveInput = Vector2.zero;
    }

    private void OnEnable()
    {
        // Enable player input when object is active
        input.Player.Enable();
    }

    private void OnDisable()
    {
        // Disable player input to prevent memory leaks
        input.Player.Disable();
    }

    #endregion

    #region === Public Accessors ===

    /// <summary>
    /// Returns the current movement direction as a Vector2.
    /// </summary>
    public Vector2 GetMoveInput() => moveInput;

    #endregion
}
