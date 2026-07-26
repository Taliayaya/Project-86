using System.Collections.Generic;
using AI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using Utility;

/// <summary>
/// Scans a railway SplineContainer for junctions (spline endpoints touching another
/// spline) and creates the Intersection trigger GameObjects the Morpho needs.
/// Re-runnable: junctions that already have an Intersection nearby are skipped.
/// X-crossings (two splines crossing mid-spline with no shared endpoint) are not
/// detected — place those by hand.
/// </summary>
public static class MorphoIntersectionBuilder
{
    private const float EndpointTolerance = 6f; // endpoint this close to another spline = junction
    private const float MergeRadius = 15f;      // junction points closer than this are one junction
    private const float TriggerRadius = 12f;

    [MenuItem("Tools/Morpho/Build Intersections From Selected Track")]
    public static void Build()
    {
        var container = Selection.activeGameObject
            ? Selection.activeGameObject.GetComponentInChildren<SplineContainer>()
            : null;
        if (!container)
        {
            EditorUtility.DisplayDialog("Build Intersections",
                "Select the railway SplineContainer GameObject first.", "OK");
            return;
        }

        var channel = FindChannel();
        if (!channel)
            Debug.LogWarning("No MorphoIntersectionChannel asset found — assign the channel on the created intersections manually.");

        var junctions = FindJunctions(container);
        var existing = Object.FindObjectsByType<Intersection>(FindObjectsSortMode.None);

        var parent = GameObject.Find("Morpho Intersections");
        if (!parent)
        {
            parent = new GameObject("Morpho Intersections");
            Undo.RegisterCreatedObjectUndo(parent, "Build Intersections");
        }

        int created = 0;
        foreach (var point in junctions)
        {
            if (HasIntersectionNear(existing, point))
                continue;

            var go = new GameObject($"Intersection {++created}");
            Undo.RegisterCreatedObjectUndo(go, "Build Intersections");
            go.transform.SetParent(parent.transform);
            go.transform.position = point;

            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = TriggerRadius;

            var intersection = go.AddComponent<Intersection>();
            intersection.track = container;
            intersection.detectionRadius = TriggerRadius;
            intersection.intersectionChannel = channel;
            intersection.AutoPopulateBranches();

            var colliderEvent = go.AddComponent<ColliderEvent>();
            colliderEvent.tagFilter = "Morpho";
            UnityEventTools.AddVoidPersistentListener(colliderEvent.onTriggerEnter, intersection.SendIntersectionData);

            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        Debug.Log($"Morpho intersections: {junctions.Count} junction(s) found, {created} created, " +
                  $"{junctions.Count - created} already covered by an existing Intersection.");
    }

    private static List<Vector3> FindJunctions(SplineContainer container)
    {
        var points = new List<Vector3>();
        var splines = container.Splines;
        for (int i = 0; i < splines.Count; i++)
        {
            for (int end = 0; end <= 1; end++)
            {
                Vector3 endpoint = container.EvaluatePosition(i, end);
                for (int j = 0; j < splines.Count; j++)
                {
                    if (j == i)
                        continue;
                    if (Intersection.DistanceToSpline(container, j, endpoint) <= EndpointTolerance)
                    {
                        AddMerged(points, endpoint);
                        break;
                    }
                }
            }
        }
        return points;
    }

    private static void AddMerged(List<Vector3> points, Vector3 point)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (Vector3.Distance(points[i], point) < MergeRadius)
            {
                points[i] = (points[i] + point) * 0.5f;
                return;
            }
        }
        points.Add(point);
    }

    private static bool HasIntersectionNear(Intersection[] existing, Vector3 point)
    {
        foreach (var intersection in existing)
            if (Vector3.Distance(intersection.transform.position, point) < MergeRadius)
                return true;
        return false;
    }

    private static MorphoIntersectionChannel FindChannel()
    {
        var guids = AssetDatabase.FindAssets("t:MorphoIntersectionChannel");
        return guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<MorphoIntersectionChannel>(AssetDatabase.GUIDToAssetPath(guids[0]))
            : null;
    }
}
