using UnityEngine;
using UnityEngine.SceneManagement;

public class TimedSceneController : MonoBehaviour
{
    public float sceneDuration = 30f; // Duration in seconds
    private float timeRemaining;
    private bool isCompleted = false;
    private bool hasShownIntro = false;

    [SerializeField] private UIManager uiManager;
    [SerializeField] private string oceanIntroText = "Welcome to the ocean phase! Survive and navigate to the river mouth.";
    [SerializeField] private string oceanCompletionText = "You've survived the ocean and found the river mouth. Your journey inland begins!";

    // This variable will be incremented by player actions
    public static int playerScore = -1;

    void Start()
    {
        // Initialize the timer
        timeRemaining = sceneDuration;
        playerScore = 0;

        // Find UIManager if not assigned
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        // Pause the game initially to show intro
        if (uiManager != null)
        {
            ShowOceanIntro();
        }
        else
        {
            Debug.LogWarning("UIManager not found in the ocean scene. Please add UIManager to enable transition popups.");
        }
    }

    void Update()
    {
        // Don't count down if not started or already completed
        if (!hasShownIntro || isCompleted)
            return;

        // Count down the timer
        timeRemaining -= Time.deltaTime;

        // If time is up, transition to river scene
        if (timeRemaining <= 0)
        {
            isCompleted = true;
            LoadNextScene();
        }
    }

    // This method can be called when player does something
    public void IncrementScore(int amount = 1)
    {
        if (playerScore == -1)
        {
            playerScore = 0;
        }
        playerScore = Mathf.Max(0, playerScore + amount);
        Debug.Log("Score increased to: " + playerScore);
    }

    // Show the ocean intro popup
    private void ShowOceanIntro()
    {
        // Pause game until player clicks continue
        Time.timeScale = 0f;

        if (uiManager != null)
        {
            // Use level 0 to indicate ocean phase
            uiManager.ShowPreLevelPopup(0, GameText.OceanIntroText);

            // Listen for continue button
            uiManager.OnContinueClicked += OnIntroComplete;
        }
        else
        {
            // If no UI manager, just start immediately
            OnIntroComplete();
        }
    }

    private void OnIntroComplete()
    {
        // Unsubscribe from event
        if (uiManager != null)
        {
            uiManager.OnContinueClicked -= OnIntroComplete;
        }

        // Resume game
        Time.timeScale = 1.0f;
        hasShownIntro = true;
    }

    void LoadNextScene()
    {
        // Count surviving fish to set the score
        if (playerScore <= 0)
        {
            GameObject[] survivingFish = GameObject.FindGameObjectsWithTag("Player");
            playerScore = survivingFish.Length;
        }

        // Save the score to be used in the next scene
        GameController.SetOceanScore(playerScore);

        // Load the next scene in the build index
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextSceneIndex);
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset
        Time.timeScale = 1.0f;

        // Clean up any event subscriptions
        if (uiManager != null)
        {
            uiManager.OnContinueClicked -= OnIntroComplete;
        }
    }
}
