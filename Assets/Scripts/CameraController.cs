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
    public float followSmoothSpeed = 6f;

    [Header("Recenter")]
    [Tooltip("Seconds after releasing right mouse before the camera drifts back behind the target.")]
    public float recenterDelay = 2f;

    [Header("Portal Proximity Zoom")]
    public float portalZoomRadius = 12f;
    public float portalZoomDistance = 3f;

    private Camera m_camera;
    private float m_yaw;
    private float m_pitch;
    private float m_userTargetDistance;
    private float m_targetDistance;
    private float m_currentDistance;
    private bool m_inManualMode;
    private float m_manualReleaseTime = float.NegativeInfinity;

    private Portal[] m_portals;

    private void Awake()
    {
        CacheCamera();
        InitOrbit();
    }

    private void OnEnable()
    {
        CacheCamera();
        UpdateShaderGlobal();
        SetDepthTextureMode();
        InitOrbit();
        RefreshPortals();
    }

    private void LateUpdate()
    {
        UpdateShaderGlobal();

        if (!Application.isPlaying) return;
        if (!target) return;

        HandleManualLook();
        HandleZoom();
        ApplyOrbit();
    }

    private void InitOrbit()
    {
        m_pitch = defaultPitch;
        m_yaw = 0f;
        m_userTargetDistance = defaultOrbitDistance;
        m_targetDistance = defaultOrbitDistance;
        m_currentDistance = defaultOrbitDistance;
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
        if (Input.GetMouseButton(1))
        {
            m_yaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            m_pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            m_pitch = Mathf.Clamp(m_pitch, verticalOrbitClamp.x, verticalOrbitClamp.y);
            m_inManualMode = true;
            return;
        }

        if (m_inManualMode)
        {
            m_inManualMode = false;
            m_manualReleaseTime = Time.time;
        }

        if (Time.time - m_manualReleaseTime >= recenterDelay)
        {
            m_yaw = Mathf.LerpAngle(m_yaw, 0f, Time.deltaTime * followSmoothSpeed);
            m_pitch = Mathf.Lerp(m_pitch, defaultPitch, Time.deltaTime * followSmoothSpeed);
        }
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

        float proximityDistance = ComputePortalProximityDistance(out bool nearPortal);
        m_targetDistance = nearPortal ? proximityDistance : m_userTargetDistance;

        m_currentDistance = Mathf.Lerp(
            m_currentDistance, m_targetDistance, Time.deltaTime * zoomSmoothSpeed);
    }

    private float ComputePortalProximityDistance(out bool nearPortal)
    {
        nearPortal = false;
        if (m_portals == null || m_portals.Length == 0 || portalZoomRadius <= 0f)
            return m_userTargetDistance;

        float minDist = float.PositiveInfinity;
        for (int i = 0; i < m_portals.Length; i++)
        {
            Portal p = m_portals[i];
            if (!p) continue;
            float d = Vector3.Distance(target.position, p.transform.position);
            if (d < minDist) minDist = d;
        }

        if (minDist >= portalZoomRadius) return m_userTargetDistance;

        nearPortal = true;
        float t = Mathf.Clamp01(minDist / portalZoomRadius);
        // Right at the portal => portalZoomDistance. At the radius edge => user's distance.
        return Mathf.Lerp(portalZoomDistance, m_userTargetDistance, t);
    }

    private void ApplyOrbit()
    {
        Quaternion rot = Quaternion.Euler(m_pitch, m_yaw, 0f);
        m_camera.transform.localRotation = rot;
        m_camera.transform.localPosition = rot * Vector3.back * m_currentDistance;
    }
}
