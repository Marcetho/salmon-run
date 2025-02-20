using UnityEngine;

public enum GameState { Ocean, Freshwater, Won, Lost }
public class GameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private GameObject gameUIPrefab;
    [SerializeField] private PlayerMovement playerMovement;

    private UIManager uiManager;
    private GameState currentState;

    private void Start()
    {
        currentState = GameState.Ocean;

        // Spawn UI
        GameObject uiInstance = Instantiate(gameUIPrefab);
        uiManager = uiInstance.GetComponent<UIManager>();

        // Initialize game systems
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Set initial UI values
        uiManager.SetLives(3);
        uiManager.SetHealth(100);
        uiManager.SetEnergy(100);
    }

    // Add methods to handle player state changes
    public void OnPlayerDamaged(float damage)
    {
        uiManager.DecreaseHealth(damage);
    }

    public void OnEnergyUsed(float amount)
    {
        uiManager.DecreaseEnergy(amount);
    }
}