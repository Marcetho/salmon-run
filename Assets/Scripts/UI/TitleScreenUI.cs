using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TitleScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button closeControlsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject controlsScreen;
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
        controlsScreen.SetActive(false);
        startButton.onClick.AddListener(HandleStartClick);
        controlsButton.onClick.AddListener(ShowControls);
        closeControlsButton.onClick.AddListener(HideControls);
        quitButton.onClick.AddListener(Application.Quit);
        fishAnimator.SetBool("InWater", true);
        fishAnimator.SetFloat("Speed", 0.43f);
        if (titleMusic)
            AudioManager.i.PlayMusic(titleMusic, true, true);
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
    private void ShowControls()
    {
        controlsScreen.SetActive(true);
    }
    private void HideControls()
    {
        controlsScreen.SetActive(false);
    }
}
