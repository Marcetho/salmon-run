using UnityEngine;

[System.Serializable]
public class PlayerHudUI
{
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private HealthUI healthUI;

    public void Initialize()
    {
        scoreUI.Initialize();
        healthUI.Initialize();
    }

    public void Cleanup()
    {
        scoreUI.Cleanup();
        healthUI.Cleanup();
    }

    public void OnGameOver()
    {
        scoreUI.OnGameOver();
    }
}
