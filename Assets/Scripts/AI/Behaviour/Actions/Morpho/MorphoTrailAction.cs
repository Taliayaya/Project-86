using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.VFX;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MorphoTrail", story: "[Agent] leaves a dust trail [vfx]", category: "Action/Animation", id: "b2ca6fe58fac3af10c6ac67791868761")]
public partial class MorphoTrailAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<VisualEffect> Vfx;
    [SerializeReference] public BlackboardVariable<float> CurrentSpeed;
    [SerializeReference] public BlackboardVariable<float> MaxSpeed;
    
    [SerializeReference] public BlackboardVariable<float> dustMultiplier = new(15f);
    [SerializeReference] public BlackboardVariable<float> smoothness = new(0.1f);
    [SerializeReference] public BlackboardVariable<float> durationPerSmoke = new(8);

    private float _currentEmission;
    private float _velocity;
    
    protected override Status OnStart()
    {
        if (Vfx.Value == null)
            return Status.Failure;
        Vfx.Value.Play();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        float targetEmission = CurrentSpeed.Value * dustMultiplier.Value;
        _currentEmission = Mathf.SmoothDamp(_currentEmission, targetEmission, ref _velocity, smoothness);
        Vfx.Value.SetFloat("DustIntensity", _currentEmission);
        Vfx.Value.SetFloat("DustDuration", Mathf.Lerp(3, durationPerSmoke.Value, CurrentSpeed.Value / MaxSpeed.Value));
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
        Vfx.Value.SetFloat("DustIntensity", 0f);
    }
}

