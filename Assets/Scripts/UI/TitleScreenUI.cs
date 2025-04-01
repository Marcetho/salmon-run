using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor;

public class TitleScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Animator fishAnimator;
    
    [Header("Text Content")]
    [SerializeField] private string titleString = "Salmon Run";
    [SerializeField] private string subtitleString = "Fish: The Game";
    
    [Header("Scene Management")]
    [SerializeField] private string gameSceneName = "ocean";

    [Header("Music")]
    [SerializeField] AudioClip titleMusic;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        titleText.text = titleString;
        subtitleText.text = subtitleString;
        startButton.onClick.AddListener(HandleStartClick);
        quitButton.onClick.AddListener(CloseGame); 
        fishAnimator.SetBool("InWater", true);
        fishAnimator.SetFloat("Speed", 0.43f);
        if (titleMusic)
            AudioManager.i.PlayMusic(titleMusic, true);
    }

    private void OnDestroy()
    {
        startButton.onClick.RemoveListener(HandleStartClick);
    }

    private void HandleStartClick()
    {
        // Add loading screen or transition animation here
        SceneManager.LoadScene(gameSceneName);
    }
    private void CloseGame()
    {
        EditorApplication.isPlaying = false; //for illustrative purposes using unity editor
        Application.Quit(); //only works after building the game
    }
}
