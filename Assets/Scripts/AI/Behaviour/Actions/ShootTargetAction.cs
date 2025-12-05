using Gameplay.Units;
using System;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Mecha;
using ScriptableObjects.AI;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Shoot Target", story: "[Agent] starts shooting [Target] if aligned for [ShootDuration] s", category: "Action", id: "ddb25ddab5f4fe0f86879bf677d9a49b")]
public partial class ShootTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<AgentSO> AgentSo;
    [SerializeReference] public BlackboardVariable<Unit> Target;
    [SerializeReference] public BlackboardVariable<float> ShootDuration;
    [SerializeReference] public BlackboardVariable<float> AngleToShoot = new(10f);
    [SerializeReference] public BlackboardVariable<List<string>> Layers;
    
    [CreateProperty] WeaponModule[] _weaponModules;
    [CreateProperty] float _doNotShootDistanceFromTarget = 100f;
    
    [CreateProperty] private int _layerMask;

    protected override Status OnStart()
    {
        if (_weaponModules is null)
        {
            _weaponModules = Agent.Value.GetComponent<AIAgent>().WeaponModules;
            _layerMask = LayerMask.GetMask(Layers.Value.ToArray());
        }

        foreach (var weaponModule in _weaponModules)
        {
            weaponModule.StopAllCoroutines();
            if (weaponModule.HoldFire)
                weaponModule.StartCoroutine(weaponModule.ShootHoldDuringTime(ShootDuration.Value, EnemyInRange));
            else
                weaponModule.StartShootDuringTime(ShootDuration.Value, EnemyInRange);
        }
        return Status.Success;
    }
    
    // check to avoid shooting in allies
    private bool PerformRaycast(Vector3 position, Vector3 direction)
    {
        RaycastHit hit;
        
        float targetDistance = Vector3.Distance(position, Target.Value.transform.position);
        if (Physics.Raycast(position, direction.normalized, out hit, AgentSo.Value.viewDistance, _layerMask))
        {
            float errorRange = Mathf.Abs(hit.distance - targetDistance);
            Debug.Log($"{Agent.Value.name}: PerformRaycast {Target.Value.name} - hit {hit.transform.name} distance: {hit.distance} < {targetDistance} error: {errorRange}");
            if (errorRange > _doNotShootDistanceFromTarget)
                return false;
            if (hit.rigidbody)
            {
                if (hit.rigidbody.CompareTag("Enemy") && AgentSo.Value.faction == Faction.Legion)
                    return false;
            }
        }
        Debug.DrawLine(position, hit.point, Color.yellow);
        return true;
    }

    public bool EnemyInRange(Transform turret)
    {
        if (!Target.Value)
            return false;
        var direction = Target.Value.transform.position - turret.position;
        var angle = Vector3.Angle(direction, turret.forward);
        var distance = direction.magnitude;
        
        Debug.Log($"{Agent.Value.name}: EnemyInRange {Target.Value.name} - angle: {angle} < {AngleToShoot.Value * 0.5f} distance: {distance} < {AgentSo.Value.combatDistance}");

        return distance < AgentSo.Value.combatDistance
         && angle < AngleToShoot.Value * 0.5f 
         && PerformRaycast(turret.position, direction);
    }
}

