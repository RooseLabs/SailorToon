using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CameraController : MonoBehaviour
{
    private static readonly int CameraRight = Shader.PropertyToID("_CameraRight");

    private Camera m_camera;

    [Header("Orbit Target")]
    public Transform target;

    [Header("Orbit Settings")]
    public float orbitDistance = 10f;
    public float minOrbitDistance = 2f;
    public float maxOrbitDistance = 40f;
    public float scrollSpeed = 4f;
    public float zoomSmoothSpeed = 8f;
    public float orbitSensitivity = 3f;
    public Vector2 verticalOrbitClamp = new Vector2(-10f, 70f);
    public float followSmoothSpeed = 6f;

    [Header("First Person")]
    public float firstPersonThreshold = 1.5f;
    public Vector3 firstPersonOffset = new Vector3(0f, 1.6f, 0.5f);
    public float firstPersonSensitivity = 2f;
    public Vector2 firstPersonVerticalClamp = new Vector2(-60f, 60f);

    private bool m_manualControl = false;
    private float m_yaw = 0f;
    private float m_pitch = 20f;
    private float m_targetDistance;
    private float m_currentDistance;
    private bool m_isFirstPerson = false;

    private float m_fpYaw = 0f;
    private float m_fpPitch = 0f;

    private void OnEnable()
    {
        CacheCamera();
        UpdateShaderGlobal();
        InitOrbit();
        SetDepthTextureMode();
    }

    private void Awake()
    {
        CacheCamera();
        InitOrbit();
    }

    private void LateUpdate()
    {
        UpdateShaderGlobal();

        if (!Application.isPlaying) return;
        if (!target) return;

        HandleZoom();

        if (m_isFirstPerson)
            UpdateFirstPerson();
        else
            UpdateOrbit();
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

    private void InitOrbit()
    {
        m_targetDistance = orbitDistance;
        m_currentDistance = orbitDistance;
        m_yaw = transform.eulerAngles.y;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (m_isFirstPerson)
        {
            if (scroll < 0f)
                ExitFirstPerson();
            return;
        }

        if (Mathf.Abs(scroll) > 0.001f)
        {
            m_targetDistance -= scroll * scrollSpeed;
            m_targetDistance = Mathf.Clamp(
                m_targetDistance, minOrbitDistance, maxOrbitDistance);
        }

        m_currentDistance = Mathf.Lerp(
            m_currentDistance, m_targetDistance, Time.deltaTime * zoomSmoothSpeed);

        if (m_currentDistance <= firstPersonThreshold)
            EnterFirstPerson();
    }

    private void UpdateOrbit()
    {
        if (Input.GetMouseButton(1))
        {
            m_manualControl = true;
            m_yaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            m_pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            m_pitch = Mathf.Clamp(m_pitch, verticalOrbitClamp.x, verticalOrbitClamp.y);
        }
        else
        {
            if (m_manualControl)
                m_manualControl = false;

            float targetYaw = target.eulerAngles.y;
            m_yaw = Mathf.LerpAngle(m_yaw, targetYaw, Time.deltaTime * followSmoothSpeed);
        }

        Quaternion rotation = Quaternion.Euler(m_pitch, m_yaw, 0f);
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPos = target.position + direction * m_currentDistance;

        transform.position = Vector3.Lerp(
            transform.position, desiredPos, Time.deltaTime * followSmoothSpeed);

        transform.rotation = Quaternion.Slerp(
            transform.rotation, rotation, Time.deltaTime * followSmoothSpeed);
    }

    private void EnterFirstPerson()
    {
        m_isFirstPerson = true;
        m_currentDistance = 0f;
        m_targetDistance = 0f;
        m_fpYaw = m_yaw;
        m_fpPitch = m_pitch;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitFirstPerson()
    {
        m_isFirstPerson = false;
        m_currentDistance = firstPersonThreshold + 0.1f;
        m_targetDistance = minOrbitDistance;
        m_yaw = m_fpYaw;
        m_pitch = m_fpPitch;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateFirstPerson()
    {
        m_fpYaw += Input.GetAxis("Mouse X") * firstPersonSensitivity;
        m_fpPitch -= Input.GetAxis("Mouse Y") * firstPersonSensitivity;
        m_fpPitch = Mathf.Clamp(m_fpPitch,
            firstPersonVerticalClamp.x, firstPersonVerticalClamp.y);

        transform.position = target.TransformPoint(firstPersonOffset);
        transform.rotation = Quaternion.Euler(m_fpPitch, m_fpYaw, 0f);
    }
}
