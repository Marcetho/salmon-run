using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float energyRegenRate = 0.2f;

    [Header("Status")]
    [SerializeField] private bool isCurrentPlayer = false;

    // Events
    public event Action<float> OnHealthChanged;
    public event Action<float> OnEnergyChanged;
    public event Action OnPlayerDeath;

    // Properties
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxEnergy => maxEnergy;
    public float CurrentEnergy => currentEnergy;
    public bool IsCurrentPlayer
    {
        get => isCurrentPlayer;
        set => isCurrentPlayer = value;
    }

    private void FixedUpdate()
    {
        // Regenerate energy over time
        if (currentEnergy < maxEnergy)
        {
            ModifyEnergy(energyRegenRate);
        }
    }

    public void SetHealth(float value)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);

        if (oldHealth != currentHealth)
        {
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                OnPlayerDeath?.Invoke();
            }
        }
    }

    public void ModifyHealth(float amount)
    {
        SetHealth(currentHealth + amount);
    }

    public void SetEnergy(float value)
    {
        float oldEnergy = currentEnergy;
        currentEnergy = Mathf.Clamp(value, 0, maxEnergy);

        if (oldEnergy != currentEnergy)
        {
            OnEnergyChanged?.Invoke(currentEnergy);
        }
    }

    public void ModifyEnergy(float amount)
    {
        SetEnergy(currentEnergy + amount);
    }

    public bool TryUseEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            ModifyEnergy(-amount);
            return true;
        }
        return false;
    }

    public void ResetStats()
    {
        SetHealth(maxHealth);
        SetEnergy(maxEnergy);
    }
}
