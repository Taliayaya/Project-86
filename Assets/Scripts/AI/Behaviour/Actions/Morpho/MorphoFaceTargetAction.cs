using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MorphoFaceTarget", story: "[Agent] rotates so its turret can reach [Target]", category: "Morpho", id: "8f3c2a91d45e6b07a2c91f30de84b5c1")]
public partial class tMorphoFaceTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [Tooltip("Rotate the body until the target is within this angle of body forward.")]
    [SerializeReference] public BlackboardVariable<float> MaxAngle = new(45f);
    [SerializeReference] public BlackboardVariable<float> RotationSpeed = new(30f); // deg/s, like turnAroundSpeed

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
            return Status.Failure;
        return Mathf.Approximately(NeededYaw(), 0f) ? Status.Success : Status.Running;
    }

    protected override Status OnUpdate()
    {
        float needed = NeededYaw();
        if (Mathf.Approximately(needed, 0f))
            return Status.Success;
        var t = Agent.Value.transform;
        float step = Mathf.Sign(needed) * Mathf.Min(Mathf.Abs(needed), RotationSpeed.Value * Time.deltaTime);
        t.rotation = Quaternion.AngleAxis(step, t.up) * t.rotation;
        return Status.Running;
    }

    // signed extra body yaw still required to bring the target inside the turret's yaw window.
    // No cleanup needed on resume: TrackFollowerAction re-aligns the body to the track tangent.
    private float NeededYaw()
    {
        var t = Agent.Value.transform;
        Vector3 dir = Vector3.ProjectOnPlane(Target.Value.position - t.position, t.up);
        if (dir.sqrMagnitude < 0.01f)
            return 0f;
        float yaw = Vector3.SignedAngle(t.forward, dir, t.up);
        return yaw - Mathf.Clamp(yaw, -MaxAngle.Value, MaxAngle.Value);
    }
}
