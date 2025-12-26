using Gameplay.Units;
using System;
using Gameplay.Mecha;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Reload Missile", story: "[Agent] reload missiles of [RocketModule]", category: "Action", id: "9f874224bf0dace8f931f14757fe7b2e")]
public partial class ReloadMissileAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<RocketModule> RocketModule;
    [SerializeReference] public BlackboardVariable<float> reloadTime;

    protected override Status OnStart()
    {
        if (!RocketModule.Value)
        {
            RocketModule.Value = Agent.Value.GetComponentInChildren<RocketModule>();
            if (!RocketModule.Value)
            {
                LogFailure("No Rocket Module found on Agent: " + Agent.Value.name);
                return Status.Failure;
            }
        }

        RocketModule.Value.Reload(reloadTime.Value);
        return Status.Success;
    }
}

