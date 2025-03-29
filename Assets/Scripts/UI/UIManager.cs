using UnityEngine;

/// <summary>
/// Manages all UI components and provides the public interface for UI interactions.
/// This is the main entry point for all UI-related operations.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private PlayerHudUI playerHud;
    [SerializeField] private GameOverController gameOverController;

    [Header("Level Transition UI")]
    [SerializeField] private GameObject levelTransitionPanel;
    [SerializeField] private TMPro.TextMeshProUGUI levelTransitionText;
    [SerializeField] private float transitionDisplayTime = 3f;

    private bool isInitialized = false;
    private float transitionTimer = 0f;
    private bool isShowingTransition = false;
    private int pendingLivesValue = -1;
    private float pendingHealthValue = -1f;
    private float pendingEnergyValue = -1f;

    #region Internal Methods

    private void Start()
    {
        if (playerHud == null)
        {
            Debug.LogError("UIManager: playerHud is null! Make sure it's assigned in the inspector.");
            return;
        }

        if (gameOverController == null)
        {
            Debug.LogWarning("UIManager: gameOverController is null! Game over screen won't be shown.");
        }

        // Initialize UI components
        playerHud.Initialize();
        if (gameOverController != null)
            gameOverController.Initialize();

        isInitialized = true;

        // Apply any pending values
        if (pendingLivesValue >= 0)
            playerHud.SetLives(pendingLivesValue);

        if (pendingHealthValue >= 0)
            playerHud.SetHealth(pendingHealthValue);

        if (pendingEnergyValue >= 0)
            playerHud.SetEnergy(pendingEnergyValue);
    }

    private void OnDestroy()
    {
        if (playerHud != null)
            playerHud.Cleanup();
    }

    private void Update()
    {
        // Handle level transition display timer
        if (isShowingTransition)
        {
            transitionTimer -= Time.unscaledDeltaTime;
            if (transitionTimer <= 0)
            {
                HideLevelTransition();
            }
        }
    }

    internal void OnGameOver()
    {
        if (gameOverController != null)
        {
            gameOverController.ShowGameOver();

            // Hide other UI elements that shouldn't appear during game over
            if (levelTransitionPanel != null)
                levelTransitionPanel.SetActive(false);
        }

        if (playerHud != null)
            playerHud.OnGameOver();
    }

    internal void OnVictory()
    {
        if (gameOverController != null)
        {
            gameOverController.ShowVictory();

            // Hide other UI elements that shouldn't appear during victory
            if (levelTransitionPanel != null)
                levelTransitionPanel.SetActive(false);
        }

        if (playerHud != null)
            playerHud.OnGameOver(); // Can reuse the same OnGameOver method for disabling HUD
    }

    // Add method to directly access main menu functionality
    public void ReturnToMainMenu()
    {
        if (gameOverController != null)
        {
            // Use reflection to call ReturnToMainMenu since it's private
            var method = gameOverController.GetType().GetMethod("ReturnToMainMenu",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
                method.Invoke(gameOverController, null);
        }
    }

    #endregion

    #region Public API - Use these methods to interact with the UI

    // Energy Methods
    public bool HasEnoughEnergy(float amount) => (playerHud != null) ? playerHud.GetCurrentEnergy() >= amount : false;
    public float GetCurrentEnergy() => (playerHud != null) ? playerHud.GetCurrentEnergy() : 0f;
    public void IncreaseEnergy(float amount)
    {
        if (playerHud != null)
            playerHud.IncreaseEnergy(amount);
    }

    public void DecreaseEnergy(float amount)
    {
        if (playerHud != null)
            playerHud.DecreaseEnergy(amount);
    }

    // Health Methods
    public float GetCurrentHealth() => (playerHud != null) ? playerHud.GetCurrentHealth() : 0f;
    public float GetMaxHealth() => (playerHud != null) ? playerHud.GetMaxHealth() : 100f;

    public void IncreaseHealth(float amount)
    {
        if (playerHud != null)
            playerHud.IncreaseHealth(amount);
    }

    public void DecreaseHealth(float amount)
    {
        if (playerHud != null)
            playerHud.DecreaseHealth(amount);
    }

    // Lives Methods
    public int GetCurrentLives() => (playerHud != null) ? playerHud.GetCurrentLives() : 0;
    public int GetMaxLives() => (playerHud != null) ? playerHud.GetMaxLives() : 3;

    public void IncreaseLives(int amount)
    {
        if (playerHud != null)
            playerHud.IncreaseLives(amount);
    }

    public void DecreaseLives()
    {
        if (playerHud != null)
            playerHud.DecreaseLives();
    }

    // Set Methods
    public void SetLives(int value)
    {
        if (!isInitialized)
        {
            pendingLivesValue = value;
            return;
        }

        if (playerHud != null)
            playerHud.SetLives(value);
    }

    public void SetHealth(float value)
    {
        if (!isInitialized)
        {
            pendingHealthValue = value;
            return;
        }

        if (playerHud != null)
            playerHud.SetHealth(value);
    }

    public void SetEnergy(float value)
    {
        if (!isInitialized)
        {
            pendingEnergyValue = value;
            return;
        }

        if (playerHud != null)
            playerHud.SetEnergy(value);
    }

    // Player Switching Methods
    public void SwitchPlayer(PlayerHudUI newPlayerHud)
    {
        if (playerHud != null)
            playerHud.Cleanup();

        playerHud = newPlayerHud;

        if (playerHud != null && isInitialized)
            playerHud.Initialize();
    }

    // New method to refresh all stats at once (useful when switching players)
    public void RefreshAllStats(float health, float energy, int lives)
    {
        if (playerHud != null)
        {
            playerHud.SetHealth(health);
            playerHud.SetEnergy(energy);
            playerHud.SetLives(lives);
        }
    }

    #endregion

    #region Level Transition

    /// <summary>
    /// Shows a level transition UI with information about moving from one level to another
    /// </summary>
    /// <param name="previousLevel">Level the player is coming from</param>
    /// <param name="nextLevel">Level the player is going to</param>
    public void ShowLevelTransition(int previousLevel, int nextLevel)
    {
        if (levelTransitionPanel == null)
        {
            Debug.LogWarning("UIManager: Level transition panel not assigned, can't show level transition.");
            return;
        }

        // Show the transition panel
        levelTransitionPanel.SetActive(true);
        isShowingTransition = true;
        transitionTimer = transitionDisplayTime;

        // Set the transition text if available
        if (levelTransitionText != null)
        {
            string environmentFrom = GetEnvironmentName(previousLevel);
            string environmentTo = GetEnvironmentName(nextLevel);

            levelTransitionText.text = $"LEVEL {previousLevel} COMPLETE\n" +
                                       $"Moving from {environmentFrom} to {environmentTo}\n" +
                                       $"Get ready for Level {nextLevel}!";
        }
    }

    private void HideLevelTransition()
    {
        if (levelTransitionPanel != null)
        {
            levelTransitionPanel.SetActive(false);
        }
        isShowingTransition = false;
    }

    private string GetEnvironmentName(int level)
    {
        switch (level)
        {
            default:
                return $"Level {level}";
        }
    }

    #endregion
}