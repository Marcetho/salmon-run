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
    [SerializeField] private int initialLivesCount = 10;
    private GameState currentState;
    private List<GameObject> spawnedFishes = new List<GameObject>();
    private int currentPlayerIndex = 0;
    public static GameObject currentPlayer;
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
                SetCurrentPlayer(currentPlayerIndex);
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

    private void SetCurrentPlayer(int index)
    {
        if (index < 0 || index >= spawnedFishes.Count)
        {
            Debug.LogError($"GameController: Invalid player index {index}. Valid range is 0-{spawnedFishes.Count - 1}");
            return;
        }

        // First, deactivate the current player if there is one
        if (currentPlayer != null)
        {
            PlayerStats prevStats = currentPlayer.GetComponent<PlayerStats>();
            if (prevStats != null)
            {
                prevStats.IsCurrentPlayer = false;
            }
        }

        currentPlayerIndex = index;
        currentPlayer = spawnedFishes[currentPlayerIndex];
        PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.IsCurrentPlayer = true;

            // Ensure newly controlled fish have minimum energy
            float minSwitchEnergy = stats.MaxEnergy * 0.3f; // 30% of max energy
            if (stats.CurrentEnergy < minSwitchEnergy)
            {
                stats.SetEnergy(minSwitchEnergy);
            }

            // Update UI with the new fish's stats
            if (uiManager != null)
            {
                uiManager.SetHealth(stats.CurrentHealth);
                uiManager.SetEnergy(stats.CurrentEnergy);
                uiManager.SetLives(remainingLives);
            }
        }
        CameraMovement camera = cameraMovement.GetComponent<CameraMovement>();
        camera.target = currentPlayer.transform;
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
        uiManager.DecreaseLives();

        if (remainingLives <= 0)
        {
            GameOver();
            return;
        }

        // Get current player and mark as inactive
        if (currentPlayerIndex >= 0 && currentPlayerIndex < spawnedFishes.Count)
        {
            PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.IsCurrentPlayer = false;
                stats.OnPlayerDeath -= OnCurrentPlayerDied;
            }

            // Store reference before destroying
            GameObject deadPlayer = currentPlayer;

            // Find a new valid fish to control
            int newIndex = -1;
            for (int i = 0; i < spawnedFishes.Count; i++)
            {
                if (i != currentPlayerIndex && spawnedFishes[i] != null)
                {
                    newIndex = i;
                    break;
                }
            }

            // Remove the dead fish from the list
            spawnedFishes.RemoveAt(currentPlayerIndex);

            // Now destroy the dead fish
            Destroy(deadPlayer);

            // Set new current player if we found one
            if (newIndex != -1)
            {
                // Adjust index if needed after removal
                if (newIndex > currentPlayerIndex)
                {
                    newIndex--;
                }

                SetCurrentPlayer(newIndex);
            }
            else if (spawnedFishes.Count > 0)
            {
                // Fallback to first fish if available
                SetCurrentPlayer(0);
            }
            else
            {
                // No fish left, game over
                GameOver();
            }
        }
    }

    // Add method to handle AI fish death
    public void OnAIFishDied(GameObject deadFish)
    {
        if (deadFish == null) return;

        // Don't decrease lives for AI fish

        // Find and remove the fish from our list
        int fishIndex = spawnedFishes.IndexOf(deadFish);
        if (fishIndex >= 0)
        {
            // If this is somehow the current player index, we need to fix that
            if (fishIndex == currentPlayerIndex)
            {
                Debug.LogWarning("AI fish died but was marked as current player. This shouldn't happen.");

                // Find another fish to control
                int newIndex = -1;
                for (int i = 0; i < spawnedFishes.Count; i++)
                {
                    if (i != fishIndex && spawnedFishes[i] != null)
                    {
                        newIndex = i;
                        break;
                    }
                }

                spawnedFishes.RemoveAt(fishIndex);

                if (newIndex != -1)
                {
                    // Adjust index if needed after removal
                    if (newIndex > fishIndex)
                    {
                        newIndex--;
                    }

                    SetCurrentPlayer(newIndex);
                }
                else if (spawnedFishes.Count > 0)
                {
                    SetCurrentPlayer(0);
                }
                else
                {
                    // No fish left, game over
                    GameOver();
                }
            }
            else
            {
                // Just remove the fish from our list
                spawnedFishes.RemoveAt(fishIndex);

                // If this affects currentPlayerIndex, adjust it
                if (fishIndex < currentPlayerIndex)
                {
                    currentPlayerIndex--;
                }
            }
        }

        // Now destroy the fish
        Destroy(deadFish);
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
            PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.ModifyEnergy(-amount);
                uiManager.SetEnergy(stats.CurrentEnergy);
            }
        }
    }
}