using UnityEngine;

public class LookAtCameraSmooth : MonoBehaviour
{
    [Header(" Elements ")]
    private Transform cam;

    [Header(" Settings ")]
    [SerializeField] private float rotationSpeed = 5f;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 lookDirection = transform.position - cam.position;
        lookDirection.y = 0f; // Giữ cho UI không nghiêng lên/xuống nếu không cần

        if (lookDirection.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
