using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class PostProcessController : MonoBehaviour
{
    [Header("Material")]
    public Material postProcessMaterial;

    [Header("Chromatic Aberration")]
    [Range(0f, 0.05f)] public float chromaStrength = 0.02f;
    [Range(0f, 5f)] public float chromaPulse = 5.0f;

    [Header("Saturation")]
    [Range(0f, 4f)] public float saturation = 1.8f;

    [Header("Vignette")]
    [Range(0f, 1f)] public float vignetteStrength = 0.45f;
    [Range(1f, 8f)] public float vignettePower = 3.0f;

    [Header("Scanlines")]
    [Range(0f, 1f)] public float scanlineStrength = 0.08f;
    [Range(100f, 1200f)] public float scanlineCount = 600f;

    [Header("Color Grading")]
    public Color shadowTint = new Color(0.05f, 0f, 0.1f);
    public Color highlightTint = new Color(0f, 0.05f, 0.1f);
    [Range(0.5f, 1.5f)] public float brightness = 1.05f;
    [Range(0.5f, 2f)] public float contrast = 1.1f;

    [Header("Bass Zoom")]
    [Range(0f, 0.1f)] public float zoomStrength = 0.03f;
    [Range(0.1f, 1f)] public float zoomCurve = 0.4f;

    [Header("Music Reactivity")]
    public SkyboxController skyboxController;
    [Range(64, 8192)] public int spectrumSamples = 256;
    [Range(0f, 0.99f)] public float bassSmoothing = 0.8f;
    [Range(5f, 100f)] public float bassBoost = 20f;

    private float _smoothedBass = 0f;
    private float[] _spectrumData;

    private static readonly int PropChromaStr = Shader.PropertyToID("_ChromaStrength");
    private static readonly int PropChromaPulse = Shader.PropertyToID("_ChromaPulse");
    private static readonly int PropSaturation = Shader.PropertyToID("_Saturation");
    private static readonly int PropVigStr = Shader.PropertyToID("_VignetteStr");
    private static readonly int PropVigPow = Shader.PropertyToID("_VignettePow");
    private static readonly int PropScanStr = Shader.PropertyToID("_ScanlineStr");
    private static readonly int PropScanCount = Shader.PropertyToID("_ScanlineCount");
    private static readonly int PropShadowTint = Shader.PropertyToID("_ShadowTint");
    private static readonly int PropHighTint = Shader.PropertyToID("_HighlightTint");
    private static readonly int PropBrightness = Shader.PropertyToID("_Brightness");
    private static readonly int PropContrast = Shader.PropertyToID("_Contrast");
    private static readonly int PropBassValue = Shader.PropertyToID("_BassValue");
    private static readonly int PropZoomStr = Shader.PropertyToID("_ZoomStrength");
    private static readonly int PropZoomCurve = Shader.PropertyToID("_ZoomCurve");

    void OnEnable()
    {
        _spectrumData = new float[spectrumSamples];
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (postProcessMaterial == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        UpdateBass();

        postProcessMaterial.SetFloat(PropChromaStr, chromaStrength);
        postProcessMaterial.SetFloat(PropChromaPulse, chromaPulse);
        postProcessMaterial.SetFloat(PropSaturation, saturation);
        postProcessMaterial.SetFloat(PropVigStr, vignetteStrength);
        postProcessMaterial.SetFloat(PropVigPow, vignettePower);
        postProcessMaterial.SetFloat(PropScanStr, scanlineStrength);
        postProcessMaterial.SetFloat(PropScanCount, scanlineCount);
        postProcessMaterial.SetColor(PropShadowTint, shadowTint);
        postProcessMaterial.SetColor(PropHighTint, highlightTint);
        postProcessMaterial.SetFloat(PropBrightness, brightness);
        postProcessMaterial.SetFloat(PropContrast, contrast);
        postProcessMaterial.SetFloat(PropBassValue, _smoothedBass);
        postProcessMaterial.SetFloat(PropZoomStr, zoomStrength);
        postProcessMaterial.SetFloat(PropZoomCurve, zoomCurve);

        Graphics.Blit(src, dest, postProcessMaterial);
    }

    void UpdateBass()
    {
        if (!Application.isPlaying) return;

        AudioListener.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);

        float rawBass = 0f;
        int bins = Mathf.Min(6, spectrumSamples);
        for (int b = 0; b < bins; b++)
            rawBass += _spectrumData[b];
        rawBass /= bins;

        rawBass = Mathf.Clamp01(rawBass * bassBoost);

        if (rawBass > _smoothedBass)
            _smoothedBass = rawBass;
        else
            _smoothedBass = Mathf.Lerp(rawBass, _smoothedBass, bassSmoothing);
    }
}