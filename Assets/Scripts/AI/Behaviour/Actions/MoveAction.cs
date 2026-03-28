using Gameplay.Units;
using System;
using AI;
using Networking;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move Action", story: "[Agent] starts moving towards the [Target] and stops [distance] m before", category: "Action", id: "c81533c87d841334aa013af8fbc6e789")]
public partial class MoveAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Unit> Target;
    [SerializeReference] public BlackboardVariable<float> Distance;
    [CreateProperty] private NetworkNavMeshAgent m_NetworkAgent;
    [CreateProperty] private LegionSquad m_LegionSquad;
    
    protected override Status OnStart()
    {
        if (!m_NetworkAgent)
        {
            m_NetworkAgent = Agent.Value.GetComponentInChildren<NetworkNavMeshAgent>();
            m_LegionSquad = Agent.Value.GetComponent<LegionSquad>();
        }

        if (!Target.Value)
        {
            Target.Value = null;
            return Status.Failure;
        }

        float currentDistance = Vector3.Distance(Agent.Value.transform.position, Target.Value.transform.position);

        // no needs to do anything if the target is already close enough
        if (currentDistance <= Distance.Value + 5)
            return Status.Success;
        
        // m_NetworkAgent.SetStopDistanceRpc(Distance.Value);
        m_LegionSquad.FollowLeader = false;
        m_NetworkAgent.SetDestinationRpc(Target.Value.transform.position);
        return Status.Success;
    }
}

