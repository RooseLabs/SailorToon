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
///   spawnPoints  — stored in LOCAL space of this GameObject's transform.
///   The mesh vertices are also in local space (no conversion needed in RebuildMesh).
///   PaintAt converts world-space hit points to local space before storing.
///   EraseAt and TooClose work in local space too.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrassPainter : MonoBehaviour
{
    [Header("Target Surface")]
    [Tooltip("The mesh collider (or any GameObject with a Collider) to paint on.")]
    public Collider targetSurface;

    [Header("Brush Settings")]
    [Tooltip("Radius of the paint brush in world units.")]
    [Range(0.1f, 20f)]
    public float brushRadius = 1.5f;

    [Tooltip("How many blade spawn points are added per brush stroke tick.")]
    [Range(1, 50)]
    public int bladesPerStroke = 8;

    [Tooltip("Minimum distance between any two blade roots (avoids clumping). Set to 0 to disable.")]
    [Range(0f, 1f)]
    public float minSpacing = 0f;

    [Header("Erase Settings")]
    [Tooltip("Radius used when erasing (hold Shift while painting).")]
    [Range(0.1f, 20f)]
    public float eraseRadius = 2f;

    [Header("State")]
    [Tooltip("Toggle to enter / exit painting mode.")]
    public bool paintingMode = false;

    // ── Serialised point list (LOCAL space) ───────────────────────────────────
    // Stored in local space so RebuildMesh never needs to re-transform them.
    // This survives domain reloads and scene saves correctly.
    [HideInInspector]
    public List<Vector3> spawnPoints = new List<Vector3>();

    // ── Unity callbacks ───────────────────────────────────────────────────────

    // OnEnable fires in both play mode AND edit mode (after domain reload,
    // scene load, or component enable). This is how the mesh gets restored
    // automatically every time Unity opens the scene.
    private void OnEnable()
    {
        RebuildMesh();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuild the point-cloud mesh from the current spawnPoints list.
    /// spawnPoints are already in local space — no transform conversion needed.
    /// </summary>
    public void RebuildMesh()
    {
        int count = spawnPoints.Count;

        Mesh mesh = new Mesh();
        mesh.name = "GrassPaintedCloud";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        if (count == 0)
        {
            GetComponent<MeshFilter>().sharedMesh = mesh;
            return;
        }

        // spawnPoints are already local-space — copy directly, no InverseTransformPoint
        Vector3[] verts = new Vector3[count];
        int[] indices = new int[count];

        for (int i = 0; i < count; i++)
        {
            verts[i] = spawnPoints[i];
            indices[i] = i;
        }

        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    /// <summary>Remove all painted points and clear the mesh.</summary>
    public void ClearAll()
    {
        spawnPoints.Clear();
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
    private struct BrushHit { public Vector3 point; public Vector3 normal; public bool valid; }

    private BrushHit _lastHit;
    private bool _isPainting;
    private const float StrokeCooldown = 0.05f;
    private double _lastStrokeTime;

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
                painter.paintingMode ? "🖌  Painting Mode  ON  (click to disable)"
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
        {
            if (EditorUtility.DisplayDialog("Clear Grass",
                    "Remove all painted grass points?", "Yes", "Cancel"))
            {
                Undo.RecordObject(painter, "Clear Grass");
                painter.ClearAll();
                MarkDirty(painter);
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            $"Blade count: {painter.spawnPoints.Count}\n\n" +
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

        _lastHit = RaycastSurface(painter.targetSurface, e.mousePosition);

        // ── Draw brush disc ───────────────────────────────────────────────
        if (_lastHit.valid)
        {
            bool erasing = e.shift;
            float radius = erasing ? painter.eraseRadius : painter.brushRadius;
            Color discColor = erasing
                ? new Color(1f, 0.3f, 0.3f, 0.8f)
                : new Color(0.3f, 1f, 0.3f, 0.8f);

            Handles.color = discColor;
            Handles.DrawWireDisc(_lastHit.point, _lastHit.normal, radius);
            Handles.color = new Color(discColor.r, discColor.g, discColor.b, 0.08f);
            Handles.DrawSolidDisc(_lastHit.point, _lastHit.normal, radius);
        }

        // ── Mouse input ───────────────────────────────────────────────────
        // IMPORTANT: e.Use() and stroke logic must only run on actual mouse
        // events. Layout and Repaint fire every frame and must never be consumed.
        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                _isPainting = true;
                GUIUtility.hotControl = controlID;
                // Fire one stroke immediately on click (don't wait for cooldown)
                _lastStrokeTime = -StrokeCooldown;
                DoStroke(painter, e);
                e.Use();
                break;

            case EventType.MouseUp when e.button == 0:
                _isPainting = false;
                GUIUtility.hotControl = 0;
                e.Use();
                break;

            case EventType.MouseDrag when e.button == 0 && _isPainting && _lastHit.valid:
                DoStroke(painter, e);
                e.Use();
                break;

            case EventType.MouseMove:
                SceneView.RepaintAll();
                break;
        }

        // Keep default scene-view controls suppressed while painting
        if (_isPainting)
            HandleUtility.AddDefaultControl(controlID);
    }

    // ── Stroke dispatcher (cooldown lives here, not in OnSceneGUI) ───────────
    private void DoStroke(GrassPainter painter, Event e)
    {
        if (!_lastHit.valid) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - _lastStrokeTime < StrokeCooldown) return;
        _lastStrokeTime = now;

        Undo.RecordObject(painter, e.shift ? "Erase Grass" : "Paint Grass");

        if (e.shift)
            EraseAt(painter, _lastHit.point);
        else
            PaintAt(painter, _lastHit.point, _lastHit.normal);

        painter.RebuildMesh();
        MarkDirty(painter);
        SceneView.RepaintAll();
    }

    // ── Paint ─────────────────────────────────────────────────────────────────
    private void PaintAt(GrassPainter painter, Vector3 centre, Vector3 normal)
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
            Vector2 rnd = Random.insideUnitCircle * painter.brushRadius;

            // Build a tangent frame aligned to the surface normal
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(normal, Vector3.forward);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

            Vector3 candidate = centre + tangent * rnd.x + bitangent * rnd.y;

            // Snap onto the actual surface geometry
            Vector3 snappedWorld;
            if (!SnapToSurface(painter.targetSurface, candidate, normal, out snappedWorld))
                continue;

            // Convert to local space once, at paint time
            Vector3 snappedLocal = painter.transform.InverseTransformPoint(snappedWorld);

            // Spacing check (skipped entirely when minSpacing == 0)
            if (useSpacing && TooClose(painter.spawnPoints, snappedLocal, painter.minSpacing))
                continue;

            painter.spawnPoints.Add(snappedLocal);
            added++;
        }
    }

    // ── Erase ─────────────────────────────────────────────────────────────────
    private void EraseAt(GrassPainter painter, Vector3 worldCentre)
    {
        // Convert erase centre to local space to match stored points
        Vector3 localCentre = painter.transform.InverseTransformPoint(worldCentre);

        // Scale the erase radius by the inverse of the object's lossy scale
        // so it behaves correctly even if the painter object is scaled
        float localRadius = painter.eraseRadius / painter.transform.lossyScale.x;
        float r2 = localRadius * localRadius;

        painter.spawnPoints.RemoveAll(p => (p - localCentre).sqrMagnitude <= r2);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private BrushHit RaycastSurface(Collider col, Vector2 mousePos)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        RaycastHit hit;
        if (col.Raycast(ray, out hit, 1000f))
            return new BrushHit { point = hit.point, normal = hit.normal, valid = true };
        return new BrushHit { valid = false };
    }

    private bool SnapToSurface(Collider col, Vector3 candidate, Vector3 normal, out Vector3 result)
    {
        // First try: cast along the surface normal
        Vector3 origin = candidate + normal * 0.5f;
        Ray ray = new Ray(origin, -normal);
        RaycastHit hit;
        if (col.Raycast(ray, out hit, 2f))
        {
            result = hit.point;
            return true;
        }
        // Fallback: cast straight down
        ray = new Ray(candidate + Vector3.up * 0.5f, Vector3.down);
        if (col.Raycast(ray, out hit, 2f))
        {
            result = hit.point;
            return true;
        }
        result = candidate;
        return false;
    }

    private bool TooClose(List<Vector3> points, Vector3 candidateLocal, float minDist)
    {
        float md2 = minDist * minDist;
        for (int i = 0; i < points.Count; i++)
            if ((points[i] - candidateLocal).sqrMagnitude < md2) return true;
        return false;
    }

    private void MarkDirty(GrassPainter painter)
    {
        EditorUtility.SetDirty(painter);
        EditorSceneManager.MarkSceneDirty(painter.gameObject.scene);
    }
}
#endif