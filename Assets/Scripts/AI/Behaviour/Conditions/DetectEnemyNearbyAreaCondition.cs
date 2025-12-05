using Gameplay.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[Condition(name: "DetectEnemyNearbyArea", story: "More enemy than ally nears [Target] in a radius [Radius] m", category: "Conditions", id: "12c389f02e3642b9ce77e38359ef5641")]
public partial class DetectEnemyNearbyAreaCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Unit> Target;
    
    [SerializeReference] public BlackboardVariable<float> Radius;

    public override bool IsTrue()
    {
        if (!Target.Value)
            return false;
        
        Rigidbody rb = Target.Value.GetComponent<Rigidbody>();
        Vector3 interestPoint = rb.transform.position + rb.linearVelocity * 5f;
        int allyCount = CountNearbyEnemies(interestPoint, Radius.Value, Factions.GetMembers(Faction.Legion));
        int enemyCount = CountNearbyEnemies(interestPoint, Radius.Value, Factions.GetMembers(Faction.Republic));
        Debug.Log($"[DetectEnemyNearbyAreaCondition] {enemyCount} > {allyCount} * 2");
        return enemyCount * 2 > allyCount;
    }

    public int CountNearbyEnemies(Vector3 point, float radius, List<Unit> units)
    {
        // SqrMagnitude is without the sqrt,
        return units.Count(unit => Vector3.SqrMagnitude(unit.transform.position - point) < radius * radius);
    }
}

