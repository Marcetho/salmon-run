using UnityEngine;
using System.Collections.Generic;

public class boats : MonoBehaviour
{
    [SerializeField] private GameObject boatPrefab;
    [SerializeField] private float boatSpeed = 5f;
    [SerializeField] private int maxBoats = 10;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private BoxCollider spawnVolume;
    [SerializeField] private FoodSpawner foodSpawner;
    [SerializeField] private string collisionTargetTag = "Player";
    private int pointsLostOnHit = 5;

    private List<GameObject> activeBoats = new List<GameObject>();
    private float nextSpawnTime;
    private bool spawnHorizontal = true;

    public FoodSpawner FoodSpawnerReference => foodSpawner;
    public int PointsLostOnHit => pointsLostOnHit;

    void Start()
    {
        if (spawnVolume == null)
        {
            spawnVolume = GetComponent<BoxCollider>();
        }
        if (foodSpawner == null)
        {
            foodSpawner = FindObjectOfType<FoodSpawner>();
        }
        nextSpawnTime = Time.time;
    }

    void Update()
    {
        // Spawn new boats
        if (Time.time >= nextSpawnTime && activeBoats.Count < maxBoats)
        {
            SpawnBoat();
            nextSpawnTime = Time.time + spawnInterval;
        }

        // Move and check boats
        for (int i = activeBoats.Count - 1; i >= 0; i--)
        {
            GameObject boat = activeBoats[i];
            if (boat != null)
            {
                boat.transform.Translate(Vector3.forward * boatSpeed * Time.deltaTime);

                // Check if boat is outside volume
                if (!spawnVolume.bounds.Contains(boat.transform.position))
                {
                    Destroy(boat);
                    activeBoats.RemoveAt(i);
                }
            }
        }
    }

    private void SpawnBoat()
    {
        Vector3 spawnPoint = GetPerpendicularEdgePosition();
        Vector3 targetPoint = GetOppositeEdgePosition(spawnPoint);

        GameObject boat = Instantiate(boatPrefab, spawnPoint, Quaternion.identity);
        boat.transform.LookAt(targetPoint);
        if (!boat.GetComponent<BoatCollision>())
        {
            boat.AddComponent<BoatCollision>();
        }
        activeBoats.Add(boat);
        spawnHorizontal = !spawnHorizontal;
    }

    private Vector3 GetPerpendicularEdgePosition()
    {
        Bounds bounds = spawnVolume.bounds;

        if (spawnHorizontal)
        {
            // Spawn from left or right edge
            return new Vector3(
                Random.value > 0.5f ? bounds.min.x : bounds.max.x,
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }
        else
        {
            // Spawn from front or back edge
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.value > 0.5f ? bounds.min.z : bounds.max.z
            );
        }
    }

    private Vector3 GetOppositeEdgePosition(Vector3 spawnPos)
    {
        Bounds bounds = spawnVolume.bounds;

        if (Mathf.Approximately(spawnPos.x, bounds.min.x))
            return new Vector3(bounds.max.x, spawnPos.y, spawnPos.z);
        if (Mathf.Approximately(spawnPos.x, bounds.max.x))
            return new Vector3(bounds.min.x, spawnPos.y, spawnPos.z);
        if (Mathf.Approximately(spawnPos.z, bounds.min.z))
            return new Vector3(spawnPos.x, spawnPos.y, bounds.max.z);

        return new Vector3(spawnPos.x, spawnPos.y, bounds.min.z);
    }
}
