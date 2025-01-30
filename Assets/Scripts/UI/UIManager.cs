using UnityEngine;

/// <summary>
/// Manages all UI components and provides the public interface for UI interactions.
/// This is the main entry point for all UI-related operations.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private PlayerHudUI playerHud;
    [SerializeField] private GameoverTextUI gameOverTextUI;

    #region Internal Methods

    private void Start()
    {
        playerHud.Initialize();
        gameOverTextUI.Initialize();
    }

    private void OnDestroy()
    {
        playerHud.Cleanup();
    }


    internal void OnGameOver()
    {
        gameOverTextUI.ShowGameOver();
        playerHud.OnGameOver();
    }

    #endregion

    #region Public API - Use these methods to interact with the UI

    /// <summary>
    /// Decreases the player's health by one unit. Triggers game over if health reaches zero.
    /// This is the primary method for damaging the player.
    /// </summary>
    public void DecreaseHealth()
    {
        playerHud.DecreaseHealth();
    }

    /// <summary>
    /// Decreases the player's energy by the specified amount.
    /// This is the primary method for consuming player energy.
    /// </summary>
    /// <param name="amount">The amount of energy to decrease</param>
    public void DecreaseEnergy(float amount)
    {
        playerHud.DecreaseEnergy(amount);
    }

    /// <summary>
    /// Gets the current energy level of the player (0-100).
    /// Use this to check if the player has enough energy for actions.
    /// </summary>
    /// <returns>Current energy value</returns>
    public float GetCurrentEnergy()
    {
        return playerHud.GetCurrentEnergy();
    }

    /// <summary>
    /// Gets the current health points of the player.
    /// Use this to check player's remaining health.
    /// </summary>
    /// <returns>Current number of health points</returns>
    public int GetCurrentHealth()
    {
        return playerHud.GetCurrentHealth();
    }

    /// <summary>
    /// Heals the player by the specified amount of health points.
    /// </summary>
    /// <param name="amount">Amount of health points to heal</param>
    public void IncreaseHealth(int amount)
    {
        playerHud.IncreaseHealth(amount);
    }

    /// <summary>
    /// Adds energy to the player's current energy.
    /// </summary>
    /// <param name="amount">Amount of energy to add</param>
    public void AddEnergy(float amount)
    {
        playerHud.AddEnergy(amount);
    }

    /// <summary>
    /// Gets the maximum possible health points for the player.
    /// </summary>
    /// <returns>Maximum health points</returns>
    public int GetMaxHealth()
    {
        return playerHud.GetMaxHealth();
    }

    #endregion
}