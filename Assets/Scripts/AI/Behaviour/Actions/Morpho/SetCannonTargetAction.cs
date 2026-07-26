using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetCannonTarget", story: "[Turret] [index] aim towards [target]", category: "Morpho", id: "d802a9b9b478ae4dd097c09d0c1b2382")]
public partial class SetCannonTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<CannonController> Turret;
    [SerializeReference] public BlackboardVariable<int> Index;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> MaxAngle = new(5f);
    
    CannonController.TurretData TurretBase => Turret.Value.turrets[Index.Value];

    protected override Status OnStart()
    {
        Turret.Value.SetTurretTarget(Target.Value, Index.Value);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Turret.Value == null)
            return Status.Running;
        Vector3 directionToTarget = Target.Value.position - TurretBase.turret.position;
        float angle = Vector3.Angle(TurretBase.turret.forward, directionToTarget);
        if (angle < MaxAngle.Value)
            return Status.Success;
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

