using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class RollingLogManager : MonoBehaviour
{
    private const string EnableRollingLog = "_ENABLE_ROLLING_LOG";
    private const string SphereMode       = "_ROLLING_LOG_SPHERE";

    [SerializeField, Range(-0.05f, 0.05f)] private float m_amount = 0.005f;

    [Header("Sphere Mode")]
    [SerializeField] private bool m_sphereMode = false;
    [SerializeField] private Transform m_sphereCenter;

    private static readonly int AmountID = Shader.PropertyToID("_RL_Amount");
    private static readonly int CenterID = Shader.PropertyToID("_RL_Center");

    private void OnEnable()
    {
        PushGlobals();
        ApplyKeywords();
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        #endif
    }

    private void OnDisable()
    {
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        #endif
        Shader.DisableKeyword(EnableRollingLog);
        Shader.DisableKeyword(SphereMode);
    }

    private void OnValidate()
    {
        PushGlobals();
        ApplyKeywords();
    }

    private void LateUpdate()
    {
        // Push center each frame so it tracks Transform movement.
        if (m_sphereMode) PushCenter();
    }

    #if UNITY_EDITOR
    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        ApplyKeywords();
    }
    #endif

    private void PushGlobals()
    {
        Shader.SetGlobalFloat(AmountID, m_amount);
        PushCenter();
    }

    private void PushCenter()
    {
        Transform t = m_sphereCenter ? m_sphereCenter : transform;
        Shader.SetGlobalVector(CenterID, t.position);
    }

    private void ApplyKeywords()
    {
        if (Application.isPlaying) Shader.EnableKeyword(EnableRollingLog);
        else Shader.DisableKeyword(EnableRollingLog);

        if (m_sphereMode) Shader.EnableKeyword(SphereMode);
        else Shader.DisableKeyword(SphereMode);
    }
}
