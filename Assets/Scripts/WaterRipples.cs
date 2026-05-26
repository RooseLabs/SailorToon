using UnityEngine;

public class WaterRipples : MonoBehaviour
{
    public Material waterMaterial;

    [Header("Movement")]
    public float minimumMoveDistance = 0.02f;

    [Header("Ripple Settings")]
    public float rippleSpawnInterval = 0.45f;
    public float rippleExpandSpeed = 2f;
    public float rippleLifetime = 1.4f;

    private const int RippleCount = 8;

    private static readonly int PropRippleData = Shader.PropertyToID("_RippleData");

    private struct Ripple
    {
        public Vector2 position;
        public float radius;
        public float age;
    }

    private Ripple[] m_ripples;
    private Vector4[] m_shaderRipples;

    private int m_rippleIndex;
    private float m_timer;
    private Vector3 m_lastPosition;

    private void Awake()
    {
        m_ripples = new Ripple[RippleCount];
        m_shaderRipples = new Vector4[RippleCount];
    }

    private void Start()
    {
        m_lastPosition = transform.position;
    }

    private void Update()
    {
        // Movement detection
        Vector3 currentPos = transform.position;
        float moveDistance = Vector3.Distance(currentPos, m_lastPosition);
        bool isMoving = moveDistance > minimumMoveDistance;

        // Spawn ripples while moving
        m_timer += Time.deltaTime;
        if (isMoving && m_timer >= rippleSpawnInterval)
        {
            m_timer = 0f;
            Ripple ripple = new()
            {
                position = new Vector2(currentPos.x, currentPos.z),
                radius = 0f,
                age = 0f
            };

            m_ripples[m_rippleIndex] = ripple;
            m_rippleIndex = (m_rippleIndex + 1) % RippleCount;
        }

        // Update existing ripples and build shader array
        for (int i = 0; i < m_ripples.Length; i++)
        {
            Ripple r = m_ripples[i];
            r.age += Time.deltaTime;
            r.radius += rippleExpandSpeed * Time.deltaTime;

            float fade = Mathf.Clamp01(1f - r.age / rippleLifetime);
            m_shaderRipples[i] = new Vector4(r.position.x, r.position.y, r.radius, fade);

            m_ripples[i] = r;
        }

        // Send to shader if material is assigned
        if (waterMaterial)
            waterMaterial.SetVectorArray(PropRippleData, m_shaderRipples);

        m_lastPosition = currentPos;
    }
}
