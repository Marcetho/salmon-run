/*
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

using UnityEngine;
using System.Collections;

*/


public class CameraMovement : MonoBehaviour
{

    //camera??
    public Transform target;
    public Vector3 offset = new Vector3(0,2,-10);
    public float smoothTime = 0.25f;
    Vector3 currentVelocity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate() {
    transform.position = Vector3.SmoothDamp(
        transform.position,
        target.position + offset,
        ref currentVelocity,
        smoothTime
    );
}

}

