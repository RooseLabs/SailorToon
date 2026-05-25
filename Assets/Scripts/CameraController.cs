using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CameraController : MonoBehaviour
{
    private static readonly int CameraRight = Shader.PropertyToID("_CameraRight");

    private Camera m_camera;

    private void OnEnable()
    {
        CacheCamera();
        UpdateShaderGlobal();
    }

    private void Awake()
    {
        CacheCamera();
    }

    private void LateUpdate()
    {
        UpdateShaderGlobal();
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
}
