using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Splines;

namespace AI
{
    public class Intersection : MonoBehaviour
    {
        [Serializable]
        public struct Branch
        {
            public int splineId;
            public string name;
            public List<string> hints;
        }
        public List<Branch> branches;
        public MorphoIntersectionChannel intersectionChannel;

        [Tooltip("The railway spline container. Used by gizmos and branch auto-detection.")]
        public SplineContainer track;
        [Tooltip("Splines passing within this distance are considered part of this junction.")]
        public float detectionRadius = 10f;

        public void SendIntersectionData()
        {
            intersectionChannel.SendEventMessage(branches);
        }

#if UNITY_EDITOR
        // Brute-force distance by sampling every ~2 m. SplineUtility.GetNearestPoint is
        // too coarse on long rail splines (its nearest point can be tens of meters off,
        // silently failing radius checks). Editor-only, so the cost doesn't matter.
        public static float DistanceToSpline(SplineContainer container, int splineIndex, Vector3 worldPos)
        {
            float length = container.CalculateLength(splineIndex);
            int samples = Mathf.Max(16, Mathf.CeilToInt(length / 2f));
            float best = float.MaxValue;
            for (int k = 0; k <= samples; k++)
            {
                Vector3 point = container.EvaluatePosition(splineIndex, (float)k / samples);
                best = Mathf.Min(best, Vector3.Distance(point, worldPos));
            }
            return best;
        }

        [Button("Auto-Populate Branches")]
        public void AutoPopulateBranches()
        {
            if (!track)
            {
                Debug.LogWarning($"{name}: assign the track SplineContainer first", this);
                return;
            }

            UnityEditor.Undo.RecordObject(this, "Auto-Populate Branches");
            var result = new List<Branch>();
            var report = new System.Text.StringBuilder($"{name}: distances to each spline — ");
            for (int i = 0; i < track.Splines.Count; i++)
            {
                float distance = DistanceToSpline(track, i, transform.position);
                bool inRange = distance <= detectionRadius;
                report.Append($"spline {i}: {distance:F1} m{(inRange ? " ✔" : "")}  ");
                if (!inRange)
                    continue;

                // keep authored names/hints of branches we already knew about
                int existing = branches?.FindIndex(b => b.splineId == i) ?? -1;
                result.Add(existing >= 0
                    ? branches[existing]
                    : new Branch { splineId = i, name = $"Spline {i}", hints = new List<string>() });
            }
            branches = result;
            Debug.Log($"{report} → {result.Count} branch(es) within {detectionRadius} m", this);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            if (!track || branches == null)
                return;

            for (int i = 0; i < branches.Count; i++)
            {
                if (branches[i].splineId < 0 || branches[i].splineId >= track.Splines.Count)
                {
                    Gizmos.color = Color.red; // invalid spline id — fix or re-run auto-populate
                    Gizmos.DrawWireSphere(transform.position, detectionRadius * 0.5f);
                    continue;
                }

                Gizmos.color = Color.HSVToRGB((float)i / Mathf.Max(branches.Count, 1), 1f, 1f);
                const int segments = 64;
                Vector3 previous = track.EvaluatePosition(branches[i].splineId, 0f);
                for (int k = 1; k <= segments; k++)
                {
                    Vector3 current = track.EvaluatePosition(branches[i].splineId, (float)k / segments);
                    Gizmos.DrawLine(previous, current);
                    previous = current;
                }
            }
        }
#endif
    }
}
