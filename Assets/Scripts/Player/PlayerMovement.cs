using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private DynamicJoystick joystick;
    [SerializeField] private Transform modelTransform;

    private PlayerInputHandler inputHandler;
    private CharacterController controller;
    private PlayerInteractDetector interactDetector;
    private PlayerAnimation playerAnim;
    private Transform cameraTransform;

    [Header(" Settings ")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private float verticalVelocity;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        controller = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerAnim = GetComponent<PlayerAnimation>();
        interactDetector = GetComponent<PlayerInteractDetector>();
    }

    void Update()
    {
        // Không di chuyển nếu đang tương tác
        if (interactDetector != null && interactDetector.IsInteracting)
            return;

        Vector2 joystickInput = joystick.direction;
        Vector2 keyboardInput = inputHandler.GetMoveInput();

        Vector2 finalInput = joystickInput.sqrMagnitude > 0.01f ? joystickInput : keyboardInput;

        Vector3 move = Vector3.zero;
        float moveAmount = 0f;

        if (finalInput.sqrMagnitude > 0.01f && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            move = (right * finalInput.x + forward * finalInput.y).normalized;
            moveAmount = move.magnitude;

            // Xoay object con theo hướng di chuyển
            if (modelTransform != null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        // Animation
        if (playerAnim != null)
            playerAnim.UpdateMovementAnimation(moveAmount);

        bool isGrounded = controller.isGrounded || Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.15f);

        if (isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;
        controller.Move(move * speed * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        Vector3 rayDirection = Vector3.down * 0.15f;

        Gizmos.DrawRay(rayOrigin, rayDirection);
    }
}
