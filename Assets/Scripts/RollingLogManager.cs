using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class RollingLogManager : MonoBehaviour
{
    private const string EnableRollingLog = "_ENABLE_ROLLING_LOG";
    private const string SphereMode       = "_ROLLING_LOG_SPHERE";

    [SerializeField, Range(-0.05f, 0.05f)] private float m_amount = 0.005f;

    public float Amount
    {
        get => m_amount;
        set
        {
            m_amount = Mathf.Clamp(value, -0.05f, 0.05f);
            Shader.SetGlobalFloat(AmountID, m_amount);
        }
    }

    [Header("Sphere Mode")]
    [SerializeField] private bool m_sphereMode = true;
    [SerializeField] private Transform m_sphereCenter;

    private static readonly int AmountID = Shader.PropertyToID("_RL_Amount");
    private static readonly int CenterID = Shader.PropertyToID("_RL_Center");

    private Coroutine m_amountRoutine;

    private void OnEnable()
    {
        if (m_sphereCenter == null)
            m_sphereCenter = GameObject.FindGameObjectWithTag("Player").transform;
        PushGlobals();
        ApplyKeywords();
        Camera.onPreCull += OnCameraPreCull;
        Camera.onPostRender += OnCameraPostRender;
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        #endif
    }

    private void OnDisable()
    {
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        #endif
        Camera.onPreCull -= OnCameraPreCull;
        Camera.onPostRender -= OnCameraPostRender;
        if (m_amountRoutine != null)
        {
            StopCoroutine(m_amountRoutine);
            m_amountRoutine = null;
        }
        Shader.DisableKeyword(EnableRollingLog);
        Shader.DisableKeyword(SphereMode);
    }

    private void LateUpdate()
    {
        // This LateUpdate needs to happen after Portal.cs LateUpdate, which is why we have [DefaultExecutionOrder(100)]
        // This is because HandleTravelers in Portal.cs can change the position of the player, and we need to push the
        // center after that.
        if (m_sphereMode) PushCenter();
    }

    private void OnCameraPreCull(Camera cam)
    {
        cam.cullingMatrix = Matrix4x4.Ortho(-999f, 999f, -999f, 999f, 0.001f, 1000f)
                          * cam.worldToCameraMatrix;
    }

    private void OnCameraPostRender(Camera cam)
    {
        cam.ResetCullingMatrix();
    }

    private void OnValidate()
    {
        PushGlobals();
        ApplyKeywords();
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

    public void LerpAmount(float target, float duration)
    {
        if (m_amountRoutine != null) StopCoroutine(m_amountRoutine);
        m_amountRoutine = StartCoroutine(LerpAmountRoutine(target, duration));
    }

    private IEnumerator LerpAmountRoutine(float target, float duration)
    {
        float start = Amount;
        if (duration <= 0f)
        {
            Amount = target;
            m_amountRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = k * k * (3f - 2f * k);
            Amount = Mathf.Lerp(start, target, k);
            yield return null;
        }

        Amount = target;
        m_amountRoutine = null;
    }
}
