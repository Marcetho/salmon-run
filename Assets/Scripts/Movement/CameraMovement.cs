using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.1F;
    private Vector3 velocity = Vector3.zero;

    //cam movement
    public float offset_x = 0.0f;
    public float offset_y = 0.3f;
    public float offset_z = -1.5f;

    //look settings
    public float maxHorizontalLookAngle = 360f;
    public float maxVerticalLookAngle = 15f;
    public float lookSpeed = 1f;
    private Quaternion originalRotation;
    private Quaternion targetLookRotation;
    private Vector2 lookAngles = Vector2.zero;  // Store x,y look angles separately

    // Selection mode adjustments
    public bool useUnscaledTime = false;

    void Start()
    {
        originalRotation = transform.rotation;
        targetLookRotation = originalRotation;
    }

    void Update()
    {
        // Exit if no target
        if (target == null) return;

        // Calculate target position 
        Vector3 targetPosition = target.TransformPoint(new Vector3(offset_x, offset_y, offset_z));

        // Choose appropriate time delta based on settings
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Use smooth follow regardless of timeScale
        if (useUnscaledTime || Time.timeScale < 0.1f)
        {
            // For slow-motion scenes, use a custom lerp implementation
            float moveSpeed = deltaTime / smoothTime * 3f;
            moveSpeed = Mathf.Clamp01(moveSpeed); // Ensure value is between 0-1

            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed);
        }
        else
        {
            // Normal smoothing for regular gameplay
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        // Rotation handling - simplified to avoid abrupt changes
        if (Input.GetMouseButton(1))
        {
            lookAngles.x -= Input.GetAxis("Mouse Y") * lookSpeed;
            lookAngles.y += Input.GetAxis("Mouse X") * lookSpeed;

            // Clamp the angles
            lookAngles.x = Mathf.Clamp(lookAngles.x, -maxVerticalLookAngle, maxVerticalLookAngle);
            lookAngles.y = Mathf.Clamp(lookAngles.y, -maxHorizontalLookAngle, maxHorizontalLookAngle);

            // Create rotation offset from base target rotation
            Quaternion xRotation = Quaternion.Euler(lookAngles.x, 0, 0);
            Quaternion yRotation = Quaternion.Euler(0, lookAngles.y, 0);
            targetLookRotation = target.rotation * yRotation * xRotation;
        }
        else
        {
            // Smooth rotation lerp based on time settings
            lookAngles = Vector2.Lerp(lookAngles, Vector2.zero, deltaTime * 3f);
            targetLookRotation = target.rotation * Quaternion.Euler(lookAngles.x, lookAngles.y, 0);
        }

        // Apply rotation with appropriate timing
        float rotationSpeed = deltaTime * (useUnscaledTime ? 7f : 5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, rotationSpeed);
    }

    public void ForceUpdatePosition()
    {
        if (target != null)
        {
            // Immediately position camera without smoothing
            Vector3 targetPosition = target.TransformPoint(new Vector3(offset_x, offset_y, offset_z));
            transform.position = targetPosition;

            // Also update rotation
            transform.rotation = target.rotation;

            // Reset velocity to avoid unwanted drift
            velocity = Vector3.zero;
        }
    }
}