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
        //set current tag to player
        currentPlayer.tag = "Player";
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

        // Hide selection mode text
        if (selectionModeText != null)
        {
            selectionModeText.gameObject.SetActive(false);
        }

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
                    Color emissionColor = uniqueMaterials[i].GetColor("_EmissionColor") * 0.5f;
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