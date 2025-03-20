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
    
    [Header("Text Content")]
    [SerializeField] private string titleString = "Salmon Run";
    [SerializeField] private string subtitleString = "Fish: The Game";
    
    [Header("Scene Management")]
    [SerializeField] private string gameSceneName = "ocean";

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        titleText.text = titleString;
        subtitleText.text = subtitleString;
        startButton.onClick.AddListener(HandleStartClick);
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
}
