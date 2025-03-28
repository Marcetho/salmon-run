using UnityEngine;
using System.Collections.Generic;

public class FishBoidBehavior : MonoBehaviour
{
    public Transform targetFish; // Main fish to follow
    public float speed = 3f;
    public float rotationSpeed = 5f;
    public float followDistance = 2f;
    public float cohesionWeight = 1f;
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;
    public float neighborRadius = 5f;
    [SerializeField] private Animator fishAnimator;

    [Header("Speed Settings")]
    public float maxSpeed = 5f;
    public float minSpeed = 0.5f;
    public float speedTransitionDistance = 4f; // Distance at which speed starts decreasing
    public float minSpeedDistance = 1f; // Distance at which minimum speed takes effect
    public float accelerationTime = 1.5f;  // Time to reach max speed
    public float decelerationTime = 1.0f;  // Time to reach min speed
    private float currentSpeedMultiplier = 0f;

    private List<Transform> nearbyFish = new List<Transform>(); // List of nearby fish

    private Vector3 lastTargetPosition;
    private bool isTargetMoving;
    public float movementThreshold = 0.01f;

    void Start()
    {
        fishAnimator = GetComponent<Animator>();
        fishAnimator.SetBool("InWater", true);
        lastTargetPosition = targetFish.position;
    }

    void Update()
    {
        // Update the nearby fish list
        UpdateNearbyFish();

        // Get the direction to the main fish
        Vector3 directionToTarget = targetFish.position - transform.position;

        // Cohesion: Move towards the center of nearby fish
        Vector3 cohesion = CalculateCohesion();

        // Separation: Avoid crowding nearby fish
        Vector3 separation = CalculateSeparation();

        // Alignment: Match the direction of nearby fish
        Vector3 alignment = CalculateAlignment();

        // Combine the behaviors
        Vector3 steeringForce = directionToTarget + cohesion * cohesionWeight + separation * separationWeight + alignment * alignmentWeight;

        // Apply the steering force to move the fish
        ApplyMovement(steeringForce);
    }

    void UpdateNearbyFish()
    {
        nearbyFish.Clear();
        foreach (var fish in FindObjectsByType<FishBoidBehavior>(FindObjectsSortMode.None))
        {
            if (fish != this && Vector3.Distance(transform.position, fish.transform.position) <= neighborRadius)
            {
                nearbyFish.Add(fish.transform);
            }
        }
    }

    Vector3 CalculateCohesion()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        foreach (Transform fish in nearbyFish)
        {
            centerOfMass += fish.position;
        }

        centerOfMass /= nearbyFish.Count;
        return (centerOfMass - transform.position).normalized;
    }

    Vector3 CalculateSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        foreach (Transform fish in nearbyFish)
        {
            if (Vector3.Distance(transform.position, fish.position) < followDistance)
            {
                separationForce += (transform.position - fish.position).normalized / Vector3.Distance(transform.position, fish.position);
            }
        }
        return separationForce;
    }

    Vector3 CalculateAlignment()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;
        foreach (Transform fish in nearbyFish)
        {
            averageVelocity += fish.GetComponent<Rigidbody>().linearVelocity;
        }

        averageVelocity /= nearbyFish.Count;
        return averageVelocity.normalized;
    }

    void ApplyMovement(Vector3 steeringForce)
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetFish.position);

        // Check if target is moving
        isTargetMoving = Vector3.Distance(targetFish.position, lastTargetPosition) > movementThreshold;
        lastTargetPosition = targetFish.position;

        // Calculate target speed multiplier
        float targetMultiplier = isTargetMoving ? 1f : 0.1f;

        // Smooth speed transitions
        float transitionTime = targetMultiplier > currentSpeedMultiplier ? accelerationTime : decelerationTime;
        currentSpeedMultiplier = Mathf.Lerp(currentSpeedMultiplier, targetMultiplier, Time.deltaTime / transitionTime);

        // Calculate base speed from distance
        float baseSpeed = CalculateSpeedBasedOnDistance(distanceToTarget);
        float currentSpeed = baseSpeed * currentSpeedMultiplier;

        // Only rotate if target is moving or we're too far away
        if (isTargetMoving || distanceToTarget > minSpeedDistance * 1.5f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(steeringForce);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // When stationary, match target's rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetFish.rotation, rotationSpeed * Time.deltaTime);
        }

        // Apply movement with smoothed speed
        if (isTargetMoving || distanceToTarget > minSpeedDistance)
        {
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
        if (fishAnimator != null)
        {
            fishAnimator.SetFloat("Speed", currentSpeed);
        }
    }

    float CalculateSpeedBasedOnDistance(float distance)
    {
        if (distance <= minSpeedDistance)
            return minSpeed;

        if (distance >= speedTransitionDistance)
            return maxSpeed;

        // Lerp between min and max speed based on distance
        float t = (distance - minSpeedDistance) / (speedTransitionDistance - minSpeedDistance);
        return Mathf.Lerp(minSpeed, maxSpeed, t);
    }
}