using UnityEngine;

/// <summary>
/// Handles player movement using CharacterController and joystick input.
/// Applies camera-relative direction, gravity, and smooth rotation.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerControl))]
public class PlayerMovement : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("Input")]
    [SerializeField] private DynamicJoystick joystick;

    [Header("Visual")]
    [SerializeField] private Transform modelTransform;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = -9.81f;

    #endregion

    #region === Private Fields ===

    private CharacterController controller;
    private PlayerControl control;
    private Transform cameraTransform;

    private float verticalVelocity;
    private const float groundCheckOffset = 0.1f;
    private const float groundCheckDistance = 0.15f;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        control = GetComponent<PlayerControl>();
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main?.transform;
    }

    private void Update()
    {
        // Disable movement while interacting
        if (control.interactDetector?.IsInteracting == true)
        {
            if (modelTransform != null)
                modelTransform.localRotation = Quaternion.identity;
            return;
        }

        Vector2 moveInput = joystick.direction.sqrMagnitude > 0.01f
            ? joystick.direction
            : control.inputHandler.GetMoveInput();

        Vector3 moveDirection = CalculateMoveDirection(moveInput);

        HandleRotation(moveDirection);
        control.animationHandler?.SetMovementSpeed(moveDirection.magnitude);
        ApplyGravity(ref moveDirection);

        controller.Move(moveDirection * speed * Time.deltaTime);
    }

    #endregion

    #region === Movement Helpers ===

    /// <summary>
    /// Converts 2D joystick input into 3D camera-relative direction.
    /// </summary>
    private Vector3 CalculateMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.01f || cameraTransform == null)
            return Vector3.zero;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Ignore vertical direction
        forward.y = right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (right * input.x + forward * input.y).normalized;
    }

    /// <summary>
    /// Smoothly rotates the model toward the movement direction.
    /// </summary>
    private void HandleRotation(Vector3 moveDirection)
    {
        if (modelTransform != null && moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    /// <summary>
    /// Applies gravity and handles vertical motion.
    /// </summary>
    private void ApplyGravity(ref Vector3 moveDirection)
    {
        bool isGrounded = controller.isGrounded ||
            Physics.Raycast(transform.position + Vector3.up * groundCheckOffset, Vector3.down, groundCheckDistance);

        verticalVelocity = isGrounded ? -2f : verticalVelocity + gravity * Time.deltaTime;
        moveDirection.y = verticalVelocity;
    }

    #endregion

    #region === Debug ===

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * groundCheckOffset, Vector3.down * groundCheckDistance);
    }

    #endregion
}
