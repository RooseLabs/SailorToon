using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Analog Glitch")]
public class AnalogGlitchController : MonoBehaviour
{
    [Header("Glitch")]
    [Range(0f, 1f)] public float scanLineJitter = 0f;
    [Range(0f, 1f)] public float verticalJump = 0f;
    [Range(0f, 1f)] public float horizontalShake = 0f;
    [Range(0f, 1f)] public float colorDrift = 0f;
    [Range(0f, 1f)] public float horizontalRipple = 0f;

    [Header("Output")]
    public bool grayscale = false;

    [Header("Drift Tint")]
    [Tooltip("Force the color drift to appear as a single chosen color.")]
    public bool tintDrift = false;
    public Color driftColor = new(0f, 0.4f, 0f);
    public float driftIntensity = 1f;

    [Header("Shader")]
    [Tooltip("Leave empty to auto-find \"Hidden/KinoGlitch/Analog\".")]
    public Shader shader;

    private Material m_material;

    // Accumulated phase for the vertical-jump animation. Unlike color drift and
    // ripple (which the shader animates from _Time), the jump offset is a running
    // phase whose speed depends on the jump amount, so it must be integrated here.
    private float m_verticalJumpTime;

    private static readonly int PropScanLineJitter   = Shader.PropertyToID("_ScanLineJitter");
    private static readonly int PropVerticalJump     = Shader.PropertyToID("_VerticalJump");
    private static readonly int PropHorizontalShake  = Shader.PropertyToID("_HorizontalShake");
    private static readonly int PropColorDrift       = Shader.PropertyToID("_ColorDrift");
    private static readonly int PropHorizontalRipple = Shader.PropertyToID("_HorizontalRipple");
    private static readonly int PropDriftColor       = Shader.PropertyToID("_DriftColor");
    private static readonly int PropDriftIntensity   = Shader.PropertyToID("_DriftIntensity");

    private void OnDisable()
    {
        // Material is created with DontSave, so clean it up ourselves.
        if (m_material == null) return;
        if (Application.isPlaying) Destroy(m_material);
        else DestroyImmediate(m_material);
        m_material = null;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Shader glitchShader = shader != null ? shader : Shader.Find("Custom/AnalogGlitch");

        if (glitchShader == null || !glitchShader.isSupported)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (m_material == null || m_material.shader != glitchShader)
        {
            m_material = new Material(glitchShader) { hideFlags = HideFlags.HideAndDontSave };
        }

        // Advance the vertical-jump phase. deltaTime is valid in edit mode too
        // (ExecuteAlways drives it during scene/game-view repaints).
        m_verticalJumpTime = (m_verticalJumpTime + Time.deltaTime * verticalJump * 11.3f) % 600;

        // Map the 0..1 sliders onto the displacement amounts the shader expects.
        float scanDisp = 0.002f + Mathf.Pow(scanLineJitter, 3f) * 0.05f;

        m_material.SetFloat(PropScanLineJitter, scanDisp);
        m_material.SetVector(PropVerticalJump, new Vector2(verticalJump, m_verticalJumpTime));
        m_material.SetFloat(PropHorizontalShake, (Random.value * 2 - 1) * horizontalShake * 0.1f);
        m_material.SetFloat(PropColorDrift, colorDrift);
        m_material.SetFloat(PropHorizontalRipple, horizontalRipple);
        m_material.SetColor(PropDriftColor, driftColor);
        m_material.SetFloat(PropDriftIntensity, driftIntensity);

        if (grayscale) m_material.EnableKeyword("GRAYSCALE");
        else m_material.DisableKeyword("GRAYSCALE");

        if (tintDrift) m_material.EnableKeyword("TINT_DRIFT");
        else m_material.DisableKeyword("TINT_DRIFT");

        Graphics.Blit(source, destination, m_material);
    }
}
