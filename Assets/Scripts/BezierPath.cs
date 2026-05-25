using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BezierPath : MonoBehaviour
{
    [Tooltip("World-space control points. Always 3N+1 for N segments.")]
    public List<Vector3> points = new List<Vector3>();

    [Tooltip("Radius within which the boat will snap to this path.")]
    public float snapRadius = 5f;

    [Tooltip("Color used for Gizmos in the Scene view.")]
    public Color gizmoColor = Color.cyan;

    [Header("Surface Snapping")]
    [Tooltip("Layer(s) to raycast against when snapping points to the surface (e.g. your Water layer).")]
    public LayerMask surfaceLayer;

    [Tooltip("Height above each point from which the surface raycast is fired.")]
    public float raycastOriginHeight = 50f;

    [Tooltip("Vertical offset applied on top of the surface hit point.")]
    public float surfaceYOffset = 0f;

    // ── In-game LineRenderer ──────────────────────────────────────────────────

    [Header("In-Game Path Visualisation")]
    [Tooltip("Show the path curve as a line in the running game.")]
    public bool showInGame = true;

    [Tooltip("Number of line segments used to approximate each Bézier segment in-game.")]
    [Range(4, 60)]
    public int lineStepsPerSegment = 20;

    [Tooltip("Width of the in-game path line.")]
    public float lineWidth = 0.25f;

    [Tooltip("Material used for the in-game line. Leave null to use a plain colour.")]
    public Material lineMaterial;

    [Tooltip("Colour of the in-game path line (used when no material is assigned).")]
    public Color lineColor = new Color(0f, 1f, 1f, 0.8f);

    [Tooltip("Height offset applied to every LineRenderer point so the line floats above the water.")]
    public float lineHeightOffset = 0.3f;

    private LineRenderer m_lineRenderer;

    // ── Arc-length LUT ────────────────────────────────────────────────────────

    private const int SamplesPerSegment = 40;

    private float[] m_arcLengths;
    private float m_totalLength;
    private bool m_lutDirty = true;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        BezierPathNetwork.Register(this);
        m_lutDirty = true;
        SetupLineRenderer();
        RefreshLine();
    }

    private void OnDisable()
    {
        BezierPathNetwork.Unregister(this);
    }

    private void OnValidate()
    {
        m_lutDirty = true;
        // Defer the line refresh so it runs after Unity finishes its own validation pass
        // (calling GetComponent during OnValidate can be problematic in edit mode)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            SetupLineRenderer();
            RefreshLine();
        };
