using ScriptableObjects.AI;
using System;
using System.Collections.Generic;
using Gameplay.Units;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Decide Target", story: "[Agent] decides a [Target] from VisibleTargets", category: "Action", id: "d6277a2b0e2438d1b81879e0648bb61b")]
public partial class DecideTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<List<Unit>> VisibleTargets;
    [SerializeReference] public BlackboardVariable<Unit> Target;
    [SerializeReference] public BlackboardVariable<float> angleScoreMultiplicator = new(2f);
    [SerializeReference] public BlackboardVariable<float> typeScoreMultiplicator = new(3f);
    [SerializeReference] public BlackboardVariable<float> distanceScoreMultiplicator = new (1f);
    
    [SerializeReference] public BlackboardVariable<bool> debugOn = new (false);
    
    [CreateProperty] AIAgent _aiAgent;

    protected override Status OnStart()
    {
        if (!_aiAgent)
            _aiAgent = Agent.Value.GetComponent<AIAgent>();
        float bestScore = float.MinValue;
        foreach (Unit unit in VisibleTargets.Value)
        {
            if (!unit)
                continue;
            float score = ComputeScore(unit);
            if (debugOn.Value)
                Debug.Log($"Target {unit.name} score: {score}");
            if (score > bestScore)
            {
                bestScore = score;
                Target.Value = unit;
            }
        }

        if (!Target.Value)
        {
            _aiAgent.Target = null;
            return Status.Failure;
        }

        _aiAgent.Target = new TargetInfo(Target.Value, TargetInfo.VisibilityStatus.Visible, 10f);
        return Status.Success;
    }

    /// <summary>
    /// Computes the score of a given target based on distance, angle, and priority.
    /// The score is used to determine the most suitable target.
    /// </summary>
    /// <param name="target">The target for which the score is to be calculated.</param>
    /// <returns>A float value representing the computed score for the target.</returns>
    private float ComputeScore(Unit target)
    {
        Vector3 toTarget = target.transform.position - Agent.Value.transform.position;
        float distance = toTarget.magnitude;
        float angle = Vector3.Angle(Agent.Value.transform.GetChild(0).forward, toTarget);

        // Normalize distance so closer is higher score
        float distanceScore = 1f / (distance + 0.01f);

        // Angle score: closer to forward = higher
        float angleScore = Mathf.Cos(angle * Mathf.Deg2Rad); // 1 = directly forward, 0 = 90°, -1 = behind
        
        if (debugOn.Value)
            Debug.Log($"Target {target.name} - angleScore: {angleScore} distanceScore: {distanceScore} priority: {target.Priority}");
            
        float totalScore = 
            angleScore * Mathf.Max(0, angleScoreMultiplicator.Value) 
            + distanceScore * Mathf.Max(0, distanceScoreMultiplicator.Value) 
            + target.Priority * Mathf.Max(0, typeScoreMultiplicator.Value);

        return totalScore;
    }
}

