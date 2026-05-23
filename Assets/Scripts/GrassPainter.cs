using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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

    [Tooltip("Minimum distance between any two blade roots (avoids clumping).")]
    [Range(0f, 1f)]
    public float minSpacing = 0.1f;

    [Header("Erase Settings")]
    [Tooltip("Radius used when erasing (hold Shift while painting).")]
    [Range(0.1f, 20f)]
    public float eraseRadius = 2f;

    [Header("State")]
    [Tooltip("Toggle to enter / exit painting mode.")]
    public bool paintingMode = false;

    [HideInInspector]
    public List<Vector3> spawnPoints = new List<Vector3>();

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

        Vector3[] verts = new Vector3[count];
        int[] indices = new int[count];

        for (int i = 0; i < count; i++)
        {
            verts[i] = transform.InverseTransformPoint(spawnPoints[i]);
            indices[i] = i;
        }

        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public void ClearAll()
    {
        spawnPoints.Clear();
        RebuildMesh();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GrassPainter))]
public class GrassPainterEditor : Editor
{
    private struct BrushHit { public Vector3 point; public Vector3 normal; public bool valid; }

    private BrushHit  _lastHit;
    private bool      _isPainting;
    private bool      _isErasing;
    private const float StrokeCooldown = 0.05f;
    private double      _lastStrokeTime;

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

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            _isPainting = true;
            GUIUtility.hotControl = controlID;
            e.Use();
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            _isPainting = false;
            GUIUtility.hotControl = 0;
            e.Use();
        }

        if ((_isPainting || e.type == EventType.MouseDrag) && _lastHit.valid)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastStrokeTime >= StrokeCooldown)
            {
                _lastStrokeTime = now;

                Undo.RecordObject(painter, e.shift ? "Erase Grass" : "Paint Grass");

                if (e.shift)
                    EraseAt(painter, _lastHit.point);
                else
                    PaintAt(painter, _lastHit.point, _lastHit.normal);

                painter.RebuildMesh();
                MarkDirty(painter);
            }
            e.Use();
        }

        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            SceneView.RepaintAll();

        if (_isPainting)
            HandleUtility.AddDefaultControl(controlID);
    }

    private void PaintAt(GrassPainter painter, Vector3 centre, Vector3 normal)
    {
        int added = 0;
        int attempts = 0;
        int maxAttempts = painter.bladesPerStroke * 10;

        while (added < painter.bladesPerStroke && attempts < maxAttempts)
        {
            attempts++;

            Vector2 rnd     = Random.insideUnitCircle * painter.brushRadius;
            Vector3 offset  = new Vector3(rnd.x, 0f, rnd.y);
            Vector3 tangent  = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(normal, Vector3.forward);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

            Vector3 candidate = centre
                + tangent   * rnd.x
                + bitangent * rnd.y;

            Vector3 snapped;
            if (!SnapToSurface(painter.targetSurface, candidate, normal, out snapped))
                continue;

            if (painter.minSpacing > 0f && TooClose(painter.spawnPoints, snapped, painter.minSpacing))
                continue;

            painter.spawnPoints.Add(snapped);
            added++;
        }
    }

    private void EraseAt(GrassPainter painter, Vector3 centre)
    {
        float r2 = painter.eraseRadius * painter.eraseRadius;
        painter.spawnPoints.RemoveAll(p =>
            (p - centre).sqrMagnitude <= r2);
    }

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
        Vector3 origin = candidate + normal * 0.5f;
        Ray ray = new Ray(origin, -normal);
        RaycastHit hit;
        if (col.Raycast(ray, out hit, 2f))
        {
            result = hit.point;
            return true;
        }
        ray = new Ray(candidate + Vector3.up * 0.5f, Vector3.down);
        if (col.Raycast(ray, out hit, 2f))
        {
            result = hit.point;
            return true;
        }
        result = candidate;
        return false;
    }

    private bool TooClose(List<Vector3> points, Vector3 candidate, float minDist)
    {
        float md2 = minDist * minDist;
        int start = Mathf.Max(0, points.Count - 200);
        for (int i = start; i < points.Count; i++)
            if ((points[i] - candidate).sqrMagnitude < md2) return true;
        return false;
    }

    private void MarkDirty(GrassPainter painter)
    {
        EditorUtility.SetDirty(painter);
        EditorSceneManager.MarkSceneDirty(painter.gameObject.scene);
    }
}
#endif