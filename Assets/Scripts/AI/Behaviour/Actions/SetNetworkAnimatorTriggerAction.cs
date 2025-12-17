using System;
using Unity.Behavior;
using Unity.Netcode.Components;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Network Animator Trigger", story: "Set animation trigger [Trigger] in [NetworkAnimator] to: [TriggerState]", category: "Action/Animation", id: "5c40ebb1f556b6648caf0ab0662a3b3e")]
public partial class SetNetworkAnimatorTriggerAction : Action
{
    [SerializeReference] public BlackboardVariable<string> Trigger;
    [SerializeReference] public BlackboardVariable<NetworkAnimator> NetworkAnimator;
    [SerializeReference] public BlackboardVariable<bool> TriggerState;

    protected override Status OnStart()
    {
        if (NetworkAnimator.Value == null)
        {
            LogFailure("No Animator set.");
            return Status.Failure;
        }

        if (TriggerState.Value)
        {
            NetworkAnimator.Value.SetTrigger(Trigger.Value);
        }
        else
        {
            NetworkAnimator.Value.ResetTrigger(Trigger.Value);
        }
        
        return Status.Success;
    }
}

