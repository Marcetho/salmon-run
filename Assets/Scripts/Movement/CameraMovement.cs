/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float smoothness = 5.0f;
    public Transform targetObject;
    private Vector3 initalOffset;
    private Vector3 cameraPosition;

    void Start()
    {  
        initalOffset = transform.position - targetObject.position;
    }

    void Update()
    {
        cameraPosition = targetObject.position + initalOffset;
        transform.position = cameraPosition;

        transform.rotation = targetObject.rotation;
    }
}
*/
using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.2F;
    private Vector3 velocity = Vector3.zero;

    //cam movment
    public float offset_x = 0.0f;
    public float offset_y = 0.3f;
    public float offset_z = -1.5f;
    //cam rotation
    float timeCount = 0.0f;
    float speed = 0.008f;

    void Update()
    {
        // Define a target position above and behind the target transform
        Vector3 targetPosition = target.TransformPoint(new Vector3(offset_x, offset_y, offset_z));

        // Smoothly move the camera towards that target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // move the rotation as well

        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, timeCount * speed);
        timeCount = timeCount + Time.deltaTime;
    }
}