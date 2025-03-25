using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    private Volume postProcessVolume;
    private float currentAudioVolume;

    [Header("Above Water Effect")]
    public bool enableAboveWaterEffect = true;
    public float aboveWaterBlurAmount = 5f;
    public float blurFadeSpeed = 2f;
    public float maxBlurDistance = 2f; // Maximum distance above water for blur effect
    private float currentBlurAmount = 0f;
    private VolumeProfile originalProfile;
    private DepthOfField depthOfField;

    [Header("Volume Settings")]
    public LayerMask volumeLayer = -1; // Default to "Everything"
    public bool autoCreateGlobalVolume = true;
    [Range(0, 1)]
    public float volumePriority = 1f;
    private Volume createdVolume;
    private bool usingGlobalVolume = false;

    [Header("Debug")]
    public bool showDebugInfo = true;

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

        // Get the post-processing volume
        SetupPostProcessVolume();

        // Initialize above water post-processing effects
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            // Store the original profile
            originalProfile = postProcessVolume.profile;

            // Create a new profile instance that we can modify at runtime
            VolumeProfile newProfile = Instantiate(postProcessVolume.profile);
            postProcessVolume.profile = newProfile;

            // Get or create depth of field settings for URP
            if (!newProfile.TryGet(out depthOfField))
            {
                // Add depth of field component if it doesn't exist
                depthOfField = newProfile.Add<DepthOfField>(true);
                if (showDebugInfo) Debug.Log("Created new Depth of Field settings");
            }
            else if (showDebugInfo)
            {
                Debug.Log("Found existing Depth of Field settings");
            }

            // Force-enable depth of field component
            depthOfField.active = false;

            // Set parameters to extreme values for testing
            depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
            depthOfField.focusDistance.Override(1f); // Very close focus
            depthOfField.focalLength.Override(300f); // Maximum blur
            depthOfField.aperture.Override(0.5f);    // Wide aperture for more blur

            // Make sure overrides are applied
            depthOfField.focusDistance.overrideState = true;
            depthOfField.focalLength.overrideState = true;
            depthOfField.aperture.overrideState = true;
            depthOfField.mode.overrideState = true;

            if (showDebugInfo)
            {
                Debug.Log("Above-water blur effect initialized with these settings:");
                Debug.Log($"DOF Mode: {depthOfField.mode.value}, Focus Dist: {depthOfField.focusDistance.value}, " +
                          $"Focal Length: {depthOfField.focalLength.value}, Aperture: {depthOfField.aperture.value}");
                Debug.Log($"Volume is global: {postProcessVolume.isGlobal}, Priority: {postProcessVolume.priority}");

                // Verify camera post-processing is enabled
                var cameraData = Camera.main.GetUniversalAdditionalCameraData();
                if (cameraData != null)
                {
                    Debug.Log($"Camera post-processing enabled: {cameraData.renderPostProcessing}");
                    if (!cameraData.renderPostProcessing)
                    {
                        Debug.LogWarning("Camera post-processing is disabled! Enabling it now.");
                        cameraData.renderPostProcessing = true;
                    }
                }
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("Volume has no profile. Above-water effect won't work.");
        }
    }

    private void SetupPostProcessVolume()
    {
        // Try to find post-processing volume on the camera
        postProcessVolume = Camera.main.GetComponent<Volume>();

        // If not found on camera, check if there's one in the scene
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();

            // If still not found and auto-create is enabled, create a global volume
            if (postProcessVolume == null && autoCreateGlobalVolume)
            {
                if (showDebugInfo) Debug.Log("No Volume found in scene. Creating a global volume for underwater effects.");

                // Create a global volume
                GameObject volumeObj = new GameObject("Global Post Process Volume");
                createdVolume = volumeObj.AddComponent<Volume>();
                postProcessVolume = createdVolume;

                // Configure the volume
                postProcessVolume.isGlobal = true;
                postProcessVolume.priority = volumePriority;

                // Create a new profile
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                postProcessVolume.profile = profile;

                usingGlobalVolume = true;
            }

            if (postProcessVolume == null && showDebugInfo)
            {
                Debug.LogError("No Volume component found and auto-create is disabled. Above-water effect won't work.");
            }
        }

        // Make sure the camera has post-processing enabled
        if (Camera.main != null)
        {
            var cameraData = Camera.main.GetUniversalAdditionalCameraData();
            if (cameraData != null && !cameraData.renderPostProcessing)
            {
                cameraData.renderPostProcessing = true;
                if (showDebugInfo) Debug.Log("Enabled post-processing on main camera");
            }

            // Set camera's volume layer mask if it's not set
            if (cameraData != null && cameraData.volumeLayerMask == 0)
            {
                cameraData.volumeLayerMask = volumeLayer;
                if (showDebugInfo) Debug.Log($"Set camera volume layer mask to {volumeLayer}");
            }
        }
    }

    void Update()
    {
        if (playerCamera.position.y < transform.position.y) // Underwater check
        {
            if (!isUnderwater)
                SetUnderwater(true);
        }
        else
        {
            if (isUnderwater)
                SetUnderwater(false);

            // Handle above-water blur effect when enabled
            if (enableAboveWaterEffect && depthOfField != null)
            {
                // Calculate how close to the water surface we are
                float distanceAboveWater = playerCamera.position.y - transform.position.y;

                // Calculate target blur amount based on distance from water
                float targetBlur = 0f;
                if (distanceAboveWater < maxBlurDistance && distanceAboveWater > 0)
                {
                    // More blur when closer to water surface
                    float normalizedDistance = 1f - (distanceAboveWater / maxBlurDistance);
                    targetBlur = aboveWaterBlurAmount * normalizedDistance;

                    if (showDebugInfo && Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"Above water distance: {distanceAboveWater:F2}m, Target blur: {targetBlur:F2}");
                    }
                }

                // Smoothly transition the blur amount
                currentBlurAmount = Mathf.Lerp(currentBlurAmount, targetBlur, Time.deltaTime * blurFadeSpeed);

                // Apply the blur effect - use more aggressive values for better visibility
                if (currentBlurAmount > 0.1f)
                {
                    // Force depth of field to be active
                    depthOfField.active = true;

                    // Make the blur much more exaggerated for better visibility
                    depthOfField.focusDistance.Override(0.5f + currentBlurAmount); // Very close focus for stronger effect

                    // Use extreme values to make the effect more obvious
                    float aperture = Mathf.Lerp(4.0f, 0.5f, currentBlurAmount / aboveWaterBlurAmount);
                    float focalLength = Mathf.Lerp(50f, 300f, currentBlurAmount / aboveWaterBlurAmount);

                    depthOfField.aperture.Override(aperture);
                    depthOfField.focalLength.Override(focalLength);

                    // Log every second while effect is active
                    if (showDebugInfo && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"Active blur effect: Strength={currentBlurAmount:F2}, " +
                                 $"Focus={depthOfField.focusDistance.value:F1}, " +
                                 $"Aperture={aperture:F1}, Length={focalLength:F0}");
                    }
                }
                else
                {
                    depthOfField.active = false;

                    if (showDebugInfo && Time.frameCount % 300 == 0)
                    {
                        Debug.Log("Depth of field disabled - too far from water");
                    }
                }
            }
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

        if (underwater)
        {
            // Apply underwater effects
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = underwaterFogColor;
            RenderSettings.fogStartDistance = underwaterFogStart;
            RenderSettings.fogEndDistance = underwaterFogEnd;
            if (playerCamera != null)
            {
                playerCamera.GetComponent<Camera>().backgroundColor = underwaterBackgroundColor;
            }

            // Disable above-water blur effect
            if (depthOfField != null)
            {
                depthOfField.active = false;
                currentBlurAmount = 0f;

                if (showDebugInfo)
                {
                    Debug.Log("Underwater - Disabled blur effect");
                }
            }
        }
        else
        {
            // Restore normal water settings
            RenderSettings.fog = originalFogState;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;
            if (playerCamera != null)
            {
                playerCamera.GetComponent<Camera>().backgroundColor = originalBackgroundColor;
            }

            if (showDebugInfo)
            {
                Debug.Log("Above water - Blur effect available");
            }
        }

        // Start/Stop underwater ambient sound
        if (underwaterAmbience != null)
        {
            if (underwater && !underwaterAmbience.isPlaying)
                underwaterAmbience.Play();
        }
    }

    private void OnDestroy()
    {
        // Reset to original profile to avoid memory leaks
        if (postProcessVolume != null && originalProfile != null && postProcessVolume != createdVolume)
        {
            postProcessVolume.profile = originalProfile;
        }

        // Clean up any volume we created
        if (createdVolume != null)
        {
            Destroy(createdVolume.gameObject);
        }
    }
}
