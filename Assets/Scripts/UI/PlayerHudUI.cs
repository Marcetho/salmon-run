using UnityEngine;

/// <summary>
/// Internal class to manage UI components for the player HUD.
/// This class should only be accessed through UIManager.
/// </summary>
[System.Serializable]
internal class PlayerHudUI
{
    [SerializeField] private EnergyUI energyUI;
    [SerializeField] private HealthUI healthUI;

    // Internal methods used by UIManager
    internal void Initialize()
    {
        energyUI.Initialize();
        healthUI.Initialize();
    }

    internal void Cleanup()
    {
        energyUI.Cleanup();
        healthUI.Cleanup();
    }

    internal void OnGameOver()
    {
        energyUI.OnGameOver();
    }

    internal void DecreaseHealth()
    {
        healthUI.DecreaseHealth();
    }

    internal void DecreaseEnergy(float amount)
    {
        energyUI.DecreaseEnergy(amount);
    }

    internal float GetCurrentEnergy()
    {
        return energyUI.CurrentEnergy;
    }

    internal int GetCurrentHealth()
    {
        return healthUI.CurrentHealth;
    }

    internal void IncreaseHealth(int amount)
    {
        healthUI.IncreaseHealth(amount);
    }

    internal void AddEnergy(float amount)
    {
        energyUI.AddEnergy(amount);
    }

    internal int GetMaxHealth()
    {
        return healthUI.MaxHealth;
    }
}
