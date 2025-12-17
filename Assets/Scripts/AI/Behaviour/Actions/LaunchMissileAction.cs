using Gameplay.Units;
using System;
using Gameplay.Mecha;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Launch Missile", story: "[Agent] send missiles to [Target] as a salve [Salve]", category: "Action", id: "9f874224bf0dace8f931f14757fe7b2d")]
public partial class LaunchMissileAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Unit> Target;
    [SerializeReference] public BlackboardVariable<bool> Salve;
    [SerializeReference] public BlackboardVariable<RocketModule> RocketModule;

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

        if (Salve.Value)
        {
            RocketModule.Value.LaunchSalve();
        }
        else
        {
            RocketModule.Value.LaunchNextRocket();
        }
        return Status.Success;
    }
}

