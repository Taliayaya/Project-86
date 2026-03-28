using System;
using AI;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Can Chase", story: "[Agent] can chase enemy", category: "Conditions", id: "c7987ee60cce59fa8ba4c6bb48e600b4")]
public partial class CanChaseCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    private LegionSquad _squad;

    public override bool IsTrue()
    {
        return _squad.CanChase();
    }

    public override void OnStart()
    {
        if (!_squad)
            _squad = Agent.Value.GetComponent<LegionSquad>();
    }

    public override void OnEnd()
    {
    }
}
