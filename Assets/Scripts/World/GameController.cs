using UnityEngine;

public enum GameState {Ocean, Freshwater, Won, Lost}
public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private UIManager gameUI;

    private GameState currentState;

    private void Start()
    {
        // Start directly in ocean state since we're in game scene
        currentState = GameState.Ocean;
        gameUI.gameObject.SetActive(true);
        
        // Initialize game systems
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Add initialization logic here
    }
}
