using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CameraController : MonoBehaviour
{
    private static readonly int CameraRight = Shader.PropertyToID("_CameraRight");

    [Header("Orbit Target")]
    public Transform target;

    [Header("Orbit")]
    public float defaultOrbitDistance = 10f;
    public float minOrbitDistance = 2f;
    public float maxOrbitDistance = 20f;
    public float defaultPitch = 15f;
    public Vector2 verticalOrbitClamp = new(-10f, 70f);

    public float orbitSensitivity = 3f;
    public float scrollSpeed = 4f;
    public float zoomSmoothSpeed = 8f;
    public float recenterSmoothSpeed = 8f;

    [Header("Portal Proximity")]
    [Tooltip("When the target is within this radius of any portal, the camera blends to its saved local rest position.")]
    public float portalZoomRadius = 12f;
    [Tooltip("Higher = faster blend in/out when entering or leaving a portal's radius.")]
    public float portalBlendSpeed = 10f;
    [Tooltip("Name of the layer to drop from the camera's culling mask once it is essentially at the rest position.")]
    public string portalHiddenLayer = "Boat";
    [Tooltip("Blend value (0..1) above which the hidden layer is dropped from the culling mask.")]
    [Range(0f, 1f)] public float portalLayerHideThreshold = 0.95f;

    private Camera m_camera;
    private float m_yaw;
    private float m_pitch;
    private float m_userTargetDistance;
    private float m_currentDistance;
    private bool m_recentering;
    private float m_portalBlend;

    private Vector3 m_initialLocalPos;
    private int m_originalCullingMask;
    private int m_hiddenLayerMask;
    private bool m_layerHidden;
    private bool m_cameraStateCached;

    private Portal[] m_portals;

    private void Awake()
    {
        CacheCamera();
        InitOrbit();
    }

    private void OnEnable()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player").transform;
        CacheCamera();
        CacheCameraRestState();
        UpdateShaderGlobal();
        SetDepthTextureMode();
        InitOrbit();
        RefreshPortals();
        LockCursor();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RestoreCullingMask();
    }

    private void CacheCameraRestState()
    {
        if (!m_camera || m_cameraStateCached) return;
        m_initialLocalPos = m_camera.transform.localPosition;
        m_originalCullingMask = m_camera.cullingMask;
        int layer = LayerMask.NameToLayer(portalHiddenLayer);
        m_hiddenLayerMask = layer >= 0 ? 1 << layer : 0;
        m_layerHidden = false;
        m_cameraStateCached = true;
    }

    private void RestoreCullingMask()
    {
        if (!m_camera || !m_layerHidden) return;
        m_camera.cullingMask = m_originalCullingMask;
        m_layerHidden = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) LockCursor();
    }

    private void LockCursor()
    {
        if (!Application.isPlaying) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        UpdateShaderGlobal();

        if (!Application.isPlaying) return;
        if (!target) return;

        HandleManualLook();
        HandleZoom();
        UpdatePortalBlend();
        UpdatePortalCullingMask();
        ApplyOrbit();
    }

    private void InitOrbit()
    {
        m_pitch = defaultPitch;
        m_yaw = 0f;
        m_userTargetDistance = defaultOrbitDistance;
        m_currentDistance = defaultOrbitDistance;
        m_recentering = false;
        m_portalBlend = 0f;
    }

    public void RefreshPortals()
    {
        m_portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
    }

    private void CacheCamera()
    {
        m_camera = Camera.main;
    }

    private void UpdateShaderGlobal()
    {
        Camera activeCamera = GetActiveCamera();
        if (activeCamera) Shader.SetGlobalVector(CameraRight, activeCamera.transform.right);
    }

    private void SetDepthTextureMode()
    {
        Camera activeCamera = GetActiveCamera();
        if (activeCamera) activeCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    private Camera GetActiveCamera()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView && sceneView.camera)
                return sceneView.camera;
        }
        #endif

        if (!m_camera) CacheCamera();
        return m_camera;
    }

    private void HandleManualLook()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Input.GetMouseButtonDown(2)) m_recentering = true;
        // Any actual look input cancels an in-progress recenter.
        if (m_recentering && (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f))
            m_recentering = false;

        if (m_recentering)
        {
            m_yaw = Mathf.LerpAngle(m_yaw, 0f, Time.deltaTime * recenterSmoothSpeed);
            m_pitch = Mathf.Lerp(m_pitch, defaultPitch, Time.deltaTime * recenterSmoothSpeed);
            if (Mathf.Abs(Mathf.DeltaAngle(m_yaw, 0f)) < 0.05f &&
                Mathf.Abs(m_pitch - defaultPitch) < 0.05f)
            {
                m_yaw = 0f;
                m_pitch = defaultPitch;
                m_recentering = false;
            }
            return;
        }

        m_yaw += mouseX * orbitSensitivity;
        m_pitch -= mouseY * orbitSensitivity;
        m_pitch = Mathf.Clamp(m_pitch, verticalOrbitClamp.x, verticalOrbitClamp.y);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            m_userTargetDistance -= scroll * scrollSpeed;
            m_userTargetDistance = Mathf.Clamp(
                m_userTargetDistance, minOrbitDistance, maxOrbitDistance);
        }

        m_currentDistance = Mathf.Lerp(
            m_currentDistance, m_userTargetDistance, Time.deltaTime * zoomSmoothSpeed);
    }

    private void UpdatePortalBlend()
    {
        float targetBlend = IsTargetInPortalRadius() ? 1f : 0f;
        m_portalBlend = Mathf.MoveTowards(
            m_portalBlend, targetBlend, Time.deltaTime * portalBlendSpeed);
    }

    private bool IsTargetInPortalRadius()
    {
        if (m_portals == null || m_portals.Length == 0 || portalZoomRadius <= 0f)
            return false;

        float r2 = portalZoomRadius * portalZoomRadius;
        for (int i = 0; i < m_portals.Length; i++)
        {
            Portal p = m_portals[i];
            if (!p) continue;
            if ((target.position - p.transform.position).sqrMagnitude < r2)
                return true;
        }
        return false;
    }

    private void UpdatePortalCullingMask()
    {
        if (!m_camera || m_hiddenLayerMask == 0) return;

        bool shouldHide = m_portalBlend >= portalLayerHideThreshold;
        if (shouldHide && !m_layerHidden)
        {
            m_camera.cullingMask = m_originalCullingMask & ~m_hiddenLayerMask;
            m_layerHidden = true;
        }
        else if (!shouldHide && m_layerHidden)
        {
            m_camera.cullingMask = m_originalCullingMask;
            m_layerHidden = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (portalZoomRadius <= 0f) return;

        Portal[] portals = (m_portals != null && m_portals.Length > 0)
            ? m_portals
            : FindObjectsByType<Portal>(FindObjectsSortMode.None);
        if (portals == null) return;

        Color prev = Gizmos.color;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        for (int i = 0; i < portals.Length; i++)
        {
            Portal p = portals[i];
            if (!p) continue;
            Gizmos.DrawWireSphere(p.transform.position, portalZoomRadius);
        }
        Gizmos.color = prev;
    }

    private void ApplyOrbit()
    {
        Quaternion rot = Quaternion.Euler(m_pitch, m_yaw, 0f);
        m_camera.transform.localRotation = rot;

        Vector3 orbitPos = rot * Vector3.back * m_currentDistance;
        // Smoothstep gives a softer in/out feel than the raw linear blend.
        float t = Mathf.SmoothStep(0f, 1f, m_portalBlend);
        m_camera.transform.localPosition = Vector3.Lerp(orbitPos, m_initialLocalPos, t);
    }
}
