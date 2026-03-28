using System;
using System.Collections.Generic;
using AI;
using Gameplay.Units;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsSquadLeader", story: "[Agent] is a squad leader", category: "Conditions", id: "3abdaf343a97ca0dc6822bbe7bf74b6f")]
public partial class IsSquadLeaderCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    private LegionSquad _squad;
    public override bool IsTrue()
    {
        if (!_squad)
            _squad = Agent.Value.GetComponent<LegionSquad>();
        return _squad.IsLeader;
    }
}
