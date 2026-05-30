using UnityEngine;

[ExecuteAlways]
public class ToonStyleManager : MonoBehaviour
{
    private const string HalftoneOn = "_TL_HALFTONE_ON";

    [Header("Lighting")]
    [ColorUsage(true, true)][SerializeField]
    private Color m_ambientColor = new(0.4f, 0.4f, 0.4f, 1f);
    [ColorUsage(true, true)][SerializeField]
    private Color m_specularColor = new(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private float m_glossiness = 32f;

    [Header("Rim")]
    [ColorUsage(true, true)][SerializeField]
    private Color m_rimColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float m_rimAmount    = 0.716f;
    [SerializeField, Range(0f, 1f)] private float m_rimThreshold = 0.1f;

    [Header("Halftone")]
    [SerializeField] private bool m_halftoneEnabled = true;
    [SerializeField, Range(2f, 120f)]  private float m_halftoneScale    = 8f;
    [SerializeField, Range(0.05f, 1f)] private float m_halftoneDotSize  = 0.5f;
    [SerializeField, Range(0f, 1f)]    private float m_halftoneSoftness = 0.15f;
    [SerializeField, Range(0.25f, 4f)] private float m_halftoneFalloff  = 1f;
    [SerializeField, Range(0f, 90f)]   private float m_halftoneAngle    = 45f;

    private static readonly int AmbientColorID     = Shader.PropertyToID("_TL_AmbientColor");
    private static readonly int SpecularColorID    = Shader.PropertyToID("_TL_SpecularColor");
    private static readonly int GlossinessID       = Shader.PropertyToID("_TL_Glossiness");
    private static readonly int RimColorID         = Shader.PropertyToID("_TL_RimColor");
    private static readonly int RimAmountID        = Shader.PropertyToID("_TL_RimAmount");
    private static readonly int RimThresholdID     = Shader.PropertyToID("_TL_RimThreshold");
    private static readonly int HalftoneScaleID    = Shader.PropertyToID("_TL_HalftoneScale");
    private static readonly int HalftoneDotSizeID  = Shader.PropertyToID("_TL_HalftoneDotSize");
    private static readonly int HalftoneSoftnessID = Shader.PropertyToID("_TL_HalftoneSoftness");
    private static readonly int HalftoneFalloffID  = Shader.PropertyToID("_TL_HalftoneFalloff");
    private static readonly int HalftoneAngleID    = Shader.PropertyToID("_TL_HalftoneAngle");

    private void OnEnable()
    {
        PushGlobals();
        ApplyKeywords();
    }

    private void OnDisable()
    {
        Shader.DisableKeyword(HalftoneOn);
    }

    private void OnValidate()
    {
        PushGlobals();
        ApplyKeywords();
    }

    private void PushGlobals()
    {
        Shader.SetGlobalColor(AmbientColorID, m_ambientColor);
        Shader.SetGlobalColor(SpecularColorID, m_specularColor);
        Shader.SetGlobalFloat(GlossinessID, m_glossiness);
        Shader.SetGlobalColor(RimColorID, m_rimColor);
        Shader.SetGlobalFloat(RimAmountID, m_rimAmount);
        Shader.SetGlobalFloat(RimThresholdID, m_rimThreshold);
        Shader.SetGlobalFloat(HalftoneScaleID, m_halftoneScale);
        Shader.SetGlobalFloat(HalftoneDotSizeID, m_halftoneDotSize);
        Shader.SetGlobalFloat(HalftoneSoftnessID, m_halftoneSoftness);
        Shader.SetGlobalFloat(HalftoneFalloffID, m_halftoneFalloff);
        Shader.SetGlobalFloat(HalftoneAngleID, m_halftoneAngle);
    }

    private void ApplyKeywords()
    {
        if (m_halftoneEnabled) Shader.EnableKeyword(HalftoneOn);
        else Shader.DisableKeyword(HalftoneOn);
    }
}
