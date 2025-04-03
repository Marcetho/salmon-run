using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private int maxVisibleLives = 3;  // Maximum number of life icons to display
    [SerializeField] private TextMeshProUGUI additionalLivesText;  // Text for "+n" indicator

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

        // Ensure maxVisibleLives doesn't exceed maxLives
        maxVisibleLives = Mathf.Min(maxVisibleLives, maxLives);
        lifeImages = new Image[maxVisibleLives];

        RectTransform containerRect = lifeContainer.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            Debug.LogError("LivesUI: lifeContainer doesn't have a RectTransform component!");
            return;
        }

        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;

        // Calculate sizing based on maxVisibleLives instead of maxLives
        float displayItems = maxVisibleLives + (additionalLivesText != null ? 0.5f : 0); // 0.5 for the +n text
        float spriteSize = Mathf.Min(containerWidth / displayItems * lifeSpacingPercent, containerHeight);
        float totalSpacing = containerWidth - (spriteSize * maxVisibleLives) - (additionalLivesText != null ? spriteSize * 0.5f : 0);
        float spacing = maxVisibleLives > 1 ? totalSpacing / (maxVisibleLives - 1) : 0;
        float heightOffset = (containerHeight - spriteSize) / 2;

        // Store the position of the last life icon for text placement
        Vector2 lastLifePosition = Vector2.zero;

        // Create only the visible life icons
        for (int i = 0; i < maxVisibleLives; i++)
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

                    // Track the position of the last life icon
                    if (i == maxVisibleLives - 1)
                    {
                        lastLifePosition = newLife.rectTransform.anchoredPosition;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating life icon #{i}: {e.Message}");
            }
        }

        // Initially hide the text
        if (additionalLivesText != null)
        {
            additionalLivesText.gameObject.SetActive(false);
        }

        // Initial positioning of the +n text will be done in UpdateLives
        UpdateLives();
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

        int visibleLives = Mathf.Min(currentLives, maxVisibleLives);

        // Update the visible life icons
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                if (fullLifeSprite != null && emptyLifeSprite != null)
                {
                    lifeImages[i].sprite = i < visibleLives ? fullLifeSprite : emptyLifeSprite;
                }
            }
        }

        // Update and position the additional lives text
        if (additionalLivesText != null)
        {
            if (currentLives > maxVisibleLives)
            {
                // Set the text content
                additionalLivesText.text = $"+ {currentLives - maxVisibleLives}";

                // Make text visible
                additionalLivesText.gameObject.SetActive(true);

                // Position the text next to the last visible icon
                if (visibleLives > 0 && lifeImages[visibleLives - 1] != null)
                {
                    RectTransform textRect = additionalLivesText.rectTransform;
                    RectTransform lastIconRect = lifeImages[visibleLives - 1].rectTransform;

                    // Calculate the right edge of the last icon
                    float iconWidth = lastIconRect.sizeDelta.x;
                    float iconRightEdge = lastIconRect.anchoredPosition.x + (iconWidth / 2);

                    // Position text with a small gap (30% of icon width)
                    float gap = iconWidth * -2f;
                    float textXPos = iconRightEdge + gap;

                    // Only update the X position, keep the original Y position from the Inspector
                    textRect.anchoredPosition = new Vector2(textXPos, textRect.anchoredPosition.y);
                }
            }
            else
            {
                additionalLivesText.gameObject.SetActive(false);
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
