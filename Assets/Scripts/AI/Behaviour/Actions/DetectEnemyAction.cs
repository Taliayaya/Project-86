using System;
using System.Collections;
using System.Collections.Generic;
using AI;
using Gameplay;
using Gameplay.Units;
using ScriptableObjects.AI;
using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Detect Enemy", story: "Find visible Target close to [Agent] and mark them", category: "Action", id: "2a71e47fe609fbbd2407d837180c3ff4")]
public partial class DetectEnemyAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<AgentSO> AgentSo;
    [SerializeReference] public BlackboardVariable<List<Unit>> visibleTargets;
    [SerializeReference] public BlackboardVariable<List<string>> Layers;
    [SerializeReference] public BlackboardVariable<float> detectFrequency = new(1f);
    [SerializeReference] public BlackboardVariable<bool> reportToNetwork = new(true);
    
    [CreateProperty] List<Unit> _allEnemies = new List<Unit>();

    private int layerMask;

    private IEnumerator _coroutine;
    List<Unit> _nextVisibleTargets = new List<Unit>();
    
    Vector3 Position => Agent.Value.transform.position;

    private Faction _enemyFaction;
    
    protected override Status OnStart()
    {
        if (Agent.Value == null)
        {
            LogFailure("No agent provided.");
            return Status.Failure;
        }

        _enemyFaction = AgentSo.Value.faction == Faction.Legion ? Faction.Republic : Faction.Legion;
        _allEnemies = Factions.GetMembers(_enemyFaction);
        layerMask = LayerMask.GetMask(Layers.Value.ToArray());
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_coroutine == null)
            _coroutine = Detect();

        if (!_coroutine.MoveNext())
            _coroutine = null;
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }

    private IEnumerator Detect()
    {
        while (true)
        {
            _nextVisibleTargets.Clear();
            IEnumerator detectTargets = DetectTargets();
            while (detectTargets.MoveNext()) yield return null;

            (_nextVisibleTargets, visibleTargets.Value) = (visibleTargets.Value, _nextVisibleTargets);

            // This enumerator is pumped manually by OnUpdate, so a WaitForSeconds yield is
            // ignored (one tick only) — wait on Time.time instead so detectFrequency is
            // actually honored. Randomized ±25% to stagger raycasts across agents.
            float resumeAt = Time.time + detectFrequency.Value * UnityEngine.Random.Range(0.75f, 1.25f);
            while (Time.time < resumeAt) yield return null;
        }
    }

    IEnumerator DetectTargets()
    {
        // iterate directly: the yields make a stale copy no safer, and Count is re-checked each step
        for (int i = 0; i < _allEnemies.Count; i++)
        {
            Unit enemy = _allEnemies[i];
            if (!enemy || !enemy.gameObject)
                continue;
            if (!CanSeeTarget(enemy))
            {
                yield return null;
                continue;
            }

            if (reportToNetwork.Value)
                LegionNetwork.Instance.ReportTargetRpc(enemy.NetworkObject, TargetInfo.VisibilityStatus.Visible,
                    enemy.transform.position);

            _nextVisibleTargets.Add(enemy);
            yield return null;
        }

    }


    private bool CanSeeTarget(Unit target)
    {
        var direction = target.transform.position - Position;
        var angle = Vector3.Angle(direction, Agent.Value.transform.GetChild(0).forward);

        if (direction.magnitude > AgentSo.Value.viewDistance)
        {
            return false;
        }

        if (angle > AgentSo.Value.fieldOfViewAngle * 0.5f)
        {
            return false;
        }

        return PerformRaycast(direction, target);
    }
    
    private bool PerformRaycast(Vector3 direction, Unit target)
    {
        RaycastHit hit;
        // Debug.Log("Performing raycast " + _agentSo.viewDistance);
        if (Physics.Raycast(Position, direction.normalized, out hit, AgentSo.Value.viewDistance, layerMask))
        {
            // Debug.Log("hit " + hit.transform.name);
            bool isDirectTarget = hit.collider.gameObject == target.gameObject;
            if (isDirectTarget || (hit.rigidbody != null && hit.rigidbody.gameObject == target.gameObject))
                return true;
        }

        return false;
    }
}

