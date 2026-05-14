using System;
using Unity.Behavior;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TrackFollower", story: "[Agent] moves along the [track] at [MaxSpeed]", category: "Action/Navigation", id: "dac50ffa996d3f563acf33f467e6475d")]
public partial class TrackFollowerAction : Action
{
    private static readonly int IsWalking = UnityEngine.Animator.StringToHash("isWalking");
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<SplineContainer> Track;
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    
    [SerializeReference] public BlackboardVariable<MorphoCruiseMode> CruiseMode;
    [SerializeReference] public BlackboardVariable<float> BrakeDuration = new(2f);
    
    [Header("Speed Settings")]
    [SerializeReference] public BlackboardVariable<float> MaxSpeed = new BlackboardVariable<float>(50);
    [SerializeReference] public BlackboardVariable<float> MinSpeed = new BlackboardVariable<float>(8);
    
    [SerializeReference] public BlackboardVariable<float> CurrentDistance;
    [SerializeReference] public BlackboardVariable<float> CurrentSpeed;
    
    [Header("Tuning")]
    [SerializeReference] public BlackboardVariable<float> walkMaxAnimationSpeed = new(2.5f);
    [SerializeReference] public BlackboardVariable<float> curveSensitivity = new(50f);
    [SerializeReference] public BlackboardVariable<float> speedSmoothTime = new(0.5f);
    [SerializeReference] public BlackboardVariable<float> lookAheadDistance = new (15f);
    
    private float _speedVelocity;
    private float _nativeLength;
    private float _currentSmoothTime;
    

    protected override Status OnStart()
    {
        if (Agent.Value == null || Track.Value == null)
            return Status.Failure;

        _nativeLength = Track.Value.CalculateLength();

        return Status.Running;
    }
    
    float AdjustSpeedBasedOnCurvature(float t)
    {
        float currentDistance = t * _nativeLength;
    
        // looking ahead to anticipate a corner
        float nextT = ((currentDistance + lookAheadDistance) % _nativeLength) / _nativeLength;

        float3 tangentA = Track.Value.EvaluateTangent(t);
        float3 tangentB = Track.Value.EvaluateTangent(nextT);

        // corner angle?
        float angle = Vector3.Angle(tangentA, tangentB);

        // Debug.Log($"Angle: {angle:F2} | Speed: {currentSpeed:F1}");

        return Mathf.Lerp(MaxSpeed.Value, MinSpeed.Value, angle * curveSensitivity);
    }

    protected override Status OnUpdate()
    {
        // speed
        float t = (float)(CurrentDistance.Value / _nativeLength) % 1f;

        float targetSpeed;
        switch (CruiseMode.Value)
        {
            case MorphoCruiseMode.Normal:
                targetSpeed = AdjustSpeedBasedOnCurvature(t);
                _currentSmoothTime = speedSmoothTime.Value;
                break;
            case MorphoCruiseMode.Braking:
                targetSpeed = 0;
                _currentSmoothTime = BrakeDuration.Value;
                // exiting this and automatically going to IS STOPPED mode
                if (CurrentSpeed.Value < 0.1f)
                {
                    Animator.Value.SetBool(IsWalking, false);
                    Animator.Value.speed = 1;
                    return Status.Success;
                }

                break;
            case MorphoCruiseMode.EmergencyStop:
                targetSpeed = 0;
                _currentSmoothTime = 1f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        CurrentSpeed.Value = Mathf.SmoothDamp(CurrentSpeed.Value, targetSpeed, ref _speedVelocity, _currentSmoothTime);
        CurrentDistance.Value += CurrentSpeed.Value * Time.deltaTime;
        
        // position
        float3 position = Track.Value.EvaluatePosition(t);
        Agent.Value.transform.position = position;
        
        // rotation
        float3 forward = Track.Value.EvaluateTangent(t);
        if (!forward.Equals(float3.zero))
            Agent.Value.transform.rotation = Quaternion.LookRotation(forward, Track.Value.EvaluateUpVector(t));
        
        // Animator animation speed
        if (Animator.Value != null)
            Animator.Value.speed = CurrentSpeed.Value / MaxSpeed.Value * walkMaxAnimationSpeed.Value;
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

