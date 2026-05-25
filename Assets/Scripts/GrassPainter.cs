using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// GrassPainter — attach to an empty GameObject.
///
/// Workflow:
///   1. Add this component to an empty GameObject (e.g. "GrassPainter_Hill").
///   2. Assign the GrassGeometry material to the MeshRenderer that gets auto-added.
///   3. In the Inspector, assign the Target Surface — any imported Blender mesh with a collider.
///   4. Enable "Painting Mode" in the Inspector, then click/drag in the Scene view.
///   5. Hold Shift to erase. Disable "Painting Mode" when done.
///   6. Use "Clear All Grass" to start over.
///
/// Data contract:
///   blades — position+normal stored in LOCAL space of this GameObject's transform.
///   The mesh vertices/normals are also in local space (no conversion in RebuildMesh).
///   PaintAt converts world-space hit data to local space before storing.
///   EraseAt and TooClose work in local space too.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrassPainter : MonoBehaviour
{
    [Serializable]
    public struct BladeData
    {
        public Vector3 position;
        public Vector3 normal;

        public BladeData(Vector3 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal;
        }
    }

    [Header("Target Surface")] [Tooltip("The mesh collider (or any GameObject with a Collider) to paint on.")]
    public Collider targetSurface;

    [Header("Brush Settings")] [Tooltip("Radius of the paint brush in world units.")] [Range(0.1f, 20f)]
    public float brushRadius = 1.5f;

    [Tooltip("How many blade spawn points are added per brush stroke tick.")] [Range(1, 50)]
    public int bladesPerStroke = 8;

    [Tooltip("Minimum distance between any two blade roots (avoids clumping). Set to 0 to disable.")] [Range(0f, 1f)]
    public float minSpacing = 0f;

    [Header("Erase Settings")] [Tooltip("Radius used when erasing (hold Shift while painting).")] [Range(0.1f, 20f)]
    public float eraseRadius = 2f;

    [Header("State")] [Tooltip("Toggle to enter / exit painting mode.")]
    public bool paintingMode = false;

    // Serialised blade list (LOCAL space). Survives domain reloads and scene saves.
    [HideInInspector] public List<BladeData> blades = new();

    // Reused mesh + scratch buffers so RebuildMesh allocates nothing in steady state.
    private Mesh m_mesh;
    private readonly List<Vector3> m_scratchVerts = new();
    private readonly List<Vector3> m_scratchNormals = new();
    private readonly List<int> m_scratchIndices = new();

    // ── Unity callbacks ───────────────────────────────────────────────────────

    // OnEnable fires in both play mode AND edit mode (after domain reload,
    // scene load, or component enable). This is how the mesh gets restored
    // automatically every time Unity opens the scene.
    private void OnEnable()
    {
        RebuildMesh();
    }

    private void OnDestroy()
    {
        if (m_mesh == null) return;
        #if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(m_mesh);
        else Destroy(m_mesh);
        #else
            Destroy(_mesh);
        #endif
        m_mesh = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuild the point-cloud mesh from the current blades list.
    /// Reuses a cached Mesh and scratch lists to avoid per-stroke allocations.
    /// </summary>
    public void RebuildMesh()
    {
        if (m_mesh == null)
        {
            m_mesh = new Mesh
            {
                name = "GrassPaintedCloud",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                hideFlags = HideFlags.DontSave
            };
        }

        m_mesh.Clear();

        int count = blades.Count;
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (count == 0)
        {
            meshFilter.sharedMesh = m_mesh;
            return;
        }

        m_scratchVerts.Clear();
        m_scratchNormals.Clear();
        m_scratchIndices.Clear();

        for (int i = 0; i < count; i++)
        {
            BladeData b = blades[i];
            m_scratchVerts.Add(b.position);
            m_scratchNormals.Add(b.normal);
            m_scratchIndices.Add(i);
        }

        m_mesh.SetVertices(m_scratchVerts);
        m_mesh.SetNormals(m_scratchNormals);
        m_mesh.SetIndices(m_scratchIndices, MeshTopology.Points, 0);
        m_mesh.RecalculateBounds();

        meshFilter.sharedMesh = m_mesh;
    }

    /// <summary>Remove all painted blades and clear the mesh.</summary>
    public void ClearAll()
    {
        blades.Clear();
        RebuildMesh();
    }
}


// ══════════════════════════════════════════════════════════════════════════════
//  Custom Editor
// ══════════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
[CustomEditor(typeof(GrassPainter))]
public class GrassPainterEditor : Editor
{
    private struct BrushHit
    {
        public Vector3 point;
        public Vector3 normal;
        public bool valid;
    }

    private BrushHit m_lastHit;
    private bool m_isPainting;
    private const float StrokeCooldown = 0.05f;
    private double m_lastStrokeTime;

    // ── Inspector GUI ─────────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        GrassPainter painter = (GrassPainter)target;

        DrawDefaultInspector();

        GUILayout.Space(6);

        GUI.backgroundColor = painter.paintingMode
            ? new Color(0.4f, 1f, 0.4f)
            : new Color(1f, 0.85f, 0.4f);

        if (GUILayout.Button(
                painter.paintingMode
                    ? "🖌  Painting Mode  ON  (click to disable)"
                    : "🖌  Enable Painting Mode",
                GUILayout.Height(34)))
        {
            painter.paintingMode = !painter.paintingMode;
            EditorUtility.SetDirty(painter);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.Space(4);

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑  Clear All Grass", GUILayout.Height(28)))
            if (painter.blades.Count > 0 &&
                EditorUtility.DisplayDialog("Clear Grass",
                    "Remove all painted grass points?", "Yes", "Cancel"))
            {
                Undo.RegisterCompleteObjectUndo(painter, "Clear Grass");
                painter.ClearAll();
                MarkDirty(painter);
            }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            $"Blade count: {painter.blades.Count}\n\n" +
            "• Enable Painting Mode, then click/drag in the Scene view to add grass.\n" +
            "• Hold  Shift  to erase.\n" +
            "• Disable Painting Mode when finished.",
            MessageType.Info);
    }

    // ── Scene GUI ─────────────────────────────────────────────────────────────
    private void OnSceneGUI()
    {
        GrassPainter painter = (GrassPainter)target;

        if (!painter.paintingMode) return;

        if (painter.targetSurface == null)
        {
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 400, 22),
                "⚠ Assign a Target Surface collider first.", EditorStyles.boldLabel);
            Handles.EndGUI();
            return;
        }

        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        m_lastHit = RaycastSurface(painter.targetSurface, e.mousePosition);

        // ── Draw brush disc ───────────────────────────────────────────────
        if (m_lastHit.valid)
        {
            bool erasing = e.shift;
            float radius = erasing ? painter.eraseRadius : painter.brushRadius;
            Color discColor = erasing
                ? new Color(1f, 0.3f, 0.3f, 0.8f)
                : new Color(0.3f, 1f, 0.3f, 0.8f);

            Handles.color = discColor;
            Handles.DrawWireDisc(m_lastHit.point, m_lastHit.normal, radius);
            Handles.color = new Color(discColor.r, discColor.g, discColor.b, 0.08f);
            Handles.DrawSolidDisc(m_lastHit.point, m_lastHit.normal, radius);
        }

        // ── Mouse input ───────────────────────────────────────────────────
        // IMPORTANT: e.Use() and stroke logic must only run on actual mouse
        // events. Layout and Repaint fire every frame and must never be consumed.
        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                m_isPainting = true;
                GUIUtility.hotControl = controlID;
                // Fire one stroke immediately on click (don't wait for cooldown)
                m_lastStrokeTime = -StrokeCooldown;
                DoStroke(painter, e);
                e.Use();
                break;

            case EventType.MouseUp when e.button == 0:
                m_isPainting = false;
                GUIUtility.hotControl = 0;
                e.Use();
                break;

            case EventType.MouseDrag when e.button == 0 && m_isPainting && m_lastHit.valid:
                DoStroke(painter, e);
                e.Use();
                break;

            case EventType.MouseMove:
                SceneView.RepaintAll();
                break;
        }

        // Keep default scene-view controls suppressed while painting
        if (m_isPainting)
            HandleUtility.AddDefaultControl(controlID);
    }

    // ── Stroke dispatcher (cooldown lives here, not in OnSceneGUI) ───────────
    // PaintAt/EraseAt record undo lazily (only when they actually mutate) and
    // return how many blades changed — we skip RebuildMesh/MarkDirty when zero
    // so dragging over empty air doesn't dirty the scene.
    private void DoStroke(GrassPainter painter, Event e)
    {
        if (!m_lastHit.valid) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - m_lastStrokeTime < StrokeCooldown) return;
        m_lastStrokeTime = now;

        int changed = e.shift
            ? EraseAt(painter, m_lastHit.point)
            : PaintAt(painter, m_lastHit.point, m_lastHit.normal);

        if (changed == 0) return;

        painter.RebuildMesh();
        MarkDirty(painter);
        SceneView.RepaintAll();
    }

    // ── Paint ─────────────────────────────────────────────────────────────────
    // Returns the number of blades actually added (0 if nothing accepted).
    private int PaintAt(GrassPainter painter, Vector3 center, Vector3 normal)
    {
        int added = 0;

        // When minSpacing is 0, every candidate that snaps to the surface is
        // accepted immediately — no rejection loop needed, so run exactly
        // bladesPerStroke iterations.
        // When minSpacing > 0, we need more attempts because some candidates
        // will be rejected for being too close to existing points.
        bool useSpacing = painter.minSpacing > 0f;
        int maxAttempts = useSpacing ? painter.bladesPerStroke * 50 : painter.bladesPerStroke;

        for (int attempts = 0; attempts < maxAttempts && added < painter.bladesPerStroke; attempts++)
        {
            Vector2 rnd = UnityEngine.Random.insideUnitCircle * painter.brushRadius;

            // Build a tangent frame aligned to the surface normal
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(normal, Vector3.forward);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

            Vector3 candidate = center + tangent * rnd.x + bitangent * rnd.y;

            // Snap onto the actual surface geometry
            if (!SnapToSurface(painter.targetSurface, candidate, normal,
                    out Vector3 snappedWorld, out Vector3 snappedNormalWorld))
                continue;

            // Convert to local space once, at paint time
            Vector3 snappedLocal = painter.transform.InverseTransformPoint(snappedWorld);
            Vector3 snappedNormalLocal = painter.transform.InverseTransformDirection(snappedNormalWorld).normalized;

            // Spacing check (skipped entirely when minSpacing == 0)
            if (useSpacing && TooClose(painter.blades, snappedLocal, painter.minSpacing))
                continue;

            // Capture pre-mutation state for undo the first time we actually add.
            if (added == 0)
                Undo.RegisterCompleteObjectUndo(painter, "Paint Grass");

            painter.blades.Add(new GrassPainter.BladeData(snappedLocal, snappedNormalLocal));
            added++;
        }

        return added;
    }

    // ── Erase ─────────────────────────────────────────────────────────────────
    // Returns the number of blades actually removed (0 if none were in range).
    private int EraseAt(GrassPainter painter, Vector3 worldCenter)
    {
        // Convert erase center to local space to match stored points
        Vector3 localCenter = painter.transform.InverseTransformPoint(worldCenter);

        // Scale the erase radius by the inverse of the object's lossy scale
        // so it behaves correctly even if the painter object is scaled
        float localRadius = painter.eraseRadius / painter.transform.lossyScale.x;
        float r2 = localRadius * localRadius;

        // Pre-check: do nothing (and skip undo capture) if no blades are in range.
        int countBefore = painter.blades.Count;
        bool anyHit = false;
        for (int i = 0; i < countBefore; i++)
        {
            if ((painter.blades[i].position - localCenter).sqrMagnitude <= r2)
            {
                anyHit = true;
                break;
            }
        }
        if (!anyHit) return 0;

        Undo.RegisterCompleteObjectUndo(painter, "Erase Grass");
        painter.blades.RemoveAll(b => (b.position - localCenter).sqrMagnitude <= r2);
        return countBefore - painter.blades.Count;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private BrushHit RaycastSurface(Collider col, Vector2 mousePos)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        if (col.Raycast(ray, out RaycastHit hit, 1000f))
            return new BrushHit { point = hit.point, normal = hit.normal, valid = true };
        return new BrushHit { valid = false };
    }

    private bool SnapToSurface(Collider col, Vector3 candidate, Vector3 normal,
        out Vector3 result, out Vector3 resultNormal)
    {
        // First try: cast along the surface normal
        Vector3 origin = candidate + normal * 0.5f;
        Ray ray = new(origin, -normal);
        if (col.Raycast(ray, out RaycastHit hit, 2f))
        {
            result = hit.point;
            resultNormal = hit.normal;
            return true;
        }

        // Fallback: cast straight down
        ray = new Ray(candidate + Vector3.up * 0.5f, Vector3.down);
        if (col.Raycast(ray, out hit, 2f))
        {
            result = hit.point;
            resultNormal = hit.normal;
            return true;
        }

        result = candidate;
        resultNormal = normal;
        return false;
    }

    private bool TooClose(List<GrassPainter.BladeData> points, Vector3 candidateLocal, float minDist)
    {
        float md2 = minDist * minDist;
        for (int i = 0; i < points.Count; i++)
            if ((points[i].position - candidateLocal).sqrMagnitude < md2)
                return true;
        return false;
    }

    private void MarkDirty(GrassPainter painter)
    {
        EditorUtility.SetDirty(painter);
        EditorSceneManager.MarkSceneDirty(painter.gameObject.scene);
    }
}
#endif
