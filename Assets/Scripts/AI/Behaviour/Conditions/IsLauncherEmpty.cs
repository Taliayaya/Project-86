using System;
using AI;
using Gameplay.Mecha;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Launcher Empty", story: "[RocketModule] is empty", category: "Conditions", id: "c7987ee60cce59fa8ba4c6bb48e610b5")]
public partial class IsLauncherEmpty : Condition
{
    [SerializeReference] public BlackboardVariable<RocketModule> RocketModule;

    public override bool IsTrue()
    {
        return RocketModule.Value.IsEmpty();
    }
}
