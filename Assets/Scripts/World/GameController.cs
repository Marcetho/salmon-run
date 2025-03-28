using UnityEngine;
using System.Collections.Generic;
using System;

public enum GameState { Ocean, Freshwater, Won, Lost, FishSelection, LevelTransition }
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
    private int currentLevel = 1;

    [Header("Level Transition")]
    [SerializeField] private float transitionDuration = 2.0f;
    [SerializeField] private bool autoFindTransitionPoints = true;
    private bool isTransitioning = false;
    private List<LevelTransition> levelTransitionPoints = new List<LevelTransition>();

    [Header("Fish Selection")]
    [SerializeField] private float selectionTimeScale = 0.01f; // Slows time during selection
    [SerializeField] private GameObject instructionBox; // Simple instruction box in top right
    [SerializeField] private TMPro.TextMeshProUGUI instructionText; // Text inside the instruction box
    private bool inSelectionMode = false;
    private int selectionIndex = 0;
    private int level = 1;
    private bool wasSpaceAlreadyPressed = false; // Track if space was already pressed when entering selection mode

    // Ghost fish properties
    [Header("Ghost Fish")]
    [SerializeField] private Color ghostColor = new Color(0.5f, 0.5f, 1.0f, 0.2f); // Blue-ish translucent color
    [SerializeField] private float ghostMoveSpeed = 2.0f; // Moderate speed for smooth transitions
    [SerializeField] private Material ghostMaterial; // Optional: base material for ghost (can be null)
    private GameObject ghostFish; // Reference to the ghost fish object

    // Camera adjustments during selection
    private Transform originalCameraTarget;
    private float originalCameraSmoothTime;
    private bool hasOriginalCameraSettings = false;

    private void Start()
    {
        currentState = GameState.Ocean;
        remainingLives = initialLivesCount;
        currentLevel = 1;

        // Find all level transition points in the scene if auto-find is enabled
        if (autoFindTransitionPoints)
        {
            FindAllLevelTransitionPoints();
        }

        // Initialize game systems
        InitializeGame();
        Debug.Log("Game initialized");
    }

    private void FindAllLevelTransitionPoints()
    {
        LevelTransition[] points = FindObjectsByType<LevelTransition>(FindObjectsSortMode.None);
        levelTransitionPoints.Clear();

        foreach (LevelTransition point in points)
        {
            levelTransitionPoints.Add(point);
        }

        Debug.Log($"Found {levelTransitionPoints.Count} level transition points in the scene.");
    }

    private void InitializeGame()
    {
        // Safety check
        if (uiManager == null)
        {
            Debug.LogWarning("GameController: Cannot initialize game. uiManager is null!");
            return;
        }
        if (instructionBox)
        {
            instructionBox.SetActive(false);
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
        player.tag = "Player"; // Set the Player tag for all spawned fish
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

                // Create the ghost fish before destroying the dead fish
                CreateGhostFish(deadPlayer);
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
                DestroyGhostFish(); // Clean up ghost if no fish left
                GameOver();
            }
        }
    }

    private void CreateGhostFish(GameObject deadFish)
    {
        DestroyGhostFish();

        ghostFish = Instantiate(deadFish, deadFish.transform.position, deadFish.transform.rotation);
        ghostFish.name = "GhostFish";

        if (cameraMovement != null)
        {
            ghostFish.transform.position = cameraMovement.transform.position +
                                          cameraMovement.transform.forward * 2f;
        }

        // Disable scripts and colliders
        var scripts = ghostFish.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != null && script.GetType() != typeof(Transform))
            {
                Destroy(script);
            }
        }

        Collider[] colliders = ghostFish.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        // Add the ghost behavior component
        GhostBehavior ghostBehavior = ghostFish.AddComponent<GhostBehavior>();
        ghostBehavior.useUnscaledTime = true;
        ghostBehavior.ghostColor = ghostColor; // Pass the ghost color to the behavior

        // Setup the target fish if available
        if (spawnedFishes.Count > 0 && selectionIndex >= 0 && selectionIndex < spawnedFishes.Count)
        {
            ghostBehavior.SetTargetFish(spawnedFishes[selectionIndex].transform);
        }
    }

    private void DestroyGhostFish()
    {
        if (ghostFish != null)
        {
            Destroy(ghostFish);
            ghostFish = null;
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

        // Move ghost toward the currently selected fish if it exists
        MoveGhostToSelectedFish();

        // Update ghost's target fish
        if (ghostFish != null && selectionIndex >= 0 && selectionIndex < spawnedFishes.Count)
        {
            GhostBehavior ghostBehavior = ghostFish.GetComponent<GhostBehavior>();
            if (ghostBehavior != null)
            {
                ghostBehavior.SetTargetFish(spawnedFishes[selectionIndex].transform);
            }
        }
    }

    private void MoveGhostToSelectedFish(bool immediate = false)
    {
        if (ghostFish != null && spawnedFishes.Count > 0 && selectionIndex >= 0 && selectionIndex < spawnedFishes.Count)
        {
            GameObject targetFish = spawnedFishes[selectionIndex];
            if (targetFish != null)
            {
                if (immediate)
                {
                    // Immediately position near the target fish 
                    ghostFish.transform.position = targetFish.transform.position;
                    ghostFish.transform.rotation = targetFish.transform.rotation;
                }
                else
                {
                    // Move ghost toward the selected fish with smooth lerp
                    float moveStep = Time.unscaledDeltaTime * ghostMoveSpeed;

                    // Position
                    ghostFish.transform.position = Vector3.Lerp(
                        ghostFish.transform.position,
                        targetFish.transform.position,
                        moveStep
                    );

                    // Rotation
                    ghostFish.transform.rotation = Quaternion.Slerp(
                        ghostFish.transform.rotation,
                        targetFish.transform.rotation,
                        moveStep
                    );
                }
            }
        }
    }

    private void ConfirmFishSelection()
    {
        if (spawnedFishes.Count > 0 && selectionIndex >= 0 && selectionIndex < spawnedFishes.Count)
        {
            // Set the camera to follow the selected fish before destroying the ghost
            if (cameraMovement != null && hasOriginalCameraSettings)
            {
                cameraMovement.target = spawnedFishes[selectionIndex].transform;
            }

            // Set current player
            SetCurrentPlayer(selectionIndex);

            // Destroy the ghost fish after selection is confirmed
            DestroyGhostFish();

            // Exit selection mode
            ExitSelectionMode();
        }
        else
        {
            Debug.LogWarning("Cannot confirm selection - invalid fish index");

            // Attempt to find any valid fish
            if (spawnedFishes.Count > 0)
            {
                selectionIndex = 0;

                // Set the camera to follow the selected fish
                if (cameraMovement != null && hasOriginalCameraSettings)
                {
                    cameraMovement.target = spawnedFishes[selectionIndex].transform;
                }

                SetCurrentPlayer(selectionIndex);

                // Destroy the ghost fish
                DestroyGhostFish();

                ExitSelectionMode();
            }
            else
            {
                GameOver();
            }
        }
    }

    private void CycleSelectionFish(int direction)
    {
        if (spawnedFishes.Count == 0) return;

        selectionIndex = (selectionIndex + direction + spawnedFishes.Count) % spawnedFishes.Count;

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

    private void EnterSelectionMode()
    {
        Debug.Log("Entering fish selection mode");
        inSelectionMode = true;
        Time.timeScale = selectionTimeScale;
        currentState = GameState.FishSelection;

        // Check if space is already pressed when entering selection mode
        wasSpaceAlreadyPressed = Input.GetKey(KeyCode.Space);

        // Display selection mode instructions
        ShowInstructionPanel("CHOOSE NEXT FISH", "Use A/D to navigate\nPress Space to select");

        // Make sure selectionIndex is valid
        if (selectionIndex < 0 || selectionIndex >= spawnedFishes.Count)
        {
            selectionIndex = 0;
        }

        // Store original camera target
        if (cameraMovement != null)
        {
            originalCameraTarget = cameraMovement.target;
            originalCameraSmoothTime = cameraMovement.smoothTime;
            hasOriginalCameraSettings = true;

            // Set camera to follow ghost fish
            if (ghostFish != null)
            {
                cameraMovement.target = ghostFish.transform;
                cameraMovement.smoothTime = 0.3f;
                cameraMovement.useUnscaledTime = true;
            }
        }

        // Make sure we have a valid fish
        if (spawnedFishes.Count > 0 && selectionIndex >= 0 && selectionIndex < spawnedFishes.Count)
        {
            // If we have a ghost fish, move it to the first selectable fish right away
            if (ghostFish != null)
            {
                // Start the ghost moving toward the first selectable fish
                MoveGhostToSelectedFish(true); // true = immediate positioning
            }

            // Update UI with this fish's stats
            if (uiManager != null)
            {
                PlayerStats stats = spawnedFishes[selectionIndex].GetComponent<PlayerStats>();
                if (stats != null)
                {
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

        // Hide instruction panel
        HideInstructionPanel();

        // Restore original camera settings
        if (cameraMovement != null && hasOriginalCameraSettings)
        {
            // Target should be the newly selected fish from ConfirmFishSelection
            cameraMovement.smoothTime = originalCameraSmoothTime;
            cameraMovement.useUnscaledTime = false;
        }
    }

    // Add method to handle AI fish death
    public void OnAIFishDied(GameObject deadFish)
    {
        if (deadFish == null) return;

        // Don't decrease lives for AI fish
        remainingLives--;
        uiManager.DecreaseLives();
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

    // New method to handle transitioning to the next level
    public void TransitionToNextLevel(int nextLevel)
    {
        if (isTransitioning) return;

        Debug.Log($"Transitioning from level {currentLevel} to level {nextLevel}");

        // Update level state
        int previousLevel = currentLevel;
        currentLevel = nextLevel;
        level = nextLevel;  // Make sure to update the level variable used for prefab selection

        // Find the start point for the next level
        Transform nextLevelStartPoint = FindLevelStartPoint(nextLevel);

        if (nextLevelStartPoint == null)
        {
            Debug.LogError($"No start point found for level {nextLevel}. Cannot transition!");
            return;
        }

        // Change game state
        GameState previousState = currentState;
        currentState = GameState.LevelTransition;
        isTransitioning = true;

        // If level is changing to 2, change to Freshwater state
        if (nextLevel == 2 && previousLevel == 1)
        {
            currentState = GameState.Freshwater;
        }

        // Update the UI or other game systems as needed based on level change
        if (uiManager != null)
        {
            uiManager.ShowLevelTransition(previousLevel, nextLevel);
        }

        // Relocate all fish to the new starting point
        StartCoroutine(RelocateAllFish(nextLevelStartPoint, transitionDuration));
    }

    private System.Collections.IEnumerator RelocateAllFish(Transform destination, float duration)
    {
        // Make fish inactive during transition
        foreach (GameObject fish in spawnedFishes)
        {
            if (fish != null)
            {
                // Disable player movement scripts
                PlayerMovement movement = fish.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.enabled = false;
                }

                // Reset physics state
                Rigidbody rb = fish.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        // Wait for a short delay
        yield return new WaitForSeconds(duration * 0.5f);

        // Replace all fish with appropriate prefabs for the new level
        List<GameObject> newFishList = new List<GameObject>();
        bool isCurrentPlayerReplaced = false;
        int newCurrentPlayerIndex = 0;

        for (int i = 0; i < spawnedFishes.Count; i++)
        {
            if (spawnedFishes[i] != null)
            {
                // Get information from the current fish
                Vector3 position = destination.position + UnityEngine.Random.insideUnitSphere * 3f;
                // Ensure fish are placed at correct water level
                position.y = destination.position.y;
                Quaternion rotation = destination.rotation;
                bool isCurrentPlayerFish = (i == currentPlayerIndex);
                PlayerStats oldStats = spawnedFishes[i].GetComponent<PlayerStats>();

                // Create new fish with appropriate prefab
                GameObject newFish = CreateNewFishForLevel(position, rotation, oldStats);
                newFishList.Add(newFish);

                // Track current player
                if (isCurrentPlayerFish)
                {
                    newCurrentPlayerIndex = newFishList.Count - 1;
                    isCurrentPlayerReplaced = true;
                }

                // Destroy the old fish
                Destroy(spawnedFishes[i]);
            }
        }

        // Update the fish list
        spawnedFishes = newFishList;

        // Set current player if it was replaced
        if (isCurrentPlayerReplaced && spawnedFishes.Count > 0)
        {
            currentPlayerIndex = newCurrentPlayerIndex;
            currentPlayer = spawnedFishes[currentPlayerIndex];

            // Update camera target
            if (cameraMovement != null)
            {
                cameraMovement.target = currentPlayer.transform;
                cameraMovement.ForceUpdatePosition();
            }
        }

        // Wait for a short delay to let everything settle
        yield return new WaitForSeconds(duration * 0.5f);

        // Set input blocking flag for a grace period
        StartCoroutine(BlockInputAfterTransition(1.0f));

        // Re-enable fish movement
        foreach (GameObject fish in spawnedFishes)
        {
            if (fish != null)
            {
                PlayerMovement movement = fish.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    // Reset physics state again before enabling movement
                    Rigidbody rb = fish.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    movement.enabled = true;
                    movement.ResetMovementState(); // Call new reset method
                }
            }
        }

        // Transition complete
        isTransitioning = false;
    }

    // Add a new method to block input for a short period after transition
    private System.Collections.IEnumerator BlockInputAfterTransition(float duration)
    {
        // Flag to block input - we'll add this to PlayerMovement
        foreach (GameObject fish in spawnedFishes)
        {
            if (fish != null)
            {
                PlayerMovement movement = fish.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.SetInputBlocked(true);
                }
            }
        }

        yield return new WaitForSeconds(duration);

        // Re-enable input
        foreach (GameObject fish in spawnedFishes)
        {
            if (fish != null)
            {
                PlayerMovement movement = fish.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.SetInputBlocked(false);
                }
            }
        }
    }

    // Helper method to create a new fish with the appropriate prefab for the current level
    private GameObject CreateNewFishForLevel(Vector3 position, Quaternion rotation, PlayerStats oldStats)
    {
        // Select the appropriate prefab based on level
        GameObject prefab;

        if (level == 1)
        {
            prefab = playerPrefabOcean;
            Debug.Log("Using ocean salmon prefab");
        }
        else
        {
            // For levels 2+, randomly choose male/female prefabs
            prefab = UnityEngine.Random.value > 0.5f ? playerPrefabM : playerPrefabF;
            Debug.Log($"Using {(prefab == playerPrefabM ? "male" : "female")} salmon prefab for level {level}");
        }

        // Create the new fish
        GameObject newFish = Instantiate(prefab, position, rotation);
        newFish.tag = "Player";

        // Transfer stats if available
        if (oldStats != null)
        {
            PlayerStats newStats = newFish.GetComponent<PlayerStats>();
            if (newStats == null)
            {
                newStats = newFish.AddComponent<PlayerStats>();
            }

            // Copy essential stats
            newStats.IsCurrentPlayer = oldStats.IsCurrentPlayer;
            newStats.SetHealth(oldStats.CurrentHealth);
            newStats.SetEnergy(oldStats.CurrentEnergy);
            newStats.OnPlayerDeath += OnCurrentPlayerDied;
        }

        // Set up player movement
        PlayerMovement playerMovement = newFish.GetComponent<PlayerMovement>();
        if (playerMovement != null && uiManager != null)
        {
            // Set reference to UI manager via reflection or serialized field
            var uiField = playerMovement.GetType().GetField("uiManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (uiField != null)
            {
                uiField.SetValue(playerMovement, uiManager);
            }
        }

        return newFish;
    }

    // Update environment names for the UI
    private string GetEnvironmentName(int level)
    {
        switch (level)
        {
            case 1:
                return "Ocean";
            case 2:
                return "Lower River";
            case 3:
                return "Upper River";
            case 4:
                return "Spawning Grounds";
            default:
                return $"Level {level}";
        }
    }

    private Transform FindLevelStartPoint(int level)
    {
        foreach (LevelTransition point in levelTransitionPoints)
        {
            if (point.LevelNumber == level && point.GetPointType == LevelTransition.PointType.Start)
            {
                return point.transform;
            }
        }

        Debug.LogWarning($"No start point found for level {level}");
        return null;
    }

    private void UpdatePlayerPrefabsForLevel(int level)
    {
        // Update the player prefabs based on the current level
        // This allows for different fish types in different environments
        if (level >= 2)
        {
            // For level 2+ (freshwater), use male/female salmon prefabs
            // This code assumes these prefabs are already set in the inspector
            Debug.Log($"Updated player prefabs for level {level} (Freshwater)");
        }
        else
        {
            // For level 1 (ocean), use ocean salmon prefab
            Debug.Log($"Updated player prefabs for level {level} (Ocean)");
        }
    }

    // Helper function to get the current level
    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public void SetInitialLives(int lives)
    {
        initialLivesCount = lives;
        remainingLives = lives;

        // Update UI if it's already initialized
        if (uiManager != null)
        {
            LivesUI livesUI = uiManager.GetComponentInChildren<LivesUI>();
            if (livesUI != null)
            {
                livesUI.SetMaxLives(initialLivesCount);
            }
            uiManager.SetLives(remainingLives);
        }
    }

    // New methods to handle the instruction panel
    public void ShowInstructionPanel(string title, string bodyText)
    {
        if (instructionBox != null && instructionText != null)
        {
            instructionBox.SetActive(true);
            instructionText.text = $"<b>{title}</b>\n{bodyText}";
        }
    }

    public void HideInstructionPanel()
    {
        if (instructionBox != null)
        {
            instructionBox.SetActive(false);
        }
    }
}

// Replace the GhostBehavior class at the bottom of the file
public class GhostBehavior : MonoBehaviour
{
    public bool useUnscaledTime = true;
    private Transform targetFish;
    public float initialAlpha = 0.8f;
    private float maxDistance = 1f;
    private float minDistance = 0.2f;
    public Color ghostColor = new Color(0.5f, 0.5f, 1.0f, 0.2f); // Ghostly blue color

    // Added debug field to visualize alpha values in inspector
    [SerializeField] private float currentAlpha;

    // Track material instances
    private List<Material> materialInstances = new List<Material>();
    private bool materialsSetup = false;

    private void Start()
    {
        SetupMaterialInstances();
    }

    private void SetupMaterialInstances()
    {
        // Create unique material instances to modify
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Create unique instances of the materials
            Material[] sharedMaterials = r.sharedMaterials;
            Material[] uniqueMaterials = new Material[sharedMaterials.Length];

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                // Create instance that we can modify
                uniqueMaterials[i] = new Material(sharedMaterials[i]);

                // Make sure rendering mode is set for transparency
                uniqueMaterials[i].SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
                uniqueMaterials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                uniqueMaterials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                uniqueMaterials[i].SetInt("_ZWrite", 0); // Turn off ZWrite for transparent objects
                uniqueMaterials[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");
                uniqueMaterials[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                uniqueMaterials[i].renderQueue = 3000; // Transparent render queue

                // Apply ghost color (keep alpha for later manipulation)
                if (uniqueMaterials[i].HasProperty("_BaseColor"))
                {
                    Color baseColor = new Color(ghostColor.r, ghostColor.g, ghostColor.b, 1f);
                    uniqueMaterials[i].SetColor("_BaseColor", baseColor);
                }
                else if (uniqueMaterials[i].HasProperty("_Color"))
                {
                    Color baseColor = new Color(ghostColor.r, ghostColor.g, ghostColor.b, 1f);
                    uniqueMaterials[i].SetColor("_Color", baseColor);
                }

                // Reduce emission to avoid overly bright ghost
                if (uniqueMaterials[i].HasProperty("_EmissionColor"))
                {
                    Color emissionColor = uniqueMaterials[i].GetColor("_EmissionColor") * 0.8f;
                    uniqueMaterials[i].SetColor("_EmissionColor", emissionColor);
                }

                materialInstances.Add(uniqueMaterials[i]);
            }

            // Assign unique materials
            r.materials = uniqueMaterials;
        }

        materialsSetup = true;
        Debug.Log($"Ghost material setup complete. Created {materialInstances.Count} material instances with ghost color.");
    }

    private void Update()
    {
        if (targetFish == null) return;
        if (!materialsSetup) SetupMaterialInstances();

        float distance = Vector3.Distance(transform.position, targetFish.position);

        // Calculate alpha with improved formula and wider range
        float newAlpha = initialAlpha;
        if (distance < maxDistance)
        {
            // Normalize distance between minDistance and maxDistance
            float normalizedDistance = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
            // Create a smoother fade out curve
            newAlpha = initialAlpha * normalizedDistance;
        }

        currentAlpha = newAlpha; // For debugging

        // Apply alpha to all materials
        foreach (Material mat in materialInstances)
        {
            if (mat != null)
            {
                // Apply alpha to all common color properties to ensure compatibility
                if (mat.HasProperty("_BaseColor"))
                {
                    Color color = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, newAlpha));
                }

                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.GetColor("_Color");
                    mat.SetColor("_Color", new Color(color.r, color.g, color.b, newAlpha));
                }

                // Adjust emission intensity with alpha too
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color emission = mat.GetColor("_EmissionColor");
                    mat.SetColor("_EmissionColor", emission * newAlpha);
                }

                // Explicitly set alpha value for shaders that use it separately
                if (mat.HasProperty("_Alpha"))
                {
                    mat.SetFloat("_Alpha", newAlpha);
                }
            }
        }
    }

    public void SetTargetFish(Transform target)
    {
        targetFish = target;

        // Force update material setup if target changes
        if (!materialsSetup)
        {
            SetupMaterialInstances();
        }
    }

    private void OnDestroy()
    {
        // Clean up material instances to prevent memory leaks
        foreach (Material mat in materialInstances)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
        materialInstances.Clear();
    }
}