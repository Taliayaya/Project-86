using System;
using AI;
using Gameplay.Mecha;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Launcher Reloading", story: "[RocketModule] is reloading == [Cond]", category: "Conditions", id: "c7987ee60cce59fa8bb4c6bb48e610b5")]
public partial class IsLauncherReloading : Condition
{
    [SerializeReference] public BlackboardVariable<RocketModule> RocketModule;
    [SerializeReference] public BlackboardVariable<bool> Cond;

    public override bool IsTrue()
    {
        return RocketModule.Value.IsReloading == Cond.Value;
    }
}
