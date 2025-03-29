using UnityEngine;

public class NewSceneController : MonoBehaviour
{
    // The new variable that will be set based on the previous score
    public int derivedValue;

    void Start()
    {
        // Get the player's score from the previous scene
        int previousScore = TimedSceneController.playerScore;
    
        // Set your derived value based on the previous score
        // For example, maybe it's double the score
        derivedValue = 1 + (previousScore * 2);
        // Get the GameController and set its initial lives
        GameController gameController = FindFirstObjectByType<GameController>();
        if (gameController != null && previousScore >= 0)
        {
            gameController.SetInitialLives(derivedValue);
        }
        else
        {
            Debug.Log("LALALALALALA");
        }

        Debug.Log("Previous score: " + previousScore);
        Debug.Log("Derived value: " + derivedValue);

        // Optionally reset the static variable for future use
        TimedSceneController.playerScore = 0;
    }
}