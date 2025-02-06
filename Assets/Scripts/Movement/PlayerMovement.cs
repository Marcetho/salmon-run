using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed = 50f;
    public float acceleration = 5f;
    public float deceleration = 3f;
    public float rotationSpeed = 100f;
    public float tiltAmount = 15f;

    private float currentSpeed;
    private float targetSpeed;
    private Vector3 velocity;

    Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
    }

    void Update()
    {
        // Handle rotation
        float rotation = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) rotation = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) rotation = 1f;
        
        // Apply rotation and tilt
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);
        float tilt = rotation * tiltAmount;
        transform.rotation = Quaternion.Lerp(transform.rotation, 
            Quaternion.Euler(0, transform.eulerAngles.y, tilt), 
            Time.deltaTime * 5f);

        // Handle forward movement
        targetSpeed = Input.GetAxis("Vertical") * maxSpeed;
        
        // Smoothly adjust current speed
        if (targetSpeed != 0)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);

        // Apply forward movement
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // Add slight up/down wave motion
        float waveMotion = Mathf.Sin(Time.time * 2f) * 0.02f;
        transform.position += Vector3.up * waveMotion;
    }
}