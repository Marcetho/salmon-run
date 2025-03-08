using System.Collections.Generic;
using UnityEngine;

public class FishSchoolManager : MonoBehaviour
{
    [SerializeField] private float cohesionWeight = 1.0f;
    [SerializeField] private float alignmentWeight = 0.8f;
    [SerializeField] private float separationWeight = 1.2f;
    [SerializeField] private float separationDistance = 3.0f;
    [SerializeField] private float neighborRadius = 10.0f;
    [SerializeField] private float verticalAlignmentReduction = 0.3f; // Reduces vertical component (0-1)
    [SerializeField] private float playerSpeedInfluence = 0.7f;       // How much player's speed affects school behavior
    [SerializeField] private float sprintFollowThreshold = 8f;       // Speed threshold to consider player sprinting - lowered to be more responsive
    [SerializeField] private float sprintDetectionSensitivity = 1.5f; // How quickly to detect sprinting
    [SerializeField] private float wallDetectionDistance = 5.0f;  // How far to check for walls
    [SerializeField] private float wallAvoidanceStrength = 3.0f;  // Strength of wall avoidance
    [SerializeField] private LayerMask obstacleLayerMask;         // Layers to treat as obstacles
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
        // Initialize last school center if we have fishes
        if (schoolFishes.Count > 0)
        {
            lastPlayerPosition = GameController.currentPlayer != null ?
                GameController.currentPlayer.transform.position : Vector3.zero;
        }

