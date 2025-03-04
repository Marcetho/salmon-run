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

    // Energy Methods
    public bool HasEnoughEnergy(float amount) => playerHud.GetCurrentEnergy() >= amount;
    public float GetCurrentEnergy() => playerHud.GetCurrentEnergy();
    public void IncreaseEnergy(float amount) => playerHud.IncreaseEnergy(amount);
    public void DecreaseEnergy(float amount) => playerHud.DecreaseEnergy(amount);

    // Health Methods
    public float GetCurrentHealth() => playerHud.GetCurrentHealth();
    public float GetMaxHealth() => playerHud.GetMaxHealth();
    public void IncreaseHealth(float amount) => playerHud.IncreaseHealth(amount);
    public void DecreaseHealth(float amount) => playerHud.DecreaseHealth(amount);

    // Lives Methods
    public int GetCurrentLives() => playerHud.GetCurrentLives();
    public int GetMaxLives() => playerHud.GetMaxLives();
    public void IncreaseLives(int amount) => playerHud.IncreaseLives(amount);
    public void DecreaseLives() => playerHud.DecreaseLives();

    // Set Methods
    public void SetLives(int value) => playerHud.SetLives(value);
    public void SetHealth(float value) => playerHud.SetHealth(value);
    public void SetEnergy(float value) => playerHud.SetEnergy(value);

    #endregion
}