using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverController : UIComponent
{
    [SerializeField] private TextMeshProUGUI gameoverText;

    [Header("Game Over UI Elements")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSound;

    [Header("Victory")]
    [SerializeField] private AudioClip victorySound;

    private UIManager uiManager;

    public override void Initialize()
    {
        uiManager = GetComponentInParent<UIManager>();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        else if (gameoverText != null)
            gameoverText.gameObject.SetActive(false);

        // Set up button listener
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    public void ShowGameOver()
    {
        // Show the game over panel or text
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else if (gameoverText != null)
            gameoverText.gameObject.SetActive(true);

        // Update score if available
        if (scoreText != null)
        {
            int currentLevel = FindAnyObjectByType<GameController>()?.GetCurrentLevel() ?? 1;
            int totalScore = GameController.GetTotalScore();
            scoreText.text = $"You reached level {currentLevel}\n\nTotal Score: {totalScore}";
        }


        // Pause game (will be handled by GameController, but as a backup)
        Time.timeScale = 0.1f;
    }

    public void ShowVictory()
    {
        // Show the game over panel or text
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else if (gameoverText != null)
            gameoverText.gameObject.SetActive(true);

        // Update score if available
        if (scoreText != null)
        {
            int totalScore = GameController.GetTotalScore();
            scoreText.text = $"VICTORY!\n\nYou completed the salmon run!\n\nTotal Score: {totalScore}";
        }


        // Slow time but don't stop completely
        Time.timeScale = 0.1f;
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1.0f; // Restore normal time
        SceneManager.LoadScene(0); // Assuming main menu is scene 0
    }
}
