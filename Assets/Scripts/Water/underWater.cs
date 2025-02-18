using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using UnityEngine.Rendering.PostProcessing;

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

    [Header("Audio")]
    public AudioSource underwaterAmbience;
    public float audioTransitionSpeed = 1f;

    [Header("Effects")]
    public PostProcessProfile underwaterProfile;
    public PostProcessProfile normalProfile;
    public ParticleSystem bubbleEffect;
    public Renderer underwaterDistortionRenderer; // Change this line - use Renderer instead of Material

    private PostProcessVolume postProcessVolume;
    private float currentAudioVolume;

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

        postProcessVolume = Camera.main.GetComponent<PostProcessVolume>();
        if (bubbleEffect != null) bubbleEffect.Stop();
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

        // Update audio transition
        if (underwaterAmbience != null)
        {
            float targetVolume = isUnderwater ? 1f : 0f;
            currentAudioVolume = Mathf.Lerp(currentAudioVolume, targetVolume, Time.deltaTime * audioTransitionSpeed);
            underwaterAmbience.volume = currentAudioVolume;
        }
    }

    void SetUnderwater(bool underwater)
    {
        isUnderwater = underwater;
        targetAlpha = underwater ? maxOverlayAlpha : 0f;

        // Handle particle effects
        if (underwater && bubbleEffect != null)
            bubbleEffect.Play();
        else if (bubbleEffect != null)
            bubbleEffect.Stop();

        // Handle post processing
        if (postProcessVolume != null)
            postProcessVolume.profile = underwater ? underwaterProfile : normalProfile;

        // Handle distortion effect - replace the material.enabled line with this:
        if (underwaterDistortionRenderer != null)
        {
            underwaterDistortionRenderer.enabled = underwater;
        }

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

        // Start/Stop underwater ambient sound
        if (underwaterAmbience != null)
        {
            if (underwater && !underwaterAmbience.isPlaying)
                underwaterAmbience.Play();
        }
    }
}
