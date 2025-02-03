using UnityEngine;

public class UnderwaterEffect : MonoBehaviour
{
    public Transform playerCamera;
    public Color underwaterFogColor = new Color(0.2f, 0.5f, 0.7f, 1f);
    public Color underwaterBackgroundColor = new Color(0.15f, 0.4f, 0.6f, 1f);
    public float underwaterFogDensity = 0.8f;
    public float underwaterFogStart = 0f;    // Distance where fog begins
    public float underwaterFogEnd = 50f;     // Distance where fog is fully opaque
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
    }

    void SetUnderwater(bool underwater)
    {
        isUnderwater = underwater;
        if (underwater)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = underwaterFogColor;
            RenderSettings.fogStartDistance = underwaterFogStart;
            RenderSettings.fogEndDistance = underwaterFogEnd;
            Camera.main.backgroundColor = underwaterBackgroundColor;
        }
        else
        {
            RenderSettings.fog = originalFogState;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;
            Camera.main.backgroundColor = originalBackgroundColor;
        }
    }
}
