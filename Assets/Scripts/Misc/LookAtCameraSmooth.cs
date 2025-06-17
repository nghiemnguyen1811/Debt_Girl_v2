using UnityEngine;

public class LookAtCameraSmooth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 5f;

    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Tính hướng từ đối tượng tới camera (chỉ trục Y)
        Vector3 direction = cam.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        // Tính rotation theo camera, nhưng ở world space
        Quaternion targetWorldRotation = Quaternion.LookRotation(direction);

        // Smooth xoay trong world space (không bị cha ảnh hưởng)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetWorldRotation, Time.deltaTime * rotationSpeed);
    }
}
