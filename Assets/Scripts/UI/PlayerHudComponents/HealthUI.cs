using UnityEngine;
using UnityEngine.UI;

public class HealthUI : UIComponent
{
    [SerializeField] private Image healthBar;
    [SerializeField] private Button healthButton;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private UIManager uiManager;

    private float currentHealth;
    private bool isGameOver = false;

    internal float CurrentHealth => currentHealth;
    internal float MaxHealth => maxHealth;

    internal event System.Action<float> OnHealthChanged;

    public override void Initialize()
    {
        currentHealth = maxHealth;
        healthButton.onClick.AddListener(OnHealthButtonClick);
        UpdateHealthBar();
    }

    private void OnHealthButtonClick()
    {
        if (!isGameOver && currentHealth >= 10)
        {
            DecreaseHealth(10);
        }
    }

    internal void DecreaseHealth(float amount)
    {
        if (!isGameOver)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            UpdateHealthBar();
            OnHealthChanged?.Invoke(currentHealth);
            
            if (currentHealth <= 0)
            {
                uiManager.DecreaseLives();
            }
        }
    }

    internal void IncreaseHealth(float amount)
    {
        if (!isGameOver)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            UpdateHealthBar();
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

    internal void SetHealth(float value)
    {
        if (!isGameOver)
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            UpdateHealthBar();
            OnHealthChanged?.Invoke(currentHealth);
            
            if (currentHealth <= 0)
            {
                uiManager.DecreaseLives();
            }
        }
    }

    private void UpdateHealthBar()
    {
        healthBar.fillAmount = currentHealth / maxHealth;
    }

    internal void OnGameOver()
    {
        isGameOver = true;
    }

    public override void Cleanup()
    {
        healthButton.onClick.RemoveListener(OnHealthButtonClick);
    }
}
