using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.VisualScripting;

/// <summary>
/// Manages the player's energy UI, including energy meter and regeneration.
/// </summary>
public class EnergyUI : UIComponent
{
    [SerializeField] private Image energyMeter;
    [SerializeField] private float energyRegenRate = 0.2f;
    [SerializeField] private UIManager uiManager;

    private float energy = 100f;
    private bool isGameOver = false;

    /// <summary>
    /// Gets the current energy level. Internal access through UIManager.
    /// </summary>
    internal float CurrentEnergy => energy;

    /// <summary>
    /// Event triggered when energy value changes.
    /// </summary>
    internal event System.Action<float> OnEnergyChanged;

    public override void Initialize()
    {
        UpdateEnergy(energy);
    }


    /// <summary>
    /// Decreases the energy by the specified amount if possible.
    /// </summary>
    /// <param name="amount">Amount of energy to decrease</param>
    internal void DecreaseEnergy(float amount)
    {
        if (!isGameOver)
        {
            UpdateEnergy(energy - amount);
        }
    }

    /// <summary>
    /// Adds energy to the player's current energy.
    /// </summary>
    internal void IncreaseEnergy(float amount)
    {
        if (!isGameOver)
        {
            UpdateEnergy(energy + amount);
        }
    }

    internal void SetEnergy(float value)
    {
        if (!isGameOver)
        {
            UpdateEnergy(Mathf.Clamp(value, 0, 100));
        }
    }

    private void UpdateEnergy(float newEnergy)
    {
        float oldEnergy = energy;
        energy = Mathf.Clamp(newEnergy, 0, 100);
        energyMeter.fillAmount = energy / 100f;
        OnEnergyChanged?.Invoke(energy);
    }

    internal void OnGameOver()
    {
        isGameOver = true;
    }

    public override void Cleanup()
    {
    }

    void FixedUpdate()
    {
        if (!isGameOver && energy < 100)
        {
            UpdateEnergy(energy + energyRegenRate);
        }
    }
}
