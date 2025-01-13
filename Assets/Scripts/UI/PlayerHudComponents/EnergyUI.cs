using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnergyUI : UIComponent
{
    [SerializeField] private Button energyButton;
    [SerializeField] private Image energyMeter;
    [SerializeField] private float energyRegenRate = 0.2f;

    private float energy = 100f;
    private bool isGameOver = false;

    public override void Initialize()
    {
        energyButton.onClick.AddListener(OnEnergyButtonClick);
        UpdateEnergy(energy);
    }

    private void OnEnergyButtonClick()
    {
        if (!isGameOver && energy >= 10)
        {
            UpdateEnergy(energy - 10);
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
    }

    public void OnGameOver()
    {
        isGameOver = true;
    }

    public override void Cleanup()
    {
        energyButton.onClick.RemoveListener(OnEnergyButtonClick);
    }
    void FixedUpdate()
    {
        if (energy < 100)
        {
            UpdateEnergy(energy + energyRegenRate);
        }
    }
}
