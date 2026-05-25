using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("Movement")]
    public float maxForwardSpeed = 10f;
    public float maxReverseSpeed = 4f;
    public float acceleration = 5f;
    public float drag = 3f;

    [Header("Turning")]
    public float maxTurnRate = 60f;

    [Range(0f, 1f)]
    public float minSpeedToTurn = 0.05f;

    [Header("Water Snapping")]
    public LayerMask waterLayer;
    public float raycastOriginHeight = 5f;
    public float snapSmoothSpeed = 10f;
    public float tiltSmoothSpeed = 5f;

    private Rigidbody m_rb;
    private float m_currentSpeed = 0f;
    private Vector3 m_waterNormal = Vector3.up;
    private float m_waterHeight = 0f;
    private bool m_onWater = false;
    private float m_throttleInput = 0f;
    private float m_steerInput = 0f;

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_rb.useGravity = false;
        m_rb.constraints = RigidbodyConstraints.None;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate;
        m_rb.linearDamping = 2f;
        m_rb.angularDamping = 10f;

        var col = GetComponent<Collider>();
        if (col != null)
        {
            var mat = new PhysicsMaterial("BoatNoFriction");
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            col.material = mat;
        }
    }

    private void Update()
    {
        m_throttleInput = Input.GetAxis("Vertical");
        m_steerInput = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        SampleWater();
        SnapToWaterY();
        ApplyThrottle(m_throttleInput);
        ApplyTurning(m_steerInput);
        ApplyVelocity();
    }

    private void ApplyThrottle(float throttle)
    {
        if (Mathf.Abs(throttle) > 0.01f)
        {
            float targetSpeed = throttle > 0f
                ? maxForwardSpeed * throttle
                : maxReverseSpeed * throttle;

            m_currentSpeed = Mathf.MoveTowards(
                m_currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            m_currentSpeed = Mathf.MoveTowards(
                m_currentSpeed, 0f, drag * Time.fixedDeltaTime);
        }
    }

    private void ApplyTurning(float steer)
    {
        if (Mathf.Abs(steer) < 0.01f)
        {
            m_rb.angularVelocity = Vector3.Lerp(m_rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
            return;
        }

        float speedFraction = Mathf.Clamp01(
            Mathf.Abs(m_currentSpeed) / maxForwardSpeed);

        if (speedFraction < minSpeedToTurn) return;

        float direction = m_currentSpeed >= 0f ? 1f : -1f;
        float torque = steer * direction * maxTurnRate * speedFraction;
        m_rb.AddTorque(m_waterNormal * torque, ForceMode.Acceleration);
    }

    private void SampleWater()
    {
        Vector3 origin = transform.position + Vector3.up * raycastOriginHeight;
        LayerMask mask = waterLayer & ~(1 << gameObject.layer);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            raycastOriginHeight * 2f, mask, QueryTriggerInteraction.Collide))
        {
            m_waterHeight = hit.point.y;
            m_waterNormal = hit.normal;
            m_onWater = true;
        }
        else
        {
            m_onWater = false;
        }
    }

    private void SnapToWaterY()
    {
        if (!m_onWater) return;

        Quaternion currentRot = m_rb.rotation;
        Quaternion targetRot = Quaternion.FromToRotation(transform.up, m_waterNormal) * currentRot;
        m_rb.MoveRotation(Quaternion.Slerp(currentRot, targetRot, tiltSmoothSpeed * Time.fixedDeltaTime));
    }

    private void ApplyVelocity()
    {
        if (!m_onWater) return;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, m_waterNormal).normalized;
        Vector3 desiredVelocity = forward * m_currentSpeed;

        Vector3 currentXZ = new Vector3(m_rb.linearVelocity.x, 0f, m_rb.linearVelocity.z);
        Vector3 desiredXZ = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
        Vector3 correction = (desiredXZ - currentXZ) * m_rb.mass * 10f;
        m_rb.AddForce(correction, ForceMode.Force);

        // Correct Y with a force instead of teleporting
        float yError = m_waterHeight - m_rb.position.y;
        float yCorrection = (yError * snapSmoothSpeed - m_rb.linearVelocity.y) * m_rb.mass;
        m_rb.AddForce(new Vector3(0f, yCorrection, 0f), ForceMode.Force);
    }

    public float CurrentSpeed => m_currentSpeed;
    public float SpeedFraction => m_currentSpeed / maxForwardSpeed;
}