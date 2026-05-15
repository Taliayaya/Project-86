using System;
using AI;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SideCrystal", story: "Change crystal state to [CrystalState]", category: "Morpho", id: "11ba122b98efdd895ea57d2991ecdb63")]
public partial class SideCrystalAction : Action
{
    [SerializeReference] public BlackboardVariable<MorphoCrystal> Crystal;
    [SerializeReference] public BlackboardVariable<MorphoCrystal.Status> CrystalState;

    protected override Status OnStart()
    {
        if (Crystal.Value.GetStatus == CrystalState.Value)
            return Status.Success;
        
        switch (CrystalState.Value)
        {
            case MorphoCrystal.Status.Invincible:
                Crystal.Value.Deactivate();
                break;
            case MorphoCrystal.Status.Active:
                Crystal.Value.Activate();
                break;
            case MorphoCrystal.Status.Destroyed:
                Crystal.Value.Deactivate();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

