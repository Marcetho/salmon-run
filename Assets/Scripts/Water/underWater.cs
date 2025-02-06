using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class UnderwaterEffect : MonoBehaviour
{
    public Transform playerCamera;
    public Color underwaterFogColor = new Color(0.2f, 0.5f, 0.7f, 1f);
    public Color underwaterBackgroundColor = new Color(0.15f, 0.4f, 0.6f, 1f);
    public float underwaterFogDensity = 0.8f;
    public float underwaterFogStart = 0f;    // Distance where fog begins
    public float underwaterFogEnd = 50f;     // Distance where fog is fully opaque

    public Image underwaterOverlay; // UI Image overlay reference
    public float overlayFadeSpeed = 2f;  // Speed at which overlay fades
    public float maxOverlayAlpha = 0.3f; // Maximum opacity of overlay
    private float targetAlpha;

    private bool isUnderwater;
    private Color originalFogColor;
    private Color originalBackgroundColor;
    private float originalFogDensity;
    private bool originalFogState;
    private FogMode originalFogMode;
    private float originalFogStart;
    private float originalFogEnd;

    void Start()
    {
        // Store original fog and background settings
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogState = RenderSettings.fog;
        originalBackgroundColor = Camera.main.backgroundColor;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;

        // Ensure the overlay is disabled initially
        if (underwaterOverlay != null)
        {
            Color c = underwaterOverlay.color;
            c.a = 0f;
            underwaterOverlay.color = c;
        }
    }

    void Update()
    {
        if (playerCamera.position.y < transform.position.y) // Attach to the water plane itself
        {
            if (!isUnderwater)
                SetUnderwater(true);
        }
        else
        {
            if (isUnderwater)
                SetUnderwater(false);
        }

        // Update overlay alpha
        if (underwaterOverlay != null)
        {
            Color currentColor = underwaterOverlay.color;
            currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * overlayFadeSpeed);
            underwaterOverlay.color = currentColor;
        }
    }

    void SetUnderwater(bool underwater)
    {
        isUnderwater = underwater;
        targetAlpha = underwater ? maxOverlayAlpha : 0f;

        if (underwater)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = underwaterFogColor;
            RenderSettings.fogStartDistance = underwaterFogStart;
            RenderSettings.fogEndDistance = underwaterFogEnd;
            if (playerCamera != null)
            {
                playerCamera.GetComponent<Camera>().backgroundColor = underwaterBackgroundColor;
            }
        }
        else
        {
            RenderSettings.fog = originalFogState;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;
            if (playerCamera != null)
            {
                playerCamera.GetComponent<Camera>().backgroundColor = originalBackgroundColor;
            }
        }
    }
}
