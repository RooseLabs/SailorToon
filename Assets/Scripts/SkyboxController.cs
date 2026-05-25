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
    public float moonOrbitOffset = 180f;

    [Header("Music Visualizer (Synthwave Sky)")]
    [Range(64, 8192)] public int spectrumSamples = 256;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    [Tooltip("Low-pass smoothing for spectrum. 0 = instant, 1 = frozen.")]
    [Range(0f, 0.99f)] public float spectrumSmoothing = 0.85f;

    private Texture2D m_spectrumTex;
    private float[] m_spectrumRaw;
    private float[] m_spectrumSmooth;
    private Color[] m_spectrumPixels;

    private static readonly int PropTimeOfDay   = Shader.PropertyToID("_TimeOfDay");
    private static readonly int PropSunDir      = Shader.PropertyToID("_SunDir");
    private static readonly int PropMoonDir     = Shader.PropertyToID("_MoonDir");
    private static readonly int PropSpectrumTex = Shader.PropertyToID("_SpectrumTex");

    // Sky bits
    private const int BitSkyDayNight  = 1 << 0;  //  1
    private const int BitSkySynthwave = 1 << 1;  //  2
    // Sun bits
    private const int BitSunStandard   = 1 << 2;  //  4
    private const int BitSunSynthwave  = 1 << 3;  //  8
    private const int BitSunRaymarched = 1 << 4;  // 16
    private const int BitSunTextured   = 1 << 5;  // 32

    // Each mode is an OR of one sky bit and one sun bit.
    public enum SkyMode
    {
        DayNight            = BitSkyDayNight  | BitSunStandard,
        Synthwave           = BitSkySynthwave | BitSunSynthwave,
        RaymarchedDayNight  = BitSkyDayNight  | BitSunRaymarched,
        RaymarchedSynth     = BitSkySynthwave | BitSunRaymarched,
        TeletubbiesDayNight = BitSkyDayNight  | BitSunTextured,
        TeletubbiesSynth    = BitSkySynthwave | BitSunTextured
    }

    // Parallel arrays — index order must match the sun bit order above.
    private static readonly int[] SunBits = { BitSunStandard, BitSunSynthwave, BitSunRaymarched, BitSunTextured };
    private static readonly string[] SunKeywords = {"_SUNMODE_STANDARD", "_SUNMODE_SYNTHWAVE", "_SUNMODE_RAYMARCHED", "_SUNMODE_TEXTURED" };

    private void OnEnable()
    {
        if (skyboxMaterial == null)
            skyboxMaterial = RenderSettings.skybox;
        InitSpectrum();
    }

    private void OnDisable()
    {
        if (m_spectrumTex != null)
            DestroyImmediate(m_spectrumTex);
    }

    private void Update()
    {
        if (!skyboxMaterial) return;

        int m = (int)mode;

        // Sky keyword
        bool skyIsSynthwave = (m & BitSkySynthwave) != 0;
        skyboxMaterial.EnableKeyword (skyIsSynthwave ? "_MODE_SYNTHWAVE" : "_MODE_DAYNIGHT");
        skyboxMaterial.DisableKeyword(skyIsSynthwave ? "_MODE_DAYNIGHT"  : "_MODE_SYNTHWAVE");

        // Sun keyword — enable whichever bit is set in the current mode
        for (int i = 0; i < SunBits.Length; i++)
        {
            if ((m & SunBits[i]) != 0) skyboxMaterial.EnableKeyword(SunKeywords[i]);
            else skyboxMaterial.DisableKeyword(SunKeywords[i]);
        }

        if (autoAdvanceTime && Application.isPlaying)
            timeOfDay = (timeOfDay + Time.deltaTime * minutesPerSecond / 1440f) % 1f;

        skyboxMaterial.SetFloat(PropTimeOfDay, timeOfDay);

        float sunAngle  = timeOfDay * 360f;
        float moonAngle = sunAngle + moonOrbitOffset;

        skyboxMaterial.SetVector(PropSunDir,  AngleToDir(sunAngle));
        skyboxMaterial.SetVector(PropMoonDir, AngleToDir(moonAngle));

        if (skyIsSynthwave)
            UpdateSpectrum();
    }

    private static Vector3 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(0f, Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void InitSpectrum()
    {
        m_spectrumRaw    = new float[spectrumSamples];
        m_spectrumSmooth = new float[spectrumSamples];
        m_spectrumPixels = new Color[256];

        m_spectrumTex = new Texture2D(256, 1, TextureFormat.RFloat, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
    }

    private void UpdateSpectrum()
    {
        if (!m_spectrumTex) InitSpectrum();

        AudioListener.GetSpectrumData(m_spectrumRaw, 0, fftWindow);

        for (int s = 0; s < 256; s++)
        {
            int   bin = Mathf.Min(s * spectrumSamples / 256, spectrumSamples - 1);
            float raw = m_spectrumRaw[bin];
            m_spectrumSmooth[s] = Mathf.Lerp(raw, m_spectrumSmooth[s], spectrumSmoothing);
            m_spectrumPixels[s] = new Color(m_spectrumSmooth[s], 0f, 0f, 1f);
        }

        m_spectrumTex.SetPixels(m_spectrumPixels);
        m_spectrumTex.Apply();

        skyboxMaterial.SetTexture(PropSpectrumTex, m_spectrumTex);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 origin = orbitPivot ? orbitPivot.position : Vector3.zero;
        const float r  = 3f;

        float sunAngle  = timeOfDay * 360f;
        float moonAngle = sunAngle + moonOrbitOffset;

        Vector3 sunPos  = origin + AngleToDir(sunAngle)  * r;
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
