using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the player's energy UI, including energy meter and regeneration.
/// </summary>
public class EnergyUI : UIComponent
{
    [SerializeField] private Button energyButton;
    [SerializeField] private Image energyMeter;
    [SerializeField] private float energyRegenRate = 0.2f;    // Energy regenerated per fixed update

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
        energyButton.onClick.AddListener(OnEnergyButtonClick);
        UpdateEnergy(energy);
    }

    private void OnEnergyButtonClick()
    {
        Debug.Log(energy);
        if (!isGameOver && energy >= 10)
        {
            UpdateEnergy(energy - 10);
        }
    }

    /// <summary>
    /// Decreases the energy by the specified amount if possible.
    /// </summary>
    /// <param name="amount">Amount of energy to decrease</param>
    internal void DecreaseEnergy(float amount)
    {
        if (!isGameOver && energy >= amount)
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
        energy = newEnergy;
        if (energy < 0)
        {
            energy = 0;
        }
        else if (energy > 100)
        {
            energy = 100;
        }
        energyMeter.fillAmount = energy / 100f;
        OnEnergyChanged?.Invoke(energy);
    }

    internal void OnGameOver()
    {
        isGameOver = true;
    }

    public override void Cleanup()
    {
        energyButton.onClick.RemoveListener(OnEnergyButtonClick);
    }
    void FixedUpdate()
    {
        if (!isGameOver && energy < 100)
        {
            UpdateEnergy(energy + energyRegenRate);
        }
    }
}
