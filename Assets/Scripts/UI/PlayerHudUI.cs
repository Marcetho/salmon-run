using UnityEngine;

/// <summary>
/// Class to manage UI components for the player HUD.
/// This class should only be accessed through UIManager.
/// </summary>
[System.Serializable]
public class PlayerHudUI
{
    [SerializeField] private EnergyUI energyUI;
    [SerializeField] private LivesUI livesUI;
    [SerializeField] private HealthUI healthUI;

    // Internal methods used by UIManager
    internal void Initialize()
    {
        energyUI.Initialize();
        livesUI.Initialize();
        healthUI.Initialize();
    }

    internal void Cleanup()
    {
        energyUI.Cleanup();
        livesUI.Cleanup();
        healthUI.Cleanup();
    }

    internal void OnGameOver()
    {
        energyUI.OnGameOver();
        healthUI.OnGameOver();
    }

    // Energy Methods
    internal float GetCurrentEnergy() => energyUI.CurrentEnergy;
    internal bool HasEnoughEnergy(float amount) => energyUI.CurrentEnergy >= amount;
    internal void IncreaseEnergy(float amount) => energyUI.IncreaseEnergy(amount);
    internal void DecreaseEnergy(float amount) => energyUI.DecreaseEnergy(amount);

    // Health Methods
    internal float GetCurrentHealth() => healthUI.CurrentHealth;
    internal float GetMaxHealth() => healthUI.MaxHealth;
    internal void IncreaseHealth(float amount) => healthUI.IncreaseHealth(amount);
    internal void DecreaseHealth(float amount) => healthUI.DecreaseHealth(amount);

    // Lives Methods
    internal int GetCurrentLives() => livesUI.CurrentLives;
    internal int GetMaxLives() => livesUI.MaxLives;
    internal void IncreaseLives(int amount) => livesUI.IncreaseLives(amount);
    internal void DecreaseLives() => livesUI.DecreaseLives();

    // Set Methods
    internal void SetLives(int value) => livesUI.SetLives(value);
    internal void SetHealth(float value) => healthUI.SetHealth(value);
    internal void SetEnergy(float value) => energyUI.SetEnergy(value);
}
