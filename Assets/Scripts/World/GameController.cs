using Unity.VisualScripting;
using UnityEngine;

public enum GameState {Title, Ocean, Freshwater, Won, Lost}
public class GameController : MonoBehaviour
{
    [SerializeField] PlayerData playerData;
    GameState currentState;
    void Start()
    {
        //open title screen
        currentState = GameState.Title;
    }
}
