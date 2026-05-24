using UnityEngine;

[ExecuteAlways]
public class SkyboxController : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("Leave null to use RenderSettings.skybox automatically.")]
    public Material skyboxMaterial;

    [Header("Mode")]
    public SkyMode mode = SkyMode.DayNight;

    [Header("Day/Night Cycle")]
    [Tooltip("0 = noon, 0.5 = midnight")]
    [Range(0f, 1f)] public float timeOfDay = 0f;
    [Tooltip("When enabled the timeOfDay advances automatically.")]
    public bool autoAdvanceTime = true;
    [Tooltip("How many in-game minutes pass per real second.")]
    public float minutesPerSecond = 1f;

    [Header("Sun & Moon")]
    [Tooltip("Pivot for the sun/moon arc. Defaults to world origin.")]
    public Transform orbitPivot;
    public float sunOrbitRadius = 1f;
    public float moonOrbitOffset = 180f;

    [Header("Music Visualizer (Synthwave Mode)")]
    [Range(64, 8192)] public int spectrumSamples = 256;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    [Tooltip("Low-pass smoothing for spectrum. 0 = instant, 1 = frozen.")]
    [Range(0f, 0.99f)] public float spectrumSmoothing = 0.85f;

    private Texture2D _spectrumTex;
    private float[] _spectrumRaw;
    private float[] _spectrumSmooth;
    private Color[] _spectrumPixels;

    private static readonly int PropTimeOfDay = Shader.PropertyToID("_TimeOfDay");
    private static readonly int PropNightBlend = Shader.PropertyToID("_NightBlend");
    private static readonly int PropSunDir = Shader.PropertyToID("_SunDir");
    private static readonly int PropMoonDir = Shader.PropertyToID("_MoonDir");
    private static readonly int PropSpectrumTex = Shader.PropertyToID("_SpectrumTex");
    private static readonly int PropModeKw_DayNight = Shader.PropertyToID("_MODE_DAYNIGHT");
    private static readonly int PropModeKw_Synthwave = Shader.PropertyToID("_MODE_SYNTHWAVE");

    public enum SkyMode { DayNight, Synthwave }

    void OnEnable()
    {
        if (skyboxMaterial == null)
            skyboxMaterial = RenderSettings.skybox;

        InitSpectrum();
    }

    void OnDisable()
    {
        if (_spectrumTex != null)
            DestroyImmediate(_spectrumTex);
    }

    void Update()
    {
        if (skyboxMaterial == null) return;

        if (mode == SkyMode.Synthwave)
        {
            skyboxMaterial.EnableKeyword("_MODE_SYNTHWAVE");
            skyboxMaterial.DisableKeyword("_MODE_DAYNIGHT");
        }
        else
        {
            skyboxMaterial.DisableKeyword("_MODE_SYNTHWAVE");
            skyboxMaterial.EnableKeyword("_MODE_DAYNIGHT");
        }

        if (autoAdvanceTime && Application.isPlaying)
        {
            timeOfDay = (timeOfDay + Time.deltaTime * minutesPerSecond / 1440f) % 1f;
        }

        skyboxMaterial.SetFloat(PropTimeOfDay, timeOfDay);

        float nb = ComputeNightBlend(timeOfDay);
        skyboxMaterial.SetFloat(PropNightBlend, nb);

        float sunAngle = timeOfDay * 360f - 90f;
        float moonAngle = sunAngle + moonOrbitOffset;

        Vector3 sunDir = AngleToDir(sunAngle);
        Vector3 moonDir = AngleToDir(moonAngle);

        skyboxMaterial.SetVector(PropSunDir, sunDir);
        skyboxMaterial.SetVector(PropMoonDir, moonDir);

        if (mode == SkyMode.Synthwave)
            UpdateSpectrum();
    }

    static float ComputeNightBlend(float t)
    {
        float dist = Mathf.Abs(t - 0.5f);
        float raw = 1.0f - (dist / 0.5f);
        return Mathf.SmoothStep(0.0f, 1.0f, raw);
    }

    static Vector3 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(0f, Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    void InitSpectrum()
    {
        _spectrumRaw = new float[spectrumSamples];
        _spectrumSmooth = new float[spectrumSamples];
        _spectrumPixels = new Color[256];

        _spectrumTex = new Texture2D(256, 1, TextureFormat.RFloat, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
    }

    void UpdateSpectrum()
    {
        if (_spectrumTex == null) InitSpectrum();

        AudioListener.GetSpectrumData(_spectrumRaw, 0, fftWindow);

        for (int s = 0; s < 256; s++)
        {
            int bin = Mathf.Min(s * spectrumSamples / 256, spectrumSamples - 1);
            float raw = _spectrumRaw[bin];
            _spectrumSmooth[s] = Mathf.Lerp(raw, _spectrumSmooth[s], spectrumSmoothing);
            _spectrumPixels[s] = new Color(_spectrumSmooth[s], 0f, 0f, 1f);
        }

        _spectrumTex.SetPixels(_spectrumPixels);
        _spectrumTex.Apply();

        skyboxMaterial.SetTexture(PropSpectrumTex, _spectrumTex);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector3 origin = orbitPivot ? orbitPivot.position : Vector3.zero;
        float r = 3f;

        float sunAngle = timeOfDay * 360f - 90f;
        float moonAngle = sunAngle + moonOrbitOffset;

        Vector3 sunPos = origin + AngleToDir(sunAngle) * r;
        Vector3 moonPos = origin + AngleToDir(moonAngle) * r;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(sunPos, 0.12f);
        Gizmos.DrawLine(origin, sunPos);

        Gizmos.color = new Color(0.8f, 0.9f, 1f);
        Gizmos.DrawSphere(moonPos, 0.10f);
        Gizmos.DrawLine(origin, moonPos);
    }
#endif
}