using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreUI : UIComponent
{
    [SerializeField] private Button scoreButton;
    [SerializeField] private TextMeshProUGUI scoreText;
    
    private int score = 0;
    private bool isGameOver = false;

    public override void Initialize()
    {
        scoreButton.onClick.AddListener(OnScoreButtonClick);
        UpdateScore(0);
    }

    private void OnScoreButtonClick()
    {
        if (!isGameOver)
        {
            score += 10;
            UpdateScore(score);
        }
    }

    private void UpdateScore(int newScore)
    {
        scoreText.text = $"Score: {newScore}";
    }

    public void OnGameOver()
    {
        isGameOver = true;
    }
}
