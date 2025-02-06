using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float maxForwardSpeed = 5f;
    public float maxBackwardSpeed = -0.5f; 
    public float acceleration = 2f;
    public float deceleration = 2f;
    public float rotationSpeed = 100f;
    public float tiltAmount = 15f;
    public float verticalTiltAmount = 10f;  // For up/down tilt

    private float currentSpeed;
    private float targetSpeed;
    private Vector3 velocity;
    private float baseYPosition;

    Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
        baseYPosition = transform.position.y;
    }

    void Update()
    {
        // Handle rotation and tilt input
        float rotation = 0f;
        if (Input.GetKey(KeyCode.A)) rotation = -1f;
        if (Input.GetKey(KeyCode.D)) rotation = 1f;
        
        float verticalInput = 0f;
        if (Input.GetKey(KeyCode.W)) verticalInput = -1f;
        if (Input.GetKey(KeyCode.S)) verticalInput = 1f;
        
        // Apply rotations
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);
        
        // Calculate and apply tilt
        float sideTilt = rotation * tiltAmount;
        float verticalTilt = verticalInput * verticalTiltAmount;
        Quaternion targetRotation = Quaternion.Euler(verticalTilt, transform.eulerAngles.y, sideTilt);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

        // Handle speed changes based on shift/ctrl
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.LeftShift)) moveInput = 1f;
        else if (Input.GetKey(KeyCode.LeftControl)) moveInput = -0.3f;

        targetSpeed = moveInput * maxForwardSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        if (currentSpeed < maxBackwardSpeed) currentSpeed = maxBackwardSpeed;

        // Apply movement in the direction the fish is facing
        Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;
        
        // Add wave motion relative to current position
        float waveMotion = Mathf.Sin(Time.time * 2f) * 0.002f;
        newPosition += transform.up * waveMotion;
        
        transform.position = newPosition;
    }
}