#endif
    }

    private void Reset()
    {
        Vector3 origin = transform.position;
        points = new List<Vector3>
        {
            origin + new Vector3(-10f, 0f,   0f),
            origin + new Vector3( -5f, 0f,   5f),
            origin + new Vector3(  5f, 0f,  -5f),
            origin + new Vector3( 10f, 0f,   0f),
        };
        SnapAllPointsToSurface();
        m_lutDirty = true;
    }

    // ── LineRenderer ──────────────────────────────────────────────────────────

    private void SetupLineRenderer()
    {
        if (m_lineRenderer == null)
            m_lineRenderer = GetComponent<LineRenderer>();

        if (m_lineRenderer == null)
            m_lineRenderer = gameObject.AddComponent<LineRenderer>();

        m_lineRenderer.useWorldSpace = true;
        m_lineRenderer.loop = false;
        m_lineRenderer.startWidth = lineWidth;
        m_lineRenderer.endWidth = lineWidth;
        m_lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_lineRenderer.receiveShadows = false;

        if (lineMaterial != null)
        {
            m_lineRenderer.material = lineMaterial;
        }
        else
        {
            // Fallback: create a simple unlit material from the built-in shader
            if (m_lineRenderer.material == null
                || m_lineRenderer.material.shader.name != "Sprites/Default")
            {
                m_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            m_lineRenderer.startColor = lineColor;
            m_lineRenderer.endColor = lineColor;
        }
    }

    /// <summary>Rebuilds the LineRenderer point list from the current control points.</summary>
    public void RefreshLine()
    {
        if (m_lineRenderer == null) return;
        m_lineRenderer.enabled = showInGame;

        int segCount = SegmentCount;
        if (segCount == 0)
        {
            m_lineRenderer.positionCount = 0;
            return;
        }

        int totalPoints = segCount * lineStepsPerSegment + 1;
        m_lineRenderer.positionCount = totalPoints;

        for (int i = 0; i < totalPoints; i++)
        {
            float t = (float)i / (totalPoints - 1);
            Vector3 p = Evaluate(t) + Vector3.up * lineHeightOffset;
            m_lineRenderer.SetPosition(i, p);
        }
    }

    // ─── Path API ─────────────────────────────────────────────────────────────

    public int SegmentCount => Mathf.Max(0, (points.Count - 1) / 3);

    public void GetSegmentPoints(int segIndex,
        out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
    {
        int i = segIndex * 3;
        p0 = points[i];
        p1 = points[i + 1];
        p2 = points[i + 2];
        p3 = points[i + 3];
    }

    public Vector3 Evaluate(float t)
    {
        t = Mathf.Clamp01(t);
        int segCount = SegmentCount;
        if (segCount == 0) return transform.position;

        float scaledT = t * segCount;
        int seg = Mathf.Min((int)scaledT, segCount - 1);
        float localT = scaledT - seg;

        GetSegmentPoints(seg, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);
        return CubicBezier(p0, p1, p2, p3, localT);
    }

    public Vector3 Tangent(float t)
    {
        t = Mathf.Clamp01(t);
        int segCount = SegmentCount;
        if (segCount == 0) return transform.forward;

        float scaledT = t * segCount;
        int seg = Mathf.Min((int)scaledT, segCount - 1);
        float localT = scaledT - seg;

        GetSegmentPoints(seg, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);
        Vector3 d = CubicBezierDerivative(p0, p1, p2, p3, localT);
        return d.sqrMagnitude < 0.0001f ? Vector3.forward : d.normalized;
    }

    // ── Arc-length LUT ────────────────────────────────────────────────────────

    private void RebuildLUT()
    {
        int segCount = SegmentCount;
        int totalSamples = segCount * SamplesPerSegment + 1;
        m_arcLengths = new float[totalSamples];

        float cumulative = 0f;
        Vector3 prev = Evaluate(0f);
        m_arcLengths[0] = 0f;

        for (int i = 1; i < totalSamples; i++)
        {
            float t = (float)i / (totalSamples - 1);
            Vector3 curr = Evaluate(t);
            cumulative += Vector3.Distance(prev, curr);
            m_arcLengths[i] = cumulative;
            prev = curr;
        }

        m_totalLength = cumulative;
        m_lutDirty = false;
    }

    public float TotalLength
    {
        get
        {
            if (m_lutDirty) RebuildLUT();
            return m_totalLength;
        }
    }

    public float DistanceToT(float distance)
    {
        if (m_lutDirty) RebuildLUT();
        if (m_totalLength < 0.0001f) return 0f;

        distance = Mathf.Clamp(distance, 0f, m_totalLength);
        int lo = 0, hi = m_arcLengths.Length - 1;

        while (lo < hi - 1)
        {
            int mid = (lo + hi) / 2;
            if (m_arcLengths[mid] < distance) lo = mid;
            else hi = mid;
        }

        float segLen = m_arcLengths[hi] - m_arcLengths[lo];
        float frac = segLen < 0.0001f ? 0f
            : (distance - m_arcLengths[lo]) / segLen;

        float tLo = (float)lo / (m_arcLengths.Length - 1);
        float tHi = (float)hi / (m_arcLengths.Length - 1);
        return Mathf.Lerp(tLo, tHi, frac);
    }

    public Vector3 ClosestPoint(Vector3 worldPos, out float closestT)
    {
        if (m_lutDirty) RebuildLUT();

        int segCount = SegmentCount;
        int coarseSamples = segCount * SamplesPerSegment;

        closestT = 0f;
        float minDist = float.MaxValue;

        for (int i = 0; i <= coarseSamples; i++)
        {
            float t = (float)i / coarseSamples;
            float d = Vector3.SqrMagnitude(Evaluate(t) - worldPos);
            if (d < minDist)
            {
                minDist = d;
                closestT = t;
            }
        }

        float step = 1f / coarseSamples;
        for (int iter = 0; iter < 8; iter++)
        {
            step *= 0.5f;
            float tA = Mathf.Clamp01(closestT - step);
            float tB = Mathf.Clamp01(closestT + step);

            float dA = Vector3.SqrMagnitude(Evaluate(tA) - worldPos);
            float dB = Vector3.SqrMagnitude(Evaluate(tB) - worldPos);

            if (dA < dB) closestT = tA;
            else closestT = tB;
        }

        return Evaluate(closestT);
    }

    public float DistanceTo(Vector3 worldPos)
    {
        ClosestPoint(worldPos, out _);
        return Vector3.Distance(ClosestPoint(worldPos, out _), worldPos);
    }

    // ── Surface snapping ─────────────────────────────────────────────────────

    public Vector3 SnapPointToSurface(Vector3 point)
    {
        if (surfaceLayer == 0) return point;

        Vector3 origin = point + Vector3.up * raycastOriginHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
            raycastOriginHeight * 2f, surfaceLayer, QueryTriggerInteraction.Collide))
        {
            return hit.point + Vector3.up * surfaceYOffset;
        }
        return point;
    }

    public void SnapAllPointsToSurface()
    {
        for (int i = 0; i < points.Count; i++)
            points[i] = SnapPointToSurface(points[i]);
        m_lutDirty = true;
    }

    // ── Segment helpers ───────────────────────────────────────────────────────

    public void AddSegment()
    {
        if (points.Count < 4) { Reset(); return; }

        Vector3 lastPoint = points[points.Count - 1];
        Vector3 lastTan = (points[points.Count - 1] - points[points.Count - 2]).normalized;

        points.Add(SnapPointToSurface(lastPoint + lastTan * 5f));
        points.Add(SnapPointToSurface(lastPoint + lastTan * 10f));
        points.Add(SnapPointToSurface(lastPoint + lastTan * 15f));
        m_lutDirty = true;
        RefreshLine();
    }

    public void RemoveLastSegment()
    {
        if (SegmentCount <= 1) return;
        points.RemoveRange(points.Count - 3, 3);
        m_lutDirty = true;
        RefreshLine();
    }

    // ── Math ─────────────────────────────────────────────────────────────────

    private static Vector3 CubicBezier(
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t2 = t * t;
        float t3 = t2 * t;
        return u3 * p0 + 3f * u2 * t * p1 + 3f * u * t2 * p2 + t3 * p3;
    }

    private static Vector3 CubicBezierDerivative(
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return 3f * u * u * (p1 - p0)
             + 6f * u * t * (p2 - p1)
             + 3f * t * t * (p3 - p2);
    }

    // ── Editor gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos() => DrawPath(false);
    private void OnDrawGizmosSelected() => DrawPath(true);

    private void DrawPath(bool selected)
    {
        if (points == null || SegmentCount == 0) return;

        Gizmos.color = selected ? Color.white : gizmoColor;

        int segCount = SegmentCount;
        for (int s = 0; s < segCount; s++)
        {
            GetSegmentPoints(s, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);

            Vector3 prev = p0;
            int steps = 20;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 curr = CubicBezier(p0, p1, p2, p3, t);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }

            if (selected)
            {
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.7f);
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p3, p2);
                Gizmos.color = gizmoColor;
            }
        }

        if (selected)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
            DrawWireDisc(points[0], snapRadius);
            DrawWireDisc(points[points.Count - 1], snapRadius);
        }
    }

    private static void DrawWireDisc(Vector3 centre, float radius)
    {
        int steps = 32;
        Vector3 prev = centre + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= steps; i++)
        {
            float angle = (float)i / steps * Mathf.PI * 2f;
            Vector3 curr = centre + new Vector3(
                Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
    }
#endif
}

