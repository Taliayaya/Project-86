using System;
using Gameplay.Units;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.Behaviour.Conditions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Target Distance Condition", story: "if [Target] distance of [Self] < [Distance]", category: "Conditions", id: "845ad59d07d50e1cad3556126b876bbd")]
    public partial class TargetDistanceCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<Unit> Target;
        [SerializeReference] public BlackboardVariable<GameObject> Self;
        [SerializeReference] public BlackboardVariable<float> Distance;


        public override bool IsTrue()
        {
            float distance = Vector3.Distance(Target.Value.transform.position, Self.Value.transform.position);
            return distance < Distance.Value;
        }
    }
}

