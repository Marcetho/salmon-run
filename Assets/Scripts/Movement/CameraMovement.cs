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
    public float maxLookAngle = 15f;
    public float lookSpeed = 1f;
    private Quaternion originalRotation;
    private Quaternion targetLookRotation;
    private Vector2 lookAngles = Vector2.zero;  // Store x,y look angles separately

    void Start()
    {
        originalRotation = transform.rotation;
        targetLookRotation = originalRotation;
    }

    void Update()
    {
        // Position handling
        Vector3 targetPosition = target.TransformPoint(new Vector3(offset_x, offset_y, offset_z));
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Rotation handling
        if (Input.GetMouseButton(1))
        {
            lookAngles.x -= Input.GetAxis("Mouse Y") * lookSpeed;
            lookAngles.y += Input.GetAxis("Mouse X") * lookSpeed;

            // Clamp the angles
            lookAngles.x = Mathf.Clamp(lookAngles.x, -maxLookAngle, maxLookAngle);
            lookAngles.y = Mathf.Clamp(lookAngles.y, -maxLookAngle, maxLookAngle);

            // Create rotation offset from base target rotation
            Quaternion xRotation = Quaternion.Euler(lookAngles.x, 0, 0);
            Quaternion yRotation = Quaternion.Euler(0, lookAngles.y, 0);
            targetLookRotation = target.rotation * yRotation * xRotation;
        }
        else
        {
            lookAngles = Vector2.Lerp(lookAngles, Vector2.zero, Time.deltaTime * 5f);
            targetLookRotation = target.rotation * Quaternion.Euler(lookAngles.x, lookAngles.y, 0);
        }

        // Smoothly interpolate rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.deltaTime *5f);
    }
}