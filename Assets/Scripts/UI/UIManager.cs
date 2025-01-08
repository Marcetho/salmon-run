using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button scoreButton;
    [SerializeField] private Button healthButton;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject lifeContainer;
    [SerializeField] private Image lifePrefab;
    [SerializeField] private Sprite fullLifeSprite;
    [SerializeField] private Sprite emptyLifeSprite;
    [SerializeField] private int maxLives;

    private int score = 0;
    private int currentLives;
    private Image[] lifeImages;

    private void Start()
    {
        var layout = lifeContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) Destroy(layout);

        scoreButton.onClick.AddListener(OnScoreButtonClick);
        healthButton.onClick.AddListener(OnHealthButtonClick);
        UpdateScore(0);

        InitializeLives();
    }

    public void InitializeLives()
    {
        // Clear previous life images
        if (lifeImages != null)
        {
            foreach (var image in lifeImages)
            {
                if (image != null) Destroy(image.gameObject);
            }
        }

        currentLives = maxLives;
        lifeImages = new Image[maxLives];

        RectTransform containerRect = lifeContainer.GetComponent<RectTransform>();
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;
        
        float spriteSize = Mathf.Min(containerWidth / maxLives * 0.8f, containerHeight * 0.4f);
        float spacing = spriteSize * 1.2f; 
        float heightOffset = containerHeight * 0.2f;
        float leftPadding = spriteSize * 0.5f; // Add some padding from the left edge

        for (int i = 0; i < maxLives; i++)
        {
            Image newLife = Instantiate(lifePrefab, lifeContainer.transform);
            newLife.sprite = fullLifeSprite;

            newLife.rectTransform.sizeDelta = new Vector2(spriteSize, spriteSize);

            float xPos = (i * spacing) + leftPadding - (containerWidth / 2); // Adjust for RectTransform anchor point
            float yPos = (i % 2 == 1) ? heightOffset : -heightOffset;

            newLife.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            lifeImages[i] = newLife;
        }
    }

    private void OnScoreButtonClick()
    {
        score += 10;
        UpdateScore(score);
    }

    private void OnHealthButtonClick()
    {
        if (currentLives > 0)
        {
            currentLives--;
            UpdateLives();
        }
    }

    private void UpdateLives()
    {
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                lifeImages[i].sprite = i < currentLives ? fullLifeSprite : emptyLifeSprite;
            }
        }
    }

    private void UpdateScore(int newScore)
    {
        scoreText.text = $"Score: {newScore}";
    }
}