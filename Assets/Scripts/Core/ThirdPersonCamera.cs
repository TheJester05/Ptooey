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

        // 1. Get Mouse Input
        _currentX += Input.GetAxis("Mouse X") * sensitivity;
        _currentY -= Input.GetAxis("Mouse Y") * sensitivity;

        // 2. Clamp the vertical rotation so the camera doesn't flip upside down
        _currentY = Mathf.Clamp(_currentY, yMinLimit, yMaxLimit);

        // 3. Create rotation based on mouse movement
        Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);

        // 4. Calculate the position: 
        // We move 'back' from the target by the offset's Z, then 'up' by the Y
        Vector3 position = target.position + rotation * new Vector3(0, 0, offset.z) + new Vector3(0, offset.y, 0);

        // 5. Apply the final values
        transform.rotation = rotation;
        transform.position = position;
    }
}