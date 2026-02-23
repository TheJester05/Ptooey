using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Tracking")]
    public Transform target;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0, 1.2f, -2.5f);
    public float sensitivity = 3.0f;

    [Header("Limits")]
    public float yMinLimit = -15f; // How far you can look down
    public float yMaxLimit = 60f;  // How far you can look up

    private float _currentX = 0f;
    private float _currentY = 0f;

    void Start()
    {
        // Optional: Starting rotation
        Vector3 angles = transform.eulerAngles;
        _currentX = angles.y;
        _currentY = angles.x;
    }

    // LateUpdate is CRITICAL to stop jitter when following an Interpolated target
    void LateUpdate()
    {
        if (!target) return;

        // 1. Vertical look stays local to the camera
        _currentY -= Input.GetAxis("Mouse Y") * sensitivity;
        _currentY = Mathf.Clamp(_currentY, yMinLimit, yMaxLimit);

        // 2. HORIZONTAL rotation comes directly from the target (the Rat)
        // This ensures the camera is ALWAYS perfectly in sync with the networked mesh
        float targetYRotation = target.eulerAngles.y;

        Quaternion rotation = Quaternion.Euler(_currentY, targetYRotation, 0);
        Vector3 position = target.position + rotation * offset;

        transform.rotation = rotation;
        transform.position = position;
    }
}