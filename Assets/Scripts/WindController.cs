using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class WindController : MonoBehaviour
{
    public static WindController Instance { get; private set; }

    [Header("Wind")]
    [Tooltip("World-space direction the wind blows toward. Magnitude is ignored; only direction is used.")]
    public Vector3 windDirection = new(1f, 0f, 0.3f);

    [Header("Variation")]
    [Tooltip("Degrees of yaw the direction can drift over time. 0 = perfectly stable.")]
    [Range(0f, 90f)] public float wanderAmplitude = 0f;
    [Tooltip("How fast the wander cycles (Hz).")]
    [Min(0f)] public float wanderSpeed = 0.05f;

    private static readonly int PropWindDir = Shader.PropertyToID("_WindDir");

    public Vector3 CurrentDirection { get; private set; } = Vector3.right;

    private void OnEnable()
    {
        Instance = this;
        Apply();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        Vector3 baseDir = windDirection.sqrMagnitude > 1e-6f
            ? windDirection.normalized
            : Vector3.right;

        // Optional gentle yaw wander around the vertical axis.
        if (wanderAmplitude > 0.001f)
        {
            float yaw = Mathf.Sin(Time.time * wanderSpeed * Mathf.PI * 2f) * wanderAmplitude;
            baseDir = Quaternion.AngleAxis(yaw, Vector3.up) * baseDir;
        }

        CurrentDirection = baseDir;
        Shader.SetGlobalVector(PropWindDir, new Vector4(baseDir.x, baseDir.y, baseDir.z, 0f));
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 d = Application.isPlaying
            ? CurrentDirection
            : (windDirection.sqrMagnitude > 1e-6f ? windDirection.normalized : Vector3.right);

        Vector3 p = transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p, p + d * 3f);
        Gizmos.DrawSphere(p + d * 3f, 0.15f);
    }
    #endif
}
