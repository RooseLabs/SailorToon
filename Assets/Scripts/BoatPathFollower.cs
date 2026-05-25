using UnityEngine;

[RequireComponent(typeof(BoatController))]
public class BoatPathFollower : MonoBehaviour
{
    [Header("Snapping")]
    [Tooltip("How far above the path point the boat rides to keep its collider clear of the water surface.")]
    public float snapHeightOffset = 0.5f;

    [Tooltip("How fast the boat slides to the rail position when snapping.")]
    public float snapPositionSpeed = 6f;

    [Tooltip("How fast the boat rotates to face the path tangent when snapping.")]
    public float snapRotationSpeed = 4f;

    [Tooltip("Distance threshold at which snapping is considered complete (metres).")]
    public float snapSettleThreshold = 0.15f;

    [Header("Exit Cooldown")]
    [Tooltip("Seconds after exiting a path before the boat can snap to it again. Prevents immediate re-snap.")]
    public float exitCooldown = 2.5f;

    // ── State machine ────────────────────────────────────────────────────────
    private enum RailState { Free, Snapping, OnRail, Exiting }

    private RailState m_state = RailState.Free;
    private BezierPath m_activePath = null;
    private BezierPath m_exitedPath = null;
    private float m_exitCooldownTimer = 0f;   // counts DOWN from exitCooldown

    // ── Rail movement ────────────────────────────────────────────────────────
    private float m_pathT = 0f;
    private float m_railCurrentSpeed = 0f;
    private float m_railDirection = 1f;   // +1 = towards t=1 , -1 = towards t=0

    // ── Components ───────────────────────────────────────────────────────────
    private BoatController m_boatController;
    private Rigidbody m_rb;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        m_boatController = GetComponent<BoatController>();
        m_rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Tick the exit cooldown regardless of state
        if (m_exitCooldownTimer > 0f)
            m_exitCooldownTimer -= Time.fixedDeltaTime;

