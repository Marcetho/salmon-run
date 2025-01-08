using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private PlayerHudUI playerHud;
    [SerializeField] private GameoverTextUI gameOverTextUI;
    private void Start()
    {
        playerHud.Initialize();
        gameOverTextUI.Initialize();
    }

    private void OnDestroy()
    {
        playerHud.Cleanup();
    }

    public void OnGameOver()
    {
        gameOverTextUI.ShowGameOver();
        playerHud.OnGameOver();
    }
}