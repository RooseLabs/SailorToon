using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : PortalTraveler
{
    [Header("Movement")]
    public float maxForwardSpeed = 10f;
    public float maxReverseSpeed = 4f;
    public float acceleration = 5f;
    public float drag = 3f;

    [Header("Turning")]
    public float maxTurnRate = 60f;
    [Range(0f, 1f)] public float minSpeedToTurn = 0.05f;

    [Header("Water Snapping")]
    public LayerMask waterLayer;
    public float raycastOriginHeight = 5f;
    public float snapSmoothSpeed = 10f;
    public float tiltSmoothSpeed = 5f;

    [Header("Rail Snapping")]
    [Tooltip("How far above the path point the boat rides to keep its collider clear of the water surface.")]
    public float snapHeightOffset = 0.5f;
    [Tooltip("How fast the boat slides to the rail position when snapping.")]
    public float snapPositionSpeed = 6f;
    [Tooltip("How fast the boat rotates to face the path tangent when snapping.")]
    public float snapRotationSpeed = 4f;
    [Tooltip("Distance threshold at which snapping is considered complete (metres).")]
    public float snapSettleThreshold = 0.15f;

    [Header("Rail Exit Cooldown")]
    [Tooltip("Seconds after exiting a path before the boat can snap to it again. Prevents immediate re-snap.")]
    public float exitCooldown = 2.5f;


    private enum RailState { Free, Snapping, OnRail, Exiting }

    private Rigidbody m_rb;

    // Free-roam state
    private float m_currentSpeed;
    private Vector3 m_waterNormal = Vector3.up;
    private float m_waterHeight;
    private bool m_onWater;
    private float m_throttleInput;
    private float m_steerInput;

    // Rail state
    private RailState m_state = RailState.Free;
    private BezierPath m_activePath;
    private BezierPath m_exitedPath;
    private float m_exitCooldownTimer;
    private float m_pathT;
    private float m_railCurrentSpeed;
    private float m_railDirection = 1f;

    public float CurrentSpeed => m_state == RailState.OnRail || m_state == RailState.Snapping
        ? m_railCurrentSpeed
        : m_currentSpeed;
    public float SpeedFraction => CurrentSpeed / maxForwardSpeed;
    public bool IsOnRail => m_state == RailState.OnRail || m_state == RailState.Snapping;
    public float PathT => m_pathT;
    public BezierPath ActivePath => m_activePath;

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

    // --- Free-roam driving ---

    private void UpdateFree()
    {
        SampleWater();
        SnapToWaterY();
        ApplyThrottle(m_throttleInput);
        ApplyTurning(m_steerInput);
        ApplyVelocity();

        BezierPath nearest = BezierPathNetwork.FindNearest(
            transform.position,
            m_exitCooldownTimer > 0f ? m_exitedPath : null,
            out float nearestT);

        if (nearest != null)
            BeginSnap(nearest, nearestT);
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

        float speedFraction = Mathf.Clamp01(Mathf.Abs(m_currentSpeed) / maxForwardSpeed);
        if (speedFraction < minSpeedToTurn) return;

        float direction = m_currentSpeed >= 0f ? 1f : -1f;
        float torque = steer * direction * maxTurnRate * speedFraction;
        m_rb.AddTorque(m_waterNormal * torque, ForceMode.Acceleration);
    }

    private void SampleWater()
    {
        Vector3 origin = transform.position + Vector3.up * raycastOriginHeight;
        LayerMask mask = waterLayer & ~(1 << gameObject.layer);

        // Long downward ray so a boat suspended high above the water (e.g. after a
        // failed post-portal snap) still detects water and falls back to it.
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                Mathf.Infinity, mask, QueryTriggerInteraction.Collide))
        {
            m_waterHeight = hit.point.y;
            m_waterNormal = hit.normal;
            m_onWater = true;
        }
        else
        {
            m_onWater = false;
        }

        // Gravity only when dynamic and away from the surface; once close to (or under)
        // the water the existing Y-correction force takes over.
        if (!m_rb.isKinematic)
            m_rb.useGravity = !m_onWater || (m_rb.position.y - m_waterHeight) > raycastOriginHeight;
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

        Vector3 currentXZ = new(m_rb.linearVelocity.x, 0f, m_rb.linearVelocity.z);
        Vector3 desiredXZ = new(desiredVelocity.x, 0f, desiredVelocity.z);
        Vector3 correction = (desiredXZ - currentXZ) * (m_rb.mass * 10f);
        m_rb.AddForce(correction, ForceMode.Force);

        float yError = m_waterHeight - m_rb.position.y;
        float yCorrection = (yError * snapSmoothSpeed - m_rb.linearVelocity.y) * m_rb.mass;
        m_rb.AddForce(new Vector3(0f, yCorrection, 0f), ForceMode.Force);
    }

    // --- Rail-following ---

    private void UpdateExiting()
    {
        SampleWater();
        SnapToWaterY();
        ApplyThrottle(m_throttleInput);
        ApplyTurning(m_steerInput);
        ApplyVelocity();

        BezierPath nearest = BezierPathNetwork.FindNearest(
            transform.position,
            m_exitedPath,
            out float nearestT);

        if (nearest != null)
        {
            BeginSnap(nearest, nearestT);
        }
        else if (m_exitCooldownTimer <= 0f)
        {
            EnterFree();
        }
    }

    private void BeginSnap(BezierPath path, float t)
    {
        m_activePath = path;
        m_exitedPath = null;
        m_pathT = t;

        Vector3 rawTan = m_activePath.Tangent(t);
        float speedOnPath = Vector3.Dot(m_currentSpeed * transform.forward, rawTan);
        m_railDirection = speedOnPath >= 0f ? 1f : -1f;
        m_railCurrentSpeed = speedOnPath;

        m_rb.isKinematic = true;
        m_state = RailState.Snapping;
    }

    private void UpdateSnapping()
    {
        if (m_activePath == null) { EnterFree(); return; }

        Vector3 railPos = RailPosition();
        Vector3 rawTan = m_activePath.Tangent(m_pathT);
        Quaternion targetRot = AlignedRotation(rawTan);

        m_rb.MovePosition(Vector3.Lerp(
            m_rb.position, railPos, snapPositionSpeed * Time.fixedDeltaTime));
        m_rb.MoveRotation(Quaternion.Slerp(
            m_rb.rotation, targetRot, snapRotationSpeed * Time.fixedDeltaTime));

        if (Vector3.Distance(m_rb.position, railPos) < snapSettleThreshold)
        {
            m_railDirection = Vector3.Dot(transform.forward, rawTan) >= 0f ? 1f : -1f;
            m_state = RailState.OnRail;
        }
    }

    private void UpdateOnRail()
    {
        if (m_activePath == null) { EnterFree(); return; }

        float totalLength = m_activePath.TotalLength;
        if (totalLength < 0.001f) { EnterFree(); return; }

        if (m_throttleInput > 0.01f)
        {
            float targetSpeed = maxForwardSpeed * m_throttleInput * m_railDirection;
            m_railCurrentSpeed = Mathf.MoveTowards(
                m_railCurrentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            m_railCurrentSpeed = Mathf.MoveTowards(
                m_railCurrentSpeed, 0f, drag * Time.fixedDeltaTime);
        }

        if (m_railDirection > 0f)
            m_railCurrentSpeed = Mathf.Max(0f, m_railCurrentSpeed);
        else
            m_railCurrentSpeed = Mathf.Min(0f, m_railCurrentSpeed);

        float newT = m_pathT + (m_railCurrentSpeed * Time.fixedDeltaTime) / totalLength;

        bool overEnd = newT >= 1f && m_railCurrentSpeed > 0f;
        bool overStart = newT <= 0f && m_railCurrentSpeed < 0f;
        if (overEnd || overStart)
        {
            // Only drift kinematically through the *connecting* end of a linked path - the
            // portal trigger swaps us onto the linked path on the next physics step. At the
            // far (non-portal) end we fall through to BeginExit and hand back manual control.
            bool atConnectingEnd = m_activePath.linkedPath != null
                && ((overEnd && m_activePath.connectingEnd == BezierPath.ConnectingEnd.End)
                    || (overStart && m_activePath.connectingEnd == BezierPath.ConnectingEnd.Start));

            if (atConnectingEnd)
            {
                m_pathT = overEnd ? 1f : 0f;
                Vector3 driftDir = FullTangent(m_pathT);
                m_rb.MovePosition(m_rb.position
                    + driftDir * (Mathf.Abs(m_railCurrentSpeed) * Time.fixedDeltaTime));
                m_rb.MoveRotation(TangentRotation(driftDir));
                return;
            }

            BeginExit();
            return;
        }

        m_pathT = Mathf.Clamp01(newT);

        Vector3 tangent = FullTangent(m_pathT);
        Quaternion facingRot = TangentRotation(tangent);

        m_rb.MovePosition(RailPosition());
        m_rb.MoveRotation(facingRot);
    }

    private void BeginExit()
    {
        m_exitCooldownTimer = exitCooldown;

        Vector3 exitDir = FullTangent(m_pathT);
        m_exitedPath = m_activePath;
        m_activePath = null;

        m_rb.isKinematic = false;
        m_rb.linearVelocity = exitDir * Mathf.Abs(m_railCurrentSpeed);
        m_currentSpeed = Mathf.Abs(m_railCurrentSpeed);
        m_state = RailState.Exiting;
    }

    private void EnterFree()
    {
        m_exitedPath = null;
        m_activePath = null;
        if (m_rb.isKinematic) m_rb.isKinematic = false;
        m_state = RailState.Free;
    }

    private Vector3 RailPosition()
    {
        return m_activePath.Evaluate(m_pathT) + Vector3.up * snapHeightOffset;
    }

    private Vector3 FullTangent(float t)
    {
        Vector3 tan = m_activePath.Tangent(t);
        if (tan.sqrMagnitude < 0.0001f) tan = Vector3.forward;
        return m_railDirection >= 0f ? tan : -tan;
    }

    private static Quaternion TangentRotation(Vector3 tangent)
    {
        Vector3 upHint = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.99f
            ? Vector3.forward
            : Vector3.up;
        return Quaternion.LookRotation(tangent, upHint);
    }

    private Quaternion AlignedRotation(Vector3 tangent3D)
    {
        Vector3 faceDir = Vector3.Dot(transform.forward, tangent3D) >= 0f
            ? tangent3D : -tangent3D;
        return TangentRotation(faceDir);
    }

    // --- Portal ---

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        base.Teleport(fromPortal, toPortal, pos, rot);

        // Hard-set the rigidbody pose so the next physics step does not interpolate
        // across the world from the pre-portal position.
        m_rb.position = pos;
        m_rb.rotation = rot;

        // Reorient velocity through the portal pair (only relevant when dynamic).
        if (!m_rb.isKinematic)
        {
            m_rb.linearVelocity = toPortal.TransformVector(fromPortal.InverseTransformVector(m_rb.linearVelocity));
            m_rb.angularVelocity = toPortal.TransformVector(fromPortal.InverseTransformVector(m_rb.angularVelocity));
        }

        Physics.SyncTransforms();

        // The path we just left is no longer geographically relevant; clear cooldown
        // so we can immediately attach to a path on the other side.
        BezierPath oldPath = m_activePath != null ? m_activePath : m_exitedPath;
        m_exitedPath = null;
        m_exitCooldownTimer = 0f;

        BezierPath newPath;
        float newT;
        bool useExplicitLink = oldPath != null && oldPath.linkedPath != null;

        // Prefer an explicit link if the user has set one - the linked path's connecting
        // endpoint is positioned via the portal's pathAttachPoint, so this is deterministic.
        // Fall back to the geometric search for unlinked paths.
        if (useExplicitLink)
        {
            newPath = oldPath.linkedPath;

            // Use the closest-point solver to keep the boat near its post-portal position
            // even when the path curves or the drift exceeds the path length.
            newPath.ClosestPoint(pos, out newT);
            newT = Mathf.Clamp01(newT);
        }
        else
        {
            newPath = PickPostPortalPath(pos, toPortal, oldPath, out newT);
        }

        if (newPath == null)
        {
            if (m_rb.isKinematic) m_rb.isKinematic = false;
            m_activePath = null;
            m_state = RailState.Free;
            return;
        }

        float speedMagnitude = m_state == RailState.OnRail || m_state == RailState.Snapping
            ? Mathf.Abs(m_railCurrentSpeed)
            : m_rb.linearVelocity.magnitude;

        Vector3 tangent = newPath.Tangent(newT);
        Vector3 boatForward = rot * Vector3.forward;
        m_railDirection = Vector3.Dot(boatForward, tangent) >= 0f ? 1f : -1f;
        m_railCurrentSpeed = speedMagnitude * m_railDirection;

        if (!m_rb.isKinematic) m_rb.isKinematic = true;

        m_activePath = newPath;
        m_pathT = newT;

        if (useExplicitLink)
        {
            // Alignment through the portal pair is guaranteed for linked paths, so the
            // Snapping lerp is unnecessary and would visibly drag the boat back to the
            // endpoint. Hard-set the pose to the rail-equivalent at the drift-adjusted
            // t and resume rail-following immediately.
            Vector3 facingTan = m_railDirection >= 0f ? tangent : -tangent;
            m_rb.position = RailPosition();
            m_rb.rotation = TangentRotation(facingTan);
            Physics.SyncTransforms();
            m_state = RailState.OnRail;
        }
        else
        {
            m_state = RailState.Snapping;
        }
    }

    // Picks the path to attach to after a portal exit. Uses the closest point on each
    // path (like the regular snap logic), but rejects candidate points that land on the
    // *wrong* side of the exit portal - otherwise the boat would lerp back through the
    // portal during Snapping and ping-pong. For each path the closest t plus both
    // endpoints are tried, and the closest valid candidate wins.
    private static BezierPath PickPostPortalPath(
        Vector3 worldPos, Transform exitPortal, BezierPath exclude, out float chosenT)
    {
        chosenT = 0f;
        BezierPath best = null;
        float bestDist = float.MaxValue;

        Vector3 portalPos = exitPortal.position;
        Vector3 portalForward = exitPortal.forward;
        float sideDot = Vector3.Dot(worldPos - portalPos, portalForward);
        int boatSide = System.Math.Sign(sideDot);
        if (boatSide == 0) boatSide = 1;

        foreach (BezierPath path in BezierPathNetwork.AllPaths)
        {
            if (path == null || path == exclude || path.SegmentCount == 0) continue;

            path.ClosestPoint(worldPos, out float closestT);
            float candidateT = -1f;
            float candidateDistSq = float.MaxValue;

            // closestT first, then the two endpoints - take the closest one that's on the boat's side.
            float[] tries = { closestT, 0f, 1f };
            for (int i = 0; i < tries.Length; i++)
            {
                float t = tries[i];
                Vector3 p = path.Evaluate(t);
                int side = System.Math.Sign(Vector3.Dot(p - portalPos, portalForward));
                if (side != 0 && side != boatSide) continue;

                float distSq = (p - worldPos).sqrMagnitude;
                if (distSq < candidateDistSq)
                {
                    candidateDistSq = distSq;
                    candidateT = t;
                }
            }

            if (candidateT < 0f) continue;

            float dist = Mathf.Sqrt(candidateDistSq);
            if (dist > path.snapRadius) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = path;
                chosenT = candidateT;
            }
        }

        return best;
    }
}
