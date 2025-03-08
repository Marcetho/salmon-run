using System.Collections.Generic;
using UnityEngine;

public class FishSchoolManager : MonoBehaviour
{
    [SerializeField] private float wallDetectionDistance = 5.0f;
    [SerializeField] private float wallAvoidanceStrength = 3.0f;
    [SerializeField] private LayerMask obstacleLayerMask;
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
        if (schoolFishes.Count > 0)
        {
            lastPlayerPosition = GameController.currentPlayer != null ?
                GameController.currentPlayer.transform.position : Vector3.zero;
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

                // Simple sprint detection
                playerIsSprinting = smoothedSpeed > 8f ||
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

    // New method to trigger natural movement updates on all fish
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

    // Basic obstacle avoidance - simplify but keep for safety
    public Vector3 CalculateObstacleAvoidance(Vector3 position, Vector3 forward)
    {
        Vector3 avoidanceVector = Vector3.zero;
        int hitCount = 0;

        // Check only forward direction for simplicity
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

    // Simplified version for minimal obstacle avoidance only
    public Vector3 GetSchoolingInfluence(Vector3 position, bool isInWater, Vector3 forward)
    {
        return CalculateObstacleAvoidance(position, forward);
    }
}
