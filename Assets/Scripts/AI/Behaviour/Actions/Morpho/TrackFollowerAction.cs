using System;
using System.Collections.Generic;
using AI;
using Unity.Behavior;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TrackFollower", story: "[Agent] moves along the [track] at [MaxSpeed]", category: "Action/Navigation", id: "dac50ffa996d3f563acf33f467e6475d")]
public partial class TrackFollowerAction : Action
{
    public enum Direction
    {
        Forward,
        Backward
    };
    
    private static readonly int IsWalking = UnityEngine.Animator.StringToHash("isWalking");
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<SplineContainer> Track;
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    
    [SerializeReference] public BlackboardVariable<MorphoCruiseMode> CruiseMode;
    [SerializeReference] public BlackboardVariable<float> BrakeDuration = new(2f);
    [SerializeReference] public BlackboardVariable<string> DestinationHint;
    [SerializeReference] public BlackboardVariable<MorphoIntersectionChannel> IntersectionChannel;
    
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
    private float _currentSmoothTime;

    private Direction _direction = Direction.Forward ;

    private int _currentSplineIndex = 0;
    private float _currentLength = 0;
    // Current Track
    public Spline CurrentSpline => Track.Value[_currentSplineIndex];
    public float CurrentLength => _currentLength;

    public void OnJunctionReached(List<Intersection.Branch> branches)
    {
        Debug.Log($"Junction reached: {branches.Count}");
        if (!string.IsNullOrEmpty(DestinationHint))
        {
            foreach (var branch in branches)
            {
                if (branch.name.Contains(DestinationHint.Value))
                {
                    _currentSplineIndex = branch.splineId;
                    break;
                }
            }
        }
        else
        {
            int randomSpline = Random.Range(0, branches.Count);
            _currentSplineIndex = branches[randomSpline].splineId;
        }
        
        float3 agentLocalPos = Track.Value.transform.InverseTransformPoint(Agent.Value.transform.position);
        SplineUtility.GetNearestPoint(CurrentSpline, agentLocalPos, out float3 nearest, out float t);
        CurrentDistance.Value = t * CurrentLength;
        _currentLength = Track.Value.CalculateLength(_currentSplineIndex);
    }

    protected override Status OnStart()
    {
        if (Agent.Value == null || Track.Value == null)
            return Status.Failure;

        if (IntersectionChannel.Value != null)
            IntersectionChannel.Value.Event += OnJunctionReached;

        // select the nearest spline on start
        float3 agentLocalPos = Track.Value.transform.InverseTransformPoint(Agent.Value.transform.position);
        float distance = float.MaxValue;
        
        for (var i = 0; i < Track.Value.Splines.Count; i++)
        {
            SplineUtility.GetNearestPoint(Track.Value.Splines[i], agentLocalPos, out float3 nearest, out float t);
            float newDistance = math.distancesq(agentLocalPos, nearest);
            if (newDistance < distance)
            {
                distance = newDistance;
                CurrentDistance.Value = t * CurrentLength;
                _currentSplineIndex = i;
            }
        }
        _currentLength = Track.Value.CalculateLength(_currentSplineIndex);
        
        return Status.Running;
    }
    
    float AdjustSpeedBasedOnCurvature(float t)
    {
        float nativeLength = CurrentLength;
        float currentDistance = t * nativeLength;
    
        // looking ahead to anticipate a corner
        float nextT = ((currentDistance + lookAheadDistance) % nativeLength) / nativeLength;

        float3 tangentA = Track.Value.EvaluateTangent(_currentSplineIndex, t);
        float3 tangentB = Track.Value.EvaluateTangent(_currentSplineIndex, nextT);

        // corner angle?
        float angle = Vector3.Angle(tangentA, tangentB);

        // Debug.Log($"Angle: {angle:F2} | Speed: {currentSpeed:F1}");

        return Mathf.Lerp(MaxSpeed.Value, MinSpeed.Value, angle * curveSensitivity);
    }

    protected override Status OnUpdate()
    {
        // speed
        float t = (float)(CurrentDistance.Value / CurrentLength);
        
        if (Mathf.Approximately(t, 1))
            _direction = Direction.Backward;
        else if (Mathf.Approximately(t, 0))
            _direction = Direction.Forward;

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
        
        if (_direction == Direction.Forward)
            CurrentDistance.Value += CurrentSpeed.Value * Time.deltaTime;
        else 
            CurrentDistance.Value -= CurrentSpeed.Value * Time.deltaTime;
        
        // position
        float3 position = Track.Value.EvaluatePosition(_currentSplineIndex, t);
        Agent.Value.transform.position = Vector3.MoveTowards(
            Agent.Value.transform.position, 
            position, 
            CurrentSpeed.Value * 1.5f * Time.deltaTime
        );
        
        // rotation
        float3 forward = Track.Value.EvaluateTangent(_currentSplineIndex, t);
        if (!forward.Equals(float3.zero))
        {
            var targetRotation = 
                Quaternion.LookRotation(forward, Track.Value.EvaluateUpVector(_currentSplineIndex, t));
            Agent.Value.transform.rotation = Quaternion.RotateTowards(
                Agent.Value.transform.rotation, 
                targetRotation, 
                540f * Time.deltaTime
            );
        }

        // Animator animation speed
        if (Animator.Value != null)
            Animator.Value.speed = CurrentSpeed.Value / MaxSpeed.Value * walkMaxAnimationSpeed.Value;
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (IntersectionChannel.Value != null)
            IntersectionChannel.Value.Event -= OnJunctionReached;
    }
}

