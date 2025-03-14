using UnityEngine;
using System.Collections.Generic;
using System;

public enum GameState { Ocean, Freshwater, Won, Lost, FishSelection }
public class GameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private GameObject playerPrefabOcean;
    [SerializeField] private GameObject playerPrefabM;
    [SerializeField] private GameObject playerPrefabF;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Game Settings")]
    [SerializeField] private int initialLivesCount = 10;
    private GameState currentState;
    private List<GameObject> spawnedFishes = new List<GameObject>();
    private int currentPlayerIndex = 0;
    public static GameObject currentPlayer;
    private int remainingLives;

    [Header("Fish Selection")]
    [SerializeField] private float selectionTimeScale = 0.01f; // Slows time during selection
    [SerializeField] private TMPro.TextMeshProUGUI selectionModeText; // Optional text to show during selection
    private bool inSelectionMode = false;
    private int selectionIndex = 0;
    private int level = 1;
    private bool wasSpaceAlreadyPressed = false; // Track if space was already pressed when entering selection mode

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
        if (selectionModeText)
        {
            selectionModeText.gameObject.SetActive(false);
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

    private void Update()
    {
        if (inSelectionMode)
        {
            HandleFishSelection();
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

        GameObject playerPrefab = level != 1 ? UnityEngine.Random.value > 0.5f ? playerPrefabM : playerPrefabF : playerPrefabOcean;

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
            // Store references before modifying the list
            GameObject deadPlayer = currentPlayer;
            PlayerStats stats = null;

            if (deadPlayer != null)
            {
                stats = deadPlayer.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.IsCurrentPlayer = false;
                    stats.OnPlayerDeath -= OnCurrentPlayerDied;
                }
            }

            // Remove the dead fish from the list
            spawnedFishes.RemoveAt(currentPlayerIndex);

            // Set currentPlayer to null to prevent further interactions with it
            currentPlayer = null;

            // Now destroy the dead fish
            if (deadPlayer != null)
            {
                Destroy(deadPlayer);
            }

            // Start fish selection mode if there are fish available
            if (spawnedFishes.Count > 0)
            {
                // Adjust selection index to a valid value
                selectionIndex = Mathf.Min(currentPlayerIndex, spawnedFishes.Count - 1);
                EnterSelectionMode();
            }
            else
            {
                // No fish left, game over
                GameOver();
            }
        }
    }

    private void HandleFishSelection()
    {
        // Navigate between fish
        if (Input.GetKeyDown(KeyCode.A))
        {
            CycleSelectionFish(-1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            CycleSelectionFish(1);
        }

        // Check for space key release before allowing selection
        if (Input.GetKeyUp(KeyCode.Space))
        {
            wasSpaceAlreadyPressed = false;
        }

        // Confirm selection only if space wasn't already pressed when entering selection mode
        if (Input.GetKeyDown(KeyCode.Space) && !wasSpaceAlreadyPressed)
        {
            ConfirmFishSelection();
        }
    }

    private void CycleSelectionFish(int direction)
    {
        if (spawnedFishes.Count == 0) return;

        selectionIndex = (selectionIndex + direction + spawnedFishes.Count) % spawnedFishes.Count;

        // Move camera to the selected fish
        if (cameraMovement != null && spawnedFishes[selectionIndex] != null)
        {
            cameraMovement.target = spawnedFishes[selectionIndex].transform;

            // Update UI to show this fish's stats using the existing player HUD
            if (uiManager != null)
            {
                PlayerStats stats = spawnedFishes[selectionIndex].GetComponent<PlayerStats>();
                if (stats != null)
                {
                    uiManager.RefreshAllStats(stats.CurrentHealth, stats.CurrentEnergy, remainingLives);
                }
            }
        }
    }

    private void ConfirmFishSelection()
    {
        if (spawnedFishes.Count > 0 && selectionIndex >= 0 && selectionIndex < spawnedFishes.Count)
        {
            SetCurrentPlayer(selectionIndex);
            ExitSelectionMode();
        }
        else
        {
            Debug.LogWarning("Cannot confirm selection - invalid fish index");

            // Attempt to find any valid fish
            if (spawnedFishes.Count > 0)
            {
                selectionIndex = 0;
                SetCurrentPlayer(selectionIndex);
                ExitSelectionMode();
            }
            else
            {
                GameOver();
            }
        }
    }

    private void EnterSelectionMode()
    {
        Debug.Log("Entering fish selection mode");
        inSelectionMode = true;
        Time.timeScale = selectionTimeScale;
        currentState = GameState.FishSelection;

        // Check if space is already pressed when entering selection mode
        wasSpaceAlreadyPressed = Input.GetKey(KeyCode.Space);

        // Display selection mode text if available
        if (selectionModeText != null)
        {
            selectionModeText.gameObject.SetActive(true);
            selectionModeText.text = "CHOOSE NEXT FISH\nUse A/D to navigate\nPress Space to select";
        }

        // Make sure selectionIndex is valid
        if (selectionIndex < 0 || selectionIndex >= spawnedFishes.Count)
        {
            selectionIndex = 0;
        }

        // Make sure we have a valid fish
        if (spawnedFishes.Count > 0 && selectionIndex < spawnedFishes.Count && spawnedFishes[selectionIndex] != null)
        {
            Debug.Log($"Initial selection fish: {spawnedFishes[selectionIndex].name}");
            // Set camera to follow this fish
            if (cameraMovement != null)
            {
                cameraMovement.target = spawnedFishes[selectionIndex].transform;
            }

            // Immediately update the UI with this fish's stats
            if (uiManager != null)
            {
                PlayerStats stats = spawnedFishes[selectionIndex].GetComponent<PlayerStats>();
                Debug.Log($"Initial selection stats: {stats}");
                if (stats != null)
                {
                    // Replace the individual stat updates with a full refresh
                    uiManager.RefreshAllStats(stats.CurrentHealth, stats.CurrentEnergy, remainingLives);
                }
            }
        }
        else
        {
            Debug.LogWarning("No valid fish available for selection");
        }
    }

    private void ExitSelectionMode()
    {
        inSelectionMode = false;
        Time.timeScale = 1.0f;
        currentState = GameState.Ocean; // or whatever the previous state was

        // Hide selection mode text
        if (selectionModeText != null)
        {
            selectionModeText.gameObject.SetActive(false);
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