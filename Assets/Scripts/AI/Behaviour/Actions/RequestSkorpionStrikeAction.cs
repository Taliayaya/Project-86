using Gameplay.Units;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RequestSkorpionStrike", story: "[Agent] requests a Skorpion Strike on [Target]", category: "Action", id: "7185b32314ff80cfaeaf9aeb8db4a5bb")]
public partial class RequestSkorpionStrikeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Unit> Target;

    private float timePreshot = 5f;
    private bool _callbackCalled = false;
    private StrikeResponse _strikeResponse;
    protected override Status OnStart()
    {
        _callbackCalled = false;
        if (!Target.Value)
            return Status.Failure;
        // In the future, we should determine with an average position of the target and see if they're camping the area. Then, we can use a strike to push them out of cover
        Rigidbody rb = Target.Value.GetComponent<Rigidbody>();
        Vector3 strikePosition = Target.Value.transform.position;

        // we preshot a future position based on velocity and time activation
        strikePosition += rb.linearVelocity * timePreshot;
        EventManager.TriggerEvent(Constants.TypedEvents.StrikeRequest, new StrikeRequest
        {
            strikePosition = strikePosition,
            strikeCallback = Callback
        });
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!_callbackCalled)
            return Status.Running;
        return _strikeResponse switch
        {
            StrikeResponse.SUCCESS => Status.Success,
            StrikeResponse.ON_COOLDOWN => Status.Failure,
            StrikeResponse.TOO_CLOSE => Status.Failure,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override void OnEnd()
    {
    }

    private void Callback(StrikeResponse response)
    {
        Debug.Log("Strike requested: " + response);
        _callbackCalled = true;
        _strikeResponse = response;
    }
}