        switch (m_state)
        {
            case RailState.Free: UpdateFree(); break;
            case RailState.Snapping: UpdateSnapping(); break;
            case RailState.OnRail: UpdateOnRail(); break;
            case RailState.Exiting: UpdateExiting(); break;
        }
    }

    // ── Free ─────────────────────────────────────────────────────────────────

    private void UpdateFree()
    {
        // Only snap once the cooldown for the last-exited path has elapsed
        BezierPath nearest = BezierPathNetwork.FindNearest(
            transform.position,
            m_exitCooldownTimer > 0f ? m_exitedPath : null,
            out float nearestT);

        if (nearest != null)
            BeginSnap(nearest, nearestT);
    }

    // ── Exiting ───────────────────────────────────────────────────────────────

    private void UpdateExiting()
    {
        // Just wait for the cooldown; physics momentum naturally carries the boat away
        if (m_exitCooldownTimer <= 0f)
            EnterFree();
    }

    // ── Snap initiation ───────────────────────────────────────────────────────

    private void BeginSnap(BezierPath path, float t)
    {
        m_activePath = path;
        m_exitedPath = null;
        m_pathT = t;

        // Carry the boat's current speed into the rail so there is no dead stop
        // Use the sign of the projection to set the initial direction
        Vector3 tangent = FlatTangent(t);
        float speedOnPath = Vector3.Dot(m_boatController.CurrentSpeed * transform.forward, tangent);
        m_railDirection = speedOnPath >= 0f ? 1f : -1f;
        m_railCurrentSpeed = speedOnPath; // signed speed along path

        m_boatController.enabled = false;
        m_rb.isKinematic = true;

        m_state = RailState.Snapping;
    }

    // ── Snapping ──────────────────────────────────────────────────────────────

    private void UpdateSnapping()
    {
        if (m_activePath == null) { EnterFree(); return; }

        Vector3 railPos = RailPosition();
        Vector3 tangent = FlatTangent(m_pathT);
        Quaternion targetRot = AlignedRotation(tangent);

        m_rb.MovePosition(Vector3.Lerp(
            m_rb.position, railPos, snapPositionSpeed * Time.fixedDeltaTime));

        m_rb.MoveRotation(Quaternion.Slerp(
            m_rb.rotation, targetRot, snapRotationSpeed * Time.fixedDeltaTime));

        if (Vector3.Distance(m_rb.position, railPos) < snapSettleThreshold)
        {
            // Ensure direction is consistent with our facing after snap
            m_railDirection = Vector3.Dot(transform.forward, tangent) >= 0f ? 1f : -1f;
            m_state = RailState.OnRail;
        }
    }

    // ── On Rail ───────────────────────────────────────────────────────────────

    private void UpdateOnRail()
    {
        if (m_activePath == null) { EnterFree(); return; }

        float throttle = Input.GetAxis("Vertical");
        float totalLength = m_activePath.TotalLength;
        if (totalLength < 0.001f) { EnterFree(); return; }

        float maxFwd = m_boatController.maxForwardSpeed;
        float accel = m_boatController.acceleration;
        float drag = m_boatController.drag;

        // ── Speed update ─────────────────────────────────────────────────────
        // Only forward throttle is allowed on the rail. Backward input is ignored
        // so the player must ride to the end to exit.
        if (throttle > 0.01f)
        {
            float targetSpeed = maxFwd * throttle * m_railDirection;
            m_railCurrentSpeed = Mathf.MoveTowards(
                m_railCurrentSpeed, targetSpeed, accel * Time.fixedDeltaTime);
        }
        else
        {
            m_railCurrentSpeed = Mathf.MoveTowards(
                m_railCurrentSpeed, 0f, drag * Time.fixedDeltaTime);
        }

        // Speed must never go against the entry direction — no reversing.
        if (m_railDirection > 0f)
            m_railCurrentSpeed = Mathf.Max(0f, m_railCurrentSpeed);
        else
            m_railCurrentSpeed = Mathf.Min(0f, m_railCurrentSpeed);

        // ── Advance t ────────────────────────────────────────────────────────
        float newT = m_pathT + (m_railCurrentSpeed * Time.fixedDeltaTime) / totalLength;

        // ── Exit at the end of the path ───────────────────────────────────────
        if (newT >= 1f && m_railCurrentSpeed > 0f) { BeginExit(); return; }
        if (newT <= 0f && m_railCurrentSpeed < 0f) { BeginExit(); return; }

        m_pathT = Mathf.Clamp01(newT);

        // ── Pose the boat ─────────────────────────────────────────────────────
        // Always face the entry direction — no reversal possible.
        Vector3 tangent = FlatTangent(m_pathT);
        Vector3 faceDir = m_railDirection >= 0f ? tangent : -tangent;
        Quaternion facingRot = Quaternion.LookRotation(faceDir, Vector3.up);

        m_rb.MovePosition(RailPosition());
        m_rb.MoveRotation(facingRot);
    }

    // ── Exit ─────────────────────────────────────────────────────────────────

    private void BeginExit()
    {
        m_exitCooldownTimer = exitCooldown; // prevents immediate re-snap

        // Read tangent BEFORE nulling m_activePath — FlatTangent reads m_activePath
        Vector3 tangent = FlatTangent(m_pathT);
        Vector3 exitDir = m_railCurrentSpeed >= 0f ? tangent : -tangent;

        m_exitedPath = m_activePath;
        m_activePath = null;

        m_rb.isKinematic = false;
        m_rb.linearVelocity = exitDir * Mathf.Abs(m_railCurrentSpeed);
        m_boatController.enabled = true;

        m_state = RailState.Exiting;
    }

    private void EnterFree()
    {
        m_exitedPath = null;
        m_rb.isKinematic = false;
        m_boatController.enabled = true;
        m_state = RailState.Free;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector3 RailPosition()
    {
        return m_activePath.Evaluate(m_pathT) + Vector3.up * snapHeightOffset;
    }

    private Vector3 FlatTangent(float t)
    {
        Vector3 tan = m_activePath.Tangent(t);
        tan.y = 0f;
        return tan.sqrMagnitude < 0.0001f ? Vector3.forward : tan.normalized;
    }

    /// <summary>
    /// Returns a rotation that faces along <paramref name="flatTangent"/> or against
    /// it, whichever is closer to the boat's current forward — so the snap never
    /// flips the model 180° unnecessarily.
    /// </summary>
    private Quaternion AlignedRotation(Vector3 flatTangent)
    {
        Vector3 faceDir = Vector3.Dot(transform.forward, flatTangent) >= 0f
            ? flatTangent : -flatTangent;
        return Quaternion.LookRotation(faceDir, Vector3.up);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsOnRail => m_state == RailState.OnRail
                                  || m_state == RailState.Snapping;
    public float PathT => m_pathT;
    public BezierPath ActivePath => m_activePath;
}