using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button scoreButton;
    [SerializeField] private Button healthButton;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image healthBar;

    private int score = 0;
    private float health = 100f;

    private void Start()
    {
        scoreButton.onClick.AddListener(OnScoreButtonClick);
        healthButton.onClick.AddListener(OnHealthButtonClick);
        UpdateScore(0);
        
        if (healthBar != null)
        {
            healthBar.type = Image.Type.Filled;
            healthBar.fillMethod = Image.FillMethod.Horizontal;
            UpdateHealth(health);
        }
        else
        {
            Debug.LogError("No health bar assigned");
        }
    }

    private void OnScoreButtonClick()
    {
        score += 10;
        UpdateScore(score);
    }
    private void OnHealthButtonClick()
    {
        health -= 10f;
        UpdateHealth(health);
    }

    private void UpdateScore(int newScore)
    {
        scoreText.text = $"Score: {newScore}";
    }

    private void UpdateHealth(float newHealth)
    {
        health = Mathf.Clamp(newHealth, 0f, 100f);
        if (healthBar != null)
        {
            healthBar.fillAmount = health / 100f;
        }
    }
}