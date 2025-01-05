using System.Collections;
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