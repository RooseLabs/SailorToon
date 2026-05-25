using System.Collections.Generic;
using UnityEngine;

public static class BezierPathNetwork
{
    private static readonly List<BezierPath> s_paths = new List<BezierPath>();

    public static void Register(BezierPath path)
    {
        if (!s_paths.Contains(path))
            s_paths.Add(path);
    }

    public static void Unregister(BezierPath path)
    {
        s_paths.Remove(path);
    }

    public static BezierPath FindNearest(
        Vector3 worldPos,
        BezierPath exclude,
        out float closestT)
    {
        closestT = 0f;

        BezierPath best = null;
        float bestDist = float.MaxValue;
        float bestT = 0f;

        foreach (BezierPath path in s_paths)
        {
            if (path == null || path == exclude) continue;

            path.ClosestPoint(worldPos, out float t);
            Vector3 closest = path.Evaluate(t);
            float dist = Vector3.Distance(worldPos, closest);

            if (dist <= path.snapRadius && dist < bestDist)
            {
                bestDist = dist;
                bestT = t;
                best = path;
            }
        }

        closestT = bestT;
        return best;
    }

    public static IReadOnlyList<BezierPath> AllPaths => s_paths;
}