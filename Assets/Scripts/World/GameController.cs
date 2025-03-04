using UnityEngine;
using System.Collections.Generic;
using System;

public enum GameState { Ocean, Freshwater, Won, Lost }
public class GameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Game Settings")]
    [SerializeField] private int initialLivesCount = 3;
    private GameState currentState;
    private List<GameObject> spawnedFishes = new List<GameObject>();
    private int currentPlayerIndex = 0;
    private int remainingLives;

    private void Start()
    {
        currentState = GameState.Ocean;
        remainingLives = initialLivesCount;

        // Initialize game systems
        InitializeGame();
        Debug.Log("Game initialized");
    }

    private void InitializeGame()
    {
        // Safety check
        if (uiManager == null)
        {
            Debug.LogError("GameController: Cannot initialize game. uiManager is null!");
            return;
        }

        // Set initial UI values
        LivesUI livesUI = uiManager.GetComponentInChildren<LivesUI>();
        if (livesUI != null)
        {
            livesUI.SetMaxLives(initialLivesCount);
        }
        uiManager.SetLives(remainingLives);
        Debug.Log($"Lives set to {remainingLives}");

        // Create players
        try
        {
            for (int i = 0; i < remainingLives; i++)
            {
                SpawnFish();
            }

            if (spawnedFishes.Count > 0)
            {
                currentPlayerIndex = 0;
                PlayerStats stats = spawnedFishes[currentPlayerIndex].GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.IsCurrentPlayer = true;
                }
                CameraMovement camera = cameraMovement.GetComponent<CameraMovement>();
                camera.target = spawnedFishes[currentPlayerIndex].transform;
            }
            else
            {
                Debug.LogError("GameController: No fish were spawned!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error spawning fish: {e.Message}\n{e.StackTrace}");
        }
    }

    private void SpawnFish()
    {
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;
            }
        }

        GameObject player = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        spawnedFishes.Add(player);

        // Get player components
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = player.AddComponent<PlayerStats>();
        }

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        playerStats.OnPlayerDeath += OnCurrentPlayerDied;

        // Update UI with initial values
        uiManager.SetHealth(playerStats.CurrentHealth);
        uiManager.SetEnergy(playerStats.CurrentEnergy);

        // Assign UI reference to player if needed
        if (playerMovement != null)
        {
            // Set reference to UI manager via reflection or serialized field
            var uiField = playerMovement.GetType().GetField("uiManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (uiField != null)
            {
                uiField.SetValue(playerMovement, uiManager);
            }
        }

    }

    private void OnCurrentPlayerDied()
    {
        remainingLives--;
        uiManager.SetLives(remainingLives);

        if (remainingLives <= 0)
        {
            GameOver();
            return;
        }

        // Get current player and mark as inactive
        if (currentPlayerIndex >= 0 && currentPlayerIndex < spawnedFishes.Count)
        {
            GameObject currentPlayer = spawnedFishes[currentPlayerIndex];
            PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.IsCurrentPlayer = false;
                stats.OnPlayerDeath -= OnCurrentPlayerDied;
            }
        }
    }

    private void GameOver()
    {
        currentState = GameState.Lost;
        uiManager.OnGameOver();
        Debug.Log("Game Over - No lives remaining");
    }

    // Add methods to handle player state changes
    public void OnPlayerDamaged(float damage)
    {
        if (currentPlayerIndex >= 0 && currentPlayerIndex < spawnedFishes.Count)
        {
            GameObject currentPlayer = spawnedFishes[currentPlayerIndex];
            PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.ModifyHealth(-damage);
                uiManager.SetHealth(stats.CurrentHealth);
            }
        }
    }

    public void OnEnergyUsed(float amount)
    {
        if (currentPlayerIndex >= 0 && currentPlayerIndex < spawnedFishes.Count)
        {
            GameObject currentPlayer = spawnedFishes[currentPlayerIndex];
            PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.ModifyEnergy(-amount);
                uiManager.SetEnergy(stats.CurrentEnergy);
            }
        }
    }
}