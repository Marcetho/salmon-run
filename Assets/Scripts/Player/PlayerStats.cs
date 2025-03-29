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
    [SerializeField] private float outOfWaterDrainDelay = 3f; // Seconds before drain starts
    [SerializeField] private float outOfWaterDrainRate = 3f; // Energy drained per second

    [Header("Zero Energy Damage")]
    [SerializeField] private float zeroEnergyDamageInterval = 1.0f; // How often to apply damage when energy is zero
    [SerializeField] private float zeroEnergyDamageAmount = 1.0f; // Health damage per interval when energy is zero
    [SerializeField] private float aiFishDamageMultiplier = 0.3f; // AI fish take less damage (30% of player damage)

    [Header("Status")]
    [SerializeField] private bool isCurrentPlayer = false;
    private bool isInWater = true; // Default to true since fish start in water
    private float outOfWaterTime = 0f;
    private float lastZeroEnergyDamageTime = 0f;

    // Events
    public event Action<float> OnHealthChanged;
    public event Action<float> OnEnergyChanged;
    public event Action OnPlayerDeath;
    // New event for AI fish death
    public event Action<GameObject> OnAIFishDeath;

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

    public bool IsInWater => isInWater;

    private void FixedUpdate()
    {
        // Handle energy regeneration and out-of-water drain
        if (isInWater)
        {
            // Reset out of water timer when in water
            outOfWaterTime = 0f;

            // Only regenerate energy when in water
            if (currentEnergy < maxEnergy)
            {
                ModifyEnergy(energyRegenRate);
            }
        }
        else
        {
            // Track time out of water
            outOfWaterTime += Time.fixedDeltaTime;

            // After delay, start draining energy
            if (outOfWaterTime > outOfWaterDrainDelay && currentEnergy > 0)
            {
                float drainAmount = outOfWaterDrainRate * Time.fixedDeltaTime;
                ModifyEnergy(-drainAmount);
            }
        }

        // Apply health damage when energy is zero (both in and out of water)
        if (currentEnergy <= 0)
        {
            if (Time.time - lastZeroEnergyDamageTime >= zeroEnergyDamageInterval)
            {
                // Apply reduced damage for AI fish
                float damageAmount = isCurrentPlayer ?
                    zeroEnergyDamageAmount :
                    zeroEnergyDamageAmount * aiFishDamageMultiplier;

                ModifyHealth(-damageAmount);
                lastZeroEnergyDamageTime = Time.time;

                if (isCurrentPlayer && currentHealth <= 20f)
                {
                    // Visual/audio feedback that health is critical when player-controlled
                    // This could trigger a UI effect, screen vignette, etc.
                }
            }
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
                if (isCurrentPlayer)
                {
                    // Only invoke player death for the current player
                    Debug.Log("Player-controlled fish died");
                    OnPlayerDeath?.Invoke();
                }
                else
                {
                    // For AI fish, invoke AI death event
                    Debug.Log("AI-controlled fish died");
                    OnAIFishDeath?.Invoke(this.gameObject);
                }
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

    public void SetInWater(bool inWater)
    {
        isInWater = inWater;

        // Reset timer if entering water
        if (inWater)
        {
            outOfWaterTime = 0f;
        }
    }
}
