using System.Collections.Generic;
using UnityEngine;

public class FishSchoolManager : MonoBehaviour
{
    [SerializeField] private float wallDetectionDistance = 5.0f;
    [SerializeField] private float wallAvoidanceStrength = 3.0f;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float sprintThreshold = 8.0f;

    private Vector3 lastPlayerPosition;
    private float playerSpeed;
    private bool playerIsSprinting = false;

    private List<GameObject> schoolFishes = new List<GameObject>();
    private static FishSchoolManager _instance;

    public static FishSchoolManager Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public void Start()
    {
        if (GameController.currentPlayer != null)
        {
            lastPlayerPosition = GameController.currentPlayer.transform.position;
        }
        else
        {
            lastPlayerPosition = Vector3.zero;
        }

        StartCoroutine(UpdatePlayerVelocity());
    }

    private System.Collections.IEnumerator UpdatePlayerVelocity()
    {
        float smoothedSpeed = 0f;
        float smoothFactor = 0.3f;

        while (true)
        {
            if (GameController.currentPlayer != null)
            {
                Vector3 playerPos = GameController.currentPlayer.transform.position;
                float instantSpeed = Vector3.Distance(playerPos, lastPlayerPosition) / 0.2f;
                lastPlayerPosition = playerPos;

                // Apply smoothing to the speed
                smoothedSpeed = Mathf.Lerp(smoothedSpeed, instantSpeed, smoothFactor);
                playerSpeed = smoothedSpeed;

                // Sprint detection - either based on speed or input keys
                playerIsSprinting = smoothedSpeed > sprintThreshold ||
                    (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Space));

                // When player makes significant speed/direction changes, trigger natural movement updates
                if (playerIsSprinting || instantSpeed > 6f || Time.frameCount % 150 == 0)
                {
                    TriggerFishMovementUpdates();
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    // Trigger natural movement updates on all fish
    private void TriggerFishMovementUpdates()
    {
        // Clean up list first
        CleanupDeadFish();

        // Update natural movement on all fish
        foreach (GameObject fish in schoolFishes)
        {
            if (fish != null)
            {
                PlayerMovement movement = fish.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    // Use reflection to call the private UpdateNaturalMovement method
                    System.Reflection.MethodInfo methodInfo =
                        typeof(PlayerMovement).GetMethod("UpdateNaturalMovement",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(movement, null);
                    }
                }
            }
        }
    }

    public void RegisterFish(GameObject fish)
    {
        if (fish != null && !schoolFishes.Contains(fish))
        {
            schoolFishes.Add(fish);
        }
    }

    public void UnregisterFish(GameObject fish)
    {
        if (fish != null && schoolFishes.Contains(fish))
        {
            schoolFishes.Remove(fish);
        }
    }

    public void CleanupDeadFish()
    {
        for (int i = schoolFishes.Count - 1; i >= 0; i--)
        {
            if (schoolFishes[i] == null)
            {
                schoolFishes.RemoveAt(i);
            }
        }
    }

    // Basic obstacle avoidance
    public Vector3 CalculateObstacleAvoidance(Vector3 position, Vector3 forward)
    {
        Vector3 avoidanceVector = Vector3.zero;
        int hitCount = 0;

        // Check forward direction
        RaycastHit hit;
        if (Physics.Raycast(position, forward, out hit, wallDetectionDistance, obstacleLayerMask))
        {
            float distanceFactor = 1.0f - (hit.distance / wallDetectionDistance);
            Vector3 avoidDirection = Vector3.Reflect(forward, hit.normal).normalized;
            avoidanceVector = avoidDirection * distanceFactor * wallAvoidanceStrength;
            hitCount++;
        }

        return hitCount > 0 ? avoidanceVector : Vector3.zero;
    }

    // Getters for player information
    public bool IsPlayerSprinting()
    {
        return playerIsSprinting;
    }

    public float GetPlayerSpeed()
    {
        return playerSpeed;
    }

    public Vector3 GetPlayerDirection()
    {
        if (GameController.currentPlayer != null)
        {
            return GameController.currentPlayer.transform.forward;
        }
        return Vector3.forward;
    }

    // Get schooling influence - currently just obstacle avoidance
    public Vector3 GetSchoolingInfluence(Vector3 position, bool isInWater, Vector3 forward)
    {
        return CalculateObstacleAvoidance(position, forward);
    }

    // Get player's facing direction with a small variation
    public Quaternion GetPlayerFacingRotation(float uniqueValue, float maxVariationDegrees = 15f)
    {
        if (GameController.currentPlayer != null)
        {
            // Get the player's current rotation
            Quaternion playerRotation = GameController.currentPlayer.transform.rotation;

            // Add slight variation for natural fish school appearance
            float timeOffset = Time.time * 0.5f;
            float yawVariation = Mathf.Sin(timeOffset + uniqueValue * 10f) * maxVariationDegrees;
            float pitchVariation = Mathf.Sin(timeOffset * 0.7f + uniqueValue * 5f) * (maxVariationDegrees * 0.5f);
            float rollVariation = Mathf.Sin(timeOffset * 0.3f + uniqueValue * 7f) * (maxVariationDegrees * 0.3f);

            return playerRotation * Quaternion.Euler(pitchVariation, yawVariation, rollVariation);
        }

        return Quaternion.identity;
    }
}
