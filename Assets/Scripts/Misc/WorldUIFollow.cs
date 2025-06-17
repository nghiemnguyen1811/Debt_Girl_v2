using UnityEngine;

public class WorldUIFollow : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target;

    [Header("Offset above the target (world units)")]
    public Vector3 offset = new Vector3(0, 2f, 0);

    [Header("Always face camera?")]
    public bool lookAtCamera = true;

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Move UI to follow target in world space
        transform.position = target.position + offset;

        // Optional: face the camera
        if (lookAtCamera && mainCam != null)
        {
            transform.LookAt(mainCam.transform);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0); // Optional: only rotate on Y axis
        }
    }
}
