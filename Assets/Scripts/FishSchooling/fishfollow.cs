using UnityEngine;
using System.Collections;

public class fishfollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.1F;
    private Vector3 velocity = Vector3.zero;

    //cam movement
    public float offset_x = 0.0f;
    public float offset_y = 0.3f;
    public float offset_z = -1.5f;

    //look settings
    private Quaternion targetLookRotation;

    void Start()
    {
        
    }

    void Update()
    {
        targetLookRotation = target.rotation;

        // Position handling
        Vector3 targetPosition = target.TransformPoint(new Vector3(offset_x, offset_y, offset_z));
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Smoothly interpolate rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.deltaTime *5f);
    }
}