// ─────────────────────────────────────────────────────────────────────────────
// Custom editor
// ─────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
[CustomEditor(typeof(BezierPath))]
public class BezierPathEditor : Editor
{
    private BezierPath Path => (BezierPath)target;

    private void OnSceneGUI()
    {
        if (Path.points == null || Path.points.Count == 0) return;

        EditorGUI.BeginChangeCheck();

        for (int i = 0; i < Path.points.Count; i++)
        {
            bool isAnchor = (i % 3 == 0);
            float handleSize = HandleUtility.GetHandleSize(Path.points[i])
                             * (isAnchor ? 0.12f : 0.08f);

            Handles.color = isAnchor
                ? Color.white
                : new Color(1f, 0.6f, 0f, 0.9f);

            Vector3 newPos = Handles.FreeMoveHandle(
                Path.points[i],
                handleSize,
                Vector3.zero,
                Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(Path, "Move Bézier Point");
                Vector3 delta = newPos - Path.points[i];

                newPos = Path.SnapPointToSurface(newPos);
                Path.points[i] = newPos;

                if (isAnchor)
                {
                    if (i - 1 >= 0)
                        Path.points[i - 1] = Path.SnapPointToSurface(Path.points[i - 1] + delta);
                    if (i + 1 < Path.points.Count)
                        Path.points[i + 1] = Path.SnapPointToSurface(Path.points[i + 1] + delta);
                }
                else
                {
                    int anchorIndex = (i % 3 == 1) ? i - 1 : i + 1;
                    int mirrorIndex = (i % 3 == 1) ? i - 2 : i + 2;

                    if (mirrorIndex >= 0 && mirrorIndex < Path.points.Count
                        && anchorIndex >= 0 && anchorIndex < Path.points.Count)
                    {
                        Vector3 anchor = Path.points[anchorIndex];
                        Path.points[mirrorIndex] =
                            Path.SnapPointToSurface(anchor - (newPos - anchor));
                    }
                }

                Path.RefreshLine();   // keep in-game line in sync while editing
                EditorUtility.SetDirty(Path);
                EditorGUI.BeginChangeCheck();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Segment"))
        {
            Undo.RecordObject(Path, "Add Bézier Segment");
            Path.AddSegment();
            EditorUtility.SetDirty(Path);
        }

        if (GUILayout.Button("Remove Last Segment"))
        {
            Undo.RecordObject(Path, "Remove Bézier Segment");
            Path.RemoveLastSegment();
            EditorUtility.SetDirty(Path);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Snap All Points to Surface"))
        {
            Undo.RecordObject(Path, "Snap All Points to Surface");
            Path.SnapAllPointsToSurface();
            EditorUtility.SetDirty(Path);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh In-Game Line"))
        {
            Path.RefreshLine();
            EditorUtility.SetDirty(Path);
        }
    }
}
#endif