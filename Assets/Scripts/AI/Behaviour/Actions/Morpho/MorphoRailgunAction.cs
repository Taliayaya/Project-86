using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MorphoRailgun", story: "[Morpho] fires railgun at [Target] with [Power] % power.", category: "Morpho", id: "06d686fec73975a9155de6076730a166")]
public partial class MorphoRailgunAction : Action
{
    [SerializeReference] public BlackboardVariable<BeamTrigger> Morpho;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> Power;

    protected override Status OnStart()
    {
        if (Target.Value != null && Target.Value.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            Debug.Log("Asurada morpho fire");
            Morpho.Value.ShootBeam(Target.Value.position, 1f);
            return Status.Running;
        }


        Morpho.Value.ShootBeam(Target.Value.position, Power.Value / 100f);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Morpho.Value.InProgress)
            return Status.Running;
        return Status.Success;
    }

    protected override void OnEnd()
    {
        // if interrupted prematurely, stop the beam
        if (Morpho.Value.InProgress)
            Morpho.Value.InterruptCharge();
    }
}

