using System;
using Gameplay.Units;
using Networking;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NetworkAgent", story: "Navigation of [Agent] [movement]", category: "Action", id: "08ea4e0bb0c003427390635875f0cc06")]
public partial class NetworkAgentAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<AgentMovement> Movement;
    [CreateProperty] private NetworkNavMeshAgent m_NetworkAgent;
    [CreateProperty] private AIAgent m_AiAgent;
    protected override Status OnStart()
    {
        if (!m_NetworkAgent)
        {
            m_NetworkAgent = Agent.Value.GetComponent<NetworkNavMeshAgent>();
            m_AiAgent = Agent.Value.GetComponent<AIAgent>();
        }

        switch (Movement.Value)
        {
            case AgentMovement.Stop:
                m_NetworkAgent.StopRpc();
                break;
            case AgentMovement.Resume:
                m_NetworkAgent.ResumeRpc();
                break;
            case AgentMovement.StopRotating:
                m_AiAgent.IsRotating = false;
                m_NetworkAgent.UpdateRotationRpc(false);
                break;
            case AgentMovement.ResumeRotating:
                m_NetworkAgent.UpdateRotationRpc(m_AiAgent.navmeshRotate);
                m_AiAgent.IsRotating = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return Status.Success;
    }
}

