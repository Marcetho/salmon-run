using UnityEngine;
using UnityEngine.UI;

public class HealthUI : UIComponent
{
    [SerializeField] private Button healthButton;
    [SerializeField] private GameObject lifeContainer;
    [SerializeField] private Image lifePrefab;
    [SerializeField] private Sprite fullLifeSprite;
    [SerializeField] private Sprite emptyLifeSprite;
    [SerializeField] private int maxLives = 10;
    [SerializeField] private float lifeSpacingPercent = 1f;
    [SerializeField] private UIManager uiManager;

    private int currentLives;
    private Image[] lifeImages;

    public override void Initialize()
    {
        var layout = lifeContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) Destroy(layout);

        healthButton.onClick.AddListener(OnHealthButtonClick);
        InitializeLives();
    }

    private void InitializeLives()
    {
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

        float spriteSize = Mathf.Min(containerWidth / maxLives * lifeSpacingPercent, containerHeight);
        float totalSpacing = containerWidth - (spriteSize * maxLives);
        float spacing = maxLives > 1 ? totalSpacing / (maxLives - 1) : 0;
        float heightOffset = (containerHeight - spriteSize) / 2;

        for (int i = 0; i < maxLives; i++)
        {
            Image newLife = Instantiate(lifePrefab, lifeContainer.transform);
            newLife.sprite = fullLifeSprite;

            newLife.rectTransform.sizeDelta = new Vector2(spriteSize, spriteSize);

            float xPos = (i * (spriteSize + spacing)) - (containerWidth / 2) + (spriteSize / 2);
            float yPos = (i % 2 == 0) ? heightOffset : -heightOffset;

            newLife.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            lifeImages[i] = newLife;
        }
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

        if (currentLives <= 0)
        {
            Debug.Log("Game Over");
            uiManager.OnGameOver();
        }
    }
}
