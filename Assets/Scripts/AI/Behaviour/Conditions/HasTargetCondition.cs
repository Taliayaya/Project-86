using System;
using System.Collections.Generic;
using Gameplay.Units;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasTarget", story: "[VisibleTargets] is non empty", category: "Conditions", id: "3abdaf343a97ca0dc6822bbe7bf74b6e")]
public partial class HasTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<List<Unit>> VisibleTargets;

    public override bool IsTrue()
    {
        return VisibleTargets.Value.Count > 0;
    }
}
