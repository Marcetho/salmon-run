using UnityEngine;
using UnityEngine.SceneManagement;

public class TimedSceneController : MonoBehaviour
{
    public float sceneDuration = 30f; // Duration in seconds
    private float timeRemaining;

    // This variable will be incremented by player actions
    public static int playerScore = 0;

    void Start()
    {
        // Initialize the timer
        timeRemaining = sceneDuration;
    }

    void Update()
    {
        // Count down the timer
        timeRemaining -= Time.deltaTime;

        // If time is up, load the next scene
        if (timeRemaining <= 0)
        {
            LoadNextScene();
        }

        // You can add UI to display remaining time here if needed
    }

    // This method can be called when player does something
    public void IncrementScore(int amount = 1)
    {
        playerScore = Mathf.Max(0, playerScore + amount);
        Debug.Log("Score increased to: " + playerScore);
    }

    void LoadNextScene()
    {
        // Load the next scene in the build index
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextSceneIndex);
    }
}
