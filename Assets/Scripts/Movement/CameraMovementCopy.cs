using UnityEngine;
using System.Collections;

public class CameraMovementCopy : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.1F;
    private Vector3 velocity = Vector3.zero;

    // Camera offset
    public float offset_x = 0.0f;
    public float offset_y = 10.0f;  // Higher y value for top-down view
    public float offset_z = 0.0f;

    // Independent rotation
    public float rotationSpeed = 100f;
    private float currentRotation = 0f;

    void Start()
    {
        // Initialize the camera rotation
        currentRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        // Position handling
        Vector3 targetPosition = target.position + new Vector3(offset_x, offset_y, offset_z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Rotation handling
        if (Input.GetKey(KeyCode.Q))
        {
            currentRotation -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            currentRotation += rotationSpeed * Time.deltaTime;
        }

        // Apply the rotation
        transform.rotation = Quaternion.Euler(90f, currentRotation, 0f);  // 90 degrees for top-down view
    }
}