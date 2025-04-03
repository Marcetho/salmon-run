using UnityEngine;
using System.Collections.Generic;

public class SealSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public BoxCollider spawnVolume;

    [Header("Spawn Settings")]
    public GameObject sealPrefab; // Change to sealPrefab
    public float sealDensity = 1f; // Seals per cubic unit

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("SealSpawner requires a player reference!");
            enabled = false;
            return;
        }

        if (spawnVolume == null)
        {
            spawnVolume = GetComponent<BoxCollider>();
            if (spawnVolume == null)
            {
                Debug.LogError("SealSpawner requires a BoxCollider! Please add one to the GameObject or assign it in the inspector.");
                enabled = false;
                return;
            }
        }

        SpawnSeals();
    }

    private void SpawnSeals()
    {
        float volumeSize = GetVolumeSize();
        int sealCount = Mathf.CeilToInt(volumeSize * sealDensity);

        for (int i = 0; i < sealCount; i++)
        {
            Vector3 spawnPos = GetRandomPositionInVolume();
            Instantiate(sealPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"Spawned {sealCount} seals.");
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
