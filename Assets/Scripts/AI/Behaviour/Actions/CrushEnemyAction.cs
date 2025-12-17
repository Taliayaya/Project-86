using System;
using Unity.Behavior;
using Unity.Netcode.Components;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Crush Enemy", story: "[Lowe] crush its enemies", category: "Action", id: "a421720af300b582c6d4db9337f7dc3e")]
public partial class CrushEnemyAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Lowe;
    [SerializeReference] public BlackboardVariable<NetworkAnimator> NetworkAnimator;
    [SerializeReference] public BlackboardVariable<float> Gravity;
    [SerializeReference] public BlackboardVariable<float> JumpHeight;

    [CreateProperty] private Rigidbody _rb;
    [CreateProperty] private int _layerIndex = 0;
    [CreateProperty] private string _stateName = "Crush";

    protected override Status OnStart()
    {
        if (!_rb)
            _rb = Lowe.Value.GetComponent<Rigidbody>();
        NetworkAnimator.Value.SetTrigger("Crush");
        return Status.Success;
    }
    


    protected override Status OnUpdate()
    {
        // Wait until the animation reaches its end
        if (NetworkAnimator.Value.Animator.GetCurrentAnimatorStateInfo(_layerIndex).normalizedTime >= 1f)
            return Status.Running;
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

