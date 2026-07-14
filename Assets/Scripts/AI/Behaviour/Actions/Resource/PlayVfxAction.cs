using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.VFX;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlayVFX", story: "Play [Vfx]", category: "Action/Resource", id: "81f5ed84bb31c0c07f1387141b604786")]
public partial class PlayVfxAction : Action
{
    [SerializeReference] public BlackboardVariable<VisualEffect> Vfx;

    protected override Status OnStart()
    {
        if (!Vfx.Value)
            return Status.Failure;
        Vfx.Value.Play();
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

