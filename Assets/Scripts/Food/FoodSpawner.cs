using UnityEngine;
using System.Collections.Generic;
using TMPro; // Add this for TextMeshPro support

public class FoodSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    [SerializeField] private TextMeshProUGUI scoreText; // Add this field

    [Header("Spawn Settings")]
    public GameObject foodPrefab;
    public BoxCollider spawnVolume;
    public float foodDensity = 1f; // Food items per cubic unit
    public float maxFoodCount = 50f;
    public float minFoodCount = 20f; // Minimum food count to maintain

    [Header("Timing")]
    public float minSpawnInterval = 0.1f;
    public float maxSpawnInterval = 2f;
    public float foodLifetime = 10f;

    private float nextSpawnTime;
    private List<FoodItem> activeFood = new List<FoodItem>();
    private int totalFoodCollected = 0;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("FoodSpawner requires a player reference!");
            enabled = false;
            return;
        }

        if (spawnVolume == null)
        {
            spawnVolume = GetComponent<BoxCollider>();
            if (spawnVolume == null)
            {
                Debug.LogError("FoodSpawner requires a BoxCollider! Please add one to the GameObject or assign it in the inspector.");
                enabled = false;
                return;
            }
        }

        SetNextSpawnTime();
    }

    private void Update()
    {
        // Clean up expired food first
        activeFood.RemoveAll(food => food == null);

        // Adjust spawn timing based on current food count
        if (activeFood.Count < minFoodCount)
        {
            // Spawn more quickly when below minimum
            if (Time.time >= nextSpawnTime)
            {
                SpawnFood();
                SetNextSpawnTime(0.1f, 0.3f); // Faster spawning
            }
        }
        else if (activeFood.Count < maxFoodCount)
        {
            // Normal spawn rate
            if (Time.time >= nextSpawnTime)
            {
                SpawnFood();
                SetNextSpawnTime(minSpawnInterval, maxSpawnInterval);
            }
        }
    }

    private void SpawnFood()
    {
        Vector3 spawnPos = GetRandomPositionInVolume();
        GameObject food = Instantiate(foodPrefab, spawnPos, Random.rotation);

        FoodItem foodItem = food.AddComponent<FoodItem>();
        foodItem.Initialize(foodLifetime);
        activeFood.Add(foodItem);
    }

    private Vector3 GetRandomPositionInVolume()
    {
        Vector3 extents = spawnVolume.size / 2f;
        Vector3 randomPoint = new Vector3(
            Random.Range(-extents.x, extents.x),
            0f, // Y will be set to player height
            Random.Range(-extents.z, extents.z)
        );

        // Transform point to world space, but use player's Y position
        Vector3 worldPoint = spawnVolume.transform.TransformPoint(randomPoint);
        worldPoint.y = player != null ? player.position.y : transform.position.y;
        return worldPoint;
    }

    private float GetVolumeSize()
    {
        Vector3 size = spawnVolume.size;
        return size.x * size.y * size.z;
    }

    private void SetNextSpawnTime(float minInterval, float maxInterval)
    {
        nextSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    private void SetNextSpawnTime()
    {
        SetNextSpawnTime(minSpawnInterval, maxSpawnInterval);
    }

    public int GetTotalFoodCollected()
    {
        return totalFoodCollected;
    }

    public void IncrementFoodCollected()
    {
        totalFoodCollected++;
        UpdateScoreDisplay();
    }

    public void DecrementFoodCollected()
    {
        totalFoodCollected = Mathf.Max(0, totalFoodCollected - 1);
        UpdateScoreDisplay();
        Debug.Log($"Lost food! Total points: {totalFoodCollected}");
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalFoodCollected}";
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnVolume != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = spawnVolume.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, spawnVolume.size);
            Gizmos.matrix = originalMatrix;
        }
    }
}

// Helper component for individual food items
public class FoodItem : MonoBehaviour
{
    private float destroyTime;
    private FoodSpawner spawner;

    public void Initialize(float lifetime)
    {
        spawner = FindObjectOfType<FoodSpawner>();
        destroyTime = Time.time + lifetime;
        StartCoroutine(DestroyAfterDelay());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (spawner != null)
            {
                spawner.IncrementFoodCollected();
                Debug.Log($"Food collected! Total points: {spawner.GetTotalFoodCollected()}");
            }
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyTime - Time.time);
        Destroy(gameObject);
    }
}
