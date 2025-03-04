using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player's lives UI, including life icons and lives-related events.
/// </summary>
public class LivesUI : UIComponent
{
    [SerializeField] private GameObject lifeContainer;
    [SerializeField] private Image lifePrefab;
    [SerializeField] private Sprite fullLifeSprite;
    [SerializeField] private Sprite emptyLifeSprite;
    [SerializeField] private int maxLives = 10;    // Maximum number of lives player can have
    [SerializeField] private float lifeSpacingPercent = 1f;   // Spacing between life icons
    [SerializeField] private UIManager uiManager;

    private int currentLives;
    private Image[] lifeImages;
    private bool isInitialized = false;

    /// <summary>
    /// Gets the current number of lives remaining.
    /// </summary>
    internal int CurrentLives => currentLives;
    internal int MaxLives => maxLives;

    /// <summary>
    /// Event triggered when lives value changes.
    /// </summary>
    internal event System.Action<int> OnLivesChanged;

    public override void Initialize()
    {
        // Check if components are assigned
        if (lifeContainer == null)
        {
            Debug.LogError("LivesUI: lifeContainer is not assigned!");
            return;
        }

        if (lifePrefab == null)
        {
            Debug.LogError("LivesUI: lifePrefab is not assigned!");
            return;
        }

        // Remove layout component if exists
        var layout = lifeContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) Destroy(layout);

        InitializeLives();
        isInitialized = true;
    }

    private void InitializeLives()
    {
        // Safety check
        if (lifeContainer == null || lifePrefab == null)
        {
            Debug.LogError("LivesUI: Cannot initialize lives - missing components!");
            return;
        }

        // Clean up existing images
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
        if (containerRect == null)
        {
            Debug.LogError("LivesUI: lifeContainer doesn't have a RectTransform component!");
            return;
        }

        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;

        float spriteSize = Mathf.Min(containerWidth / maxLives * lifeSpacingPercent, containerHeight);
        float totalSpacing = containerWidth - (spriteSize * maxLives);
        float spacing = maxLives > 1 ? totalSpacing / (maxLives - 1) : 0;
        float heightOffset = (containerHeight - spriteSize) / 2;

        for (int i = 0; i < maxLives; i++)
        {
            try
            {
                Image newLife = Instantiate(lifePrefab, lifeContainer.transform);
                if (newLife != null)
                {
                    if (fullLifeSprite != null)
                    {
                        newLife.sprite = fullLifeSprite;
                    }

                    newLife.rectTransform.sizeDelta = new Vector2(spriteSize, spriteSize);

                    float xPos = (i * (spriteSize + spacing)) - (containerWidth / 2) + (spriteSize / 2);
                    float yPos = (i % 2 == 0) ? heightOffset : -heightOffset;

                    newLife.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                    lifeImages[i] = newLife;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating life icon #{i}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Decreases player lives by one point and updates UI accordingly.
    /// Triggers game over if lives reaches zero.
    /// </summary>
    internal void DecreaseLives()
    {
        if (currentLives > 0)
        {
            currentLives--;
            UpdateLives();
            OnLivesChanged?.Invoke(currentLives);

            if (uiManager != null)
            {
                uiManager.SetHealth(100f);
                uiManager.SetEnergy(100f);
            }

            if (currentLives <= 0 && uiManager != null)
            {
                uiManager.OnGameOver();
            }
        }
    }

    internal void IncreaseLives(int amount)
    {
        if (currentLives < maxLives)
        {
            currentLives = Mathf.Min(currentLives + amount, maxLives);
            UpdateLives();
            OnLivesChanged?.Invoke(currentLives);
        }
    }

    internal void SetLives(int value)
    {
        // Make sure we're initialized before setting lives
        if (!isInitialized)
        {
            currentLives = Mathf.Clamp(value, 0, maxLives);
            return;
        }

        currentLives = Mathf.Clamp(value, 0, maxLives);
        UpdateLives();
        OnLivesChanged?.Invoke(currentLives);
    }

    internal void SetMaxLives(int value)
    {
        maxLives = value;
        if (!isInitialized)
        {
            Initialize();
        }
        else
        {
            InitializeLives();
        }
    }

    private void UpdateLives()
    {
        // Safety check
        if (lifeImages == null)
        {
            Debug.LogWarning("LivesUI: lifeImages array is null in UpdateLives!");
            return;
        }

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                if (fullLifeSprite != null && emptyLifeSprite != null)
                {
                    lifeImages[i].sprite = i < currentLives ? fullLifeSprite : emptyLifeSprite;
                }
            }
        }

        if (currentLives <= 0 && uiManager != null)
        {
            Debug.Log("Game Over");
            uiManager.OnGameOver();
        }
    }

    public override void Cleanup()
    {
    }
}