        StartCoroutine(UpdatePlayerVelocity());
    }

    private System.Collections.IEnumerator UpdatePlayerVelocity()
    {
        // For smoother detection of sprinting
        float smoothedSpeed = 0f;
        float smoothFactor = 0.3f;

        while (true)
        {
            if (GameController.currentPlayer != null)
            {
                Vector3 playerPos = GameController.currentPlayer.transform.position;
                float instantSpeed = Vector3.Distance(playerPos, lastPlayerPosition) / 0.2f;
                lastPlayerPosition = playerPos;

                // Apply smoothing to the speed for more stable sprint detection
                smoothedSpeed = Mathf.Lerp(smoothedSpeed, instantSpeed, smoothFactor);
                playerSpeed = smoothedSpeed;

                // More responsive sprint detection for better following
                playerIsSprinting = smoothedSpeed > sprintFollowThreshold;

                // Also detect sprinting from player input for more responsive behavior
                PlayerMovement playerMovement = GameController.currentPlayer.GetComponent<PlayerMovement>();
                if (playerMovement != null && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Space))
                {
                    // Override - player is deliberately sprinting
                    playerIsSprinting = true;
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    public void RegisterFish(GameObject fish)
    {
        if (!schoolFishes.Contains(fish))
        {
            schoolFishes.Add(fish);
        }
    }

    public void UnregisterFish(GameObject fish)
    {
        if (schoolFishes.Contains(fish))
        {
            schoolFishes.Remove(fish);
        }
    }

    // Calculate cohesion vector (move toward center of neighbors)
    public Vector3 CalculateCohesion(Vector3 position, float maxDistance)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (GameObject fish in schoolFishes)
        {
            if (fish == null) continue;

            float distance = Vector3.Distance(position, fish.transform.position);
            if (distance > 0 && distance < maxDistance)
            {
                // Weight player position more when sprinting to help fish keep up
                float fishWeight = 1.0f;
                if (playerIsSprinting)
                {
                    // Reduce cohesion with other fish when sprinting to allow more direct player following
                    fishWeight = 0.5f; // Reduced from 0.7f to further weaken fish-to-fish influence during sprint
                }
                center += fish.transform.position * fishWeight;
                count++;
            }
        }

        // Add player position to cohesion calculation, weighted more during sprinting
        if (GameController.currentPlayer != null)
        {
            float playerWeight = 2.0f; // Increased from 1.0f to generally favor player position
            if (playerIsSprinting)
            {
                playerWeight = 4.0f;  // Increased from 3.0f for even stronger pull toward player when sprinting
            }
            center += GameController.currentPlayer.transform.position * playerWeight;
            count += (int)playerWeight; // Count player position with its weight
        }

        if (count > 0)
        {
            center /= count;
            return (center - position).normalized * cohesionWeight;
        }

        return Vector3.zero;
    }

    // Calculate alignment vector (align with neighbors' direction)
    public Vector3 CalculateAlignment(Vector3 position, float maxDistance)
    {
        Vector3 averageDirection = Vector3.zero;
        int count = 0;

        foreach (GameObject fish in schoolFishes)
        {
            if (fish == null) continue;

            float distance = Vector3.Distance(position, fish.transform.position);
            if (distance > 0 && distance < maxDistance)
            {
                averageDirection += fish.transform.forward;
                count++;
            }
        }

        if (count > 0)
        {
            averageDirection /= count;
            return averageDirection.normalized * alignmentWeight;
        }

        return Vector3.zero;
    }

    // Calculate separation vector (avoid crowding neighbors)
    public Vector3 CalculateSeparation(Vector3 position, float maxDistance)
    {
        Vector3 separationVector = Vector3.zero;
        int count = 0;

        foreach (GameObject fish in schoolFishes)
        {
            if (fish == null) continue;

            float distance = Vector3.Distance(position, fish.transform.position);
            if (distance > 0 && distance < maxDistance)
            {
                Vector3 awayFromFish = position - fish.transform.position;
                separationVector += awayFromFish.normalized / Mathf.Max(0.1f, distance);
                count++;
            }
        }

        if (count > 0)
        {
            separationVector /= count;
            return separationVector.normalized * separationWeight;
        }

        return Vector3.zero;
    }

    // Calculate vertical alignment with reduced intensity
    public Vector3 CalculateVerticalAlignment(Vector3 position, float maxDistance)
    {
        if (GameController.currentPlayer == null) return Vector3.zero;

        // Try to maintain same Y level as player, but with reduced effect
        float targetY = GameController.currentPlayer.transform.position.y;
        float currentY = position.y;

        // Calculate vertical correction - increase effect when fish is below player
        float alignFactor = (currentY < targetY) ? verticalAlignmentReduction * 2f : verticalAlignmentReduction;
        float yDiff = (targetY - currentY) * alignFactor;

        // Stronger correction when fish is diving too deep
        if (currentY < targetY - 3f)
        {
            yDiff *= 2.0f;
        }

        Vector3 verticalCorrection = new Vector3(0, yDiff, 0);

        // Return vertical alignment
        return verticalCorrection;
    }

    // Calculate player direction influence with speed adaptation
    public Vector3 CalculatePlayerDirectionInfluence()
    {
        if (GameController.currentPlayer == null) return Vector3.zero;

        // Get player's forward direction
        Vector3 playerForward = GameController.currentPlayer.transform.forward;

        // Scale influence based on player speed
        float speedFactor = Mathf.Clamp01(playerSpeed / 8f);
        float influenceStrength = Mathf.Lerp(1.0f, 2.5f, speedFactor); // Increased base influence (0.5->1.0, 2.0->2.5)

        // Additional influence when sprinting
        if (playerIsSprinting)
        {
            influenceStrength *= 1.8f; // Increased from 1.5f
        }

        return playerForward * influenceStrength * playerSpeedInfluence;
    }

    // Calculate obstacle avoidance vector
    public Vector3 CalculateObstacleAvoidance(Vector3 position, Vector3 forward)
    {
        Vector3 avoidanceVector = Vector3.zero;
        int hitCount = 0;

        // Check for obstacles in 5 directions (forward, 45° left/right, 90° left/right)
        Vector3[] directions = new Vector3[5];
        directions[0] = forward;                               // Forward
        directions[1] = Quaternion.Euler(0, -45, 0) * forward; // 45° left
        directions[2] = Quaternion.Euler(0, 45, 0) * forward;  // 45° right
        directions[3] = Quaternion.Euler(0, -90, 0) * forward; // 90° left
        directions[4] = Quaternion.Euler(0, 90, 0) * forward;  // 90° right

        float[] weights = new float[5] { 1.0f, 0.7f, 0.7f, 0.5f, 0.5f }; // Weights for each direction

        for (int i = 0; i < directions.Length; i++)
        {
            RaycastHit hit;
            Vector3 rayDirection = directions[i];
            Debug.DrawRay(position, rayDirection * wallDetectionDistance, Color.yellow, 0.01f); // Visualization

            if (Physics.Raycast(position, rayDirection, out hit, wallDetectionDistance, obstacleLayerMask))
            {
                // Calculate avoidance force inversely proportional to distance
                float distanceFactor = 1.0f - (hit.distance / wallDetectionDistance);

                // Get direction away from wall (reflection of ray direction)
                Vector3 avoidDirection = Vector3.Reflect(rayDirection, hit.normal).normalized;

                // Add weighted avoidance force
                avoidanceVector += avoidDirection * distanceFactor * weights[i];
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            avoidanceVector /= hitCount;
            return avoidanceVector.normalized * wallAvoidanceStrength;
        }

        return Vector3.zero;
    }

    // Public getter for player sprint status
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

    // Get schooling influence for a fish at position
    public Vector3 GetSchoolingInfluence(Vector3 position)
    {
        Vector3 cohesion = CalculateCohesion(position, neighborRadius);
        Vector3 alignment = CalculateAlignment(position, neighborRadius);
        Vector3 separation = CalculateSeparation(position, separationDistance);

        return cohesion + alignment + separation;
    }

    // Get schooling influence for a fish at position
    public Vector3 GetSchoolingInfluence(Vector3 position, bool isInWater)
    {
        // Default forward direction if needed
        Vector3 forward = Vector3.forward;

        // Try to get the fish's actual forward direction
        if (GameController.currentPlayer != null)
        {
            forward = GameController.currentPlayer.transform.forward;
        }

        return GetSchoolingInfluence(position, isInWater, forward);
    }

    // Get schooling influence for a fish at position
    public Vector3 GetSchoolingInfluence(Vector3 position, bool isInWater, Vector3 forward)
    {
        Vector3 cohesion = CalculateCohesion(position, neighborRadius);
        Vector3 alignment = CalculateAlignment(position, neighborRadius);
        Vector3 separation = CalculateSeparation(position, separationDistance);
        Vector3 verticalAlignment = CalculateVerticalAlignment(position, neighborRadius);
        Vector3 playerInfluence = CalculatePlayerDirectionInfluence();
        Vector3 obstacleAvoidance = CalculateObstacleAvoidance(position, forward);

        // Apply different weights based on fish position
        if (GameController.currentPlayer != null)
        {
            Vector3 toPlayer = GameController.currentPlayer.transform.position - position;
            float distanceToPlayer = toPlayer.magnitude;

            // Calculate direct vector to player to help fish stay closer
            Vector3 directPlayerVector = toPlayer.normalized;

            // Check if fish is below player - increase vertical alignment if so
            if (position.y < GameController.currentPlayer.transform.position.y)
            {
                verticalAlignment *= 1.5f;
            }

            // When far from player, increase cohesion and direct attraction
            if (distanceToPlayer > 12f)
            {
                cohesion *= 1.8f;
                playerInfluence *= 1.8f;  // Increased from 1.6f
            }
            // When very far, add direct path to player to prevent lagging
            if (distanceToPlayer > 20f)
            {
                playerInfluence *= 2.0f;
                // Add direct vector to player with increasing weight based on distance
                float directFactor = Mathf.Clamp01((distanceToPlayer - 20f) / 15f);
                playerInfluence += directPlayerVector * (3.0f * directFactor);
            }

            // When player is sprinting, prioritize player direction influence
            if (playerIsSprinting)
            {
                playerInfluence *= 2.0f;  // Increased from 1.8f
                cohesion *= 1.5f;  // Increased from 1.4f
                // Reduce separation to allow closer schooling during sprint
                separation *= 0.5f;  // Reduced further from 0.6f
            }
        }

        // Apply different weights if fish is out of water
        if (!isInWater)
        {
            // Reduce influence of schooling behaviors when out of water
            cohesion *= 0.2f;
            alignment *= 0.2f;

            // Increase vertical alignment to get back to water
            verticalAlignment *= 2.0f;

            // Increase player influence when out of water to follow player better
            playerInfluence *= 1.5f;
        }

        // Prioritize obstacle avoidance when obstacles are detected
        if (obstacleAvoidance.magnitude > 0.1f)
        {
            // Reduce other influences when avoiding obstacles
            cohesion *= 0.5f;
            alignment *= 0.5f;
            playerInfluence *= 0.8f; // Keep some player influence

            // Double obstacle avoidance when very close to walls
            float wallRaycastHit = 0f;
            RaycastHit hit;
            if (Physics.Raycast(position, forward, out hit, wallDetectionDistance * 0.5f, obstacleLayerMask))
            {
                wallRaycastHit = 1.0f - (hit.distance / (wallDetectionDistance * 0.5f));
                obstacleAvoidance *= 1.0f + wallRaycastHit * 2.0f;
            }
        }

        // Combine all influences
        return cohesion + alignment + separation + verticalAlignment + playerInfluence + obstacleAvoidance;
    }
}
