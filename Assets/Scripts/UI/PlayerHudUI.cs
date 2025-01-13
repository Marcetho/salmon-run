using UnityEngine;

[System.Serializable]
public class PlayerHudUI
{
    [SerializeField] private EnergyUI energyUI;
    [SerializeField] private HealthUI healthUI;

    public void Initialize()
    {
        energyUI.Initialize();
        healthUI.Initialize();
    }

    public void Cleanup()
    {
        energyUI.Cleanup();
        healthUI.Cleanup();
    }

    public void OnGameOver()
    {
        energyUI.OnGameOver();
    }
}
