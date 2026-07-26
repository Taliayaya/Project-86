using System;
using System.Collections.Generic;
using AI;
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
    [SerializeReference] public BlackboardVariable<float> lookAheadDistance = new(15f);
    [SerializeReference] public BlackboardVariable<float> junctionLookAhead = new(30f);
    [SerializeReference] public BlackboardVariable<float> turnAroundSpeed = new(30f); // deg/s; negative turns the other way
    [SerializeReference] public BlackboardVariable<float> deadEndSlowDistance = new(40f);

    private const float MinExitRoom = 1f;
    // default resolution (4) can be tens of meters off on long rail splines
    private const int NearestPointResolution = 32;
    private const int NearestPointIterations = 8;

    private float _speedVelocity;
    private float _currentSmoothTime;

    private Direction _direction = Direction.Forward;

    private int _currentSplineIndex;
    private float _currentLength;
    // Current Track
    public Spline CurrentSpline => Track.Value[_currentSplineIndex];
    public float CurrentLength => _currentLength;

    private struct Exit
    {
        public int SplineId;
        public Direction Dir;
        public float T;
        public float Length;
        public Vector3 LookPos;
    }

    private readonly List<Exit> _exits = new();
    private bool _turningAround;

    public void OnJunctionReached(List<Intersection.Branch> branches)
    {
        var container = Track.Value;
        Vector3 agentPos = Agent.Value.transform.position;
        Vector3 agentForward = Agent.Value.transform.forward;
        float3 agentLocalPos = container.transform.InverseTransformPoint(agentPos);

        string hint = DestinationHint.Value;
        bool hintMode = !string.IsNullOrEmpty(hint);

        // collect every usable (branch, direction) exit at this junction
        _exits.Clear();
        foreach (var branch in branches)
        {
            if (branch.splineId < 0 || branch.splineId >= container.Splines.Count)
                continue;
            if (hintMode && !MatchesHint(branch, hint))
                continue;

            float length = container.CalculateLength(branch.splineId);
            if (length < MinExitRoom)
                continue;
            SplineUtility.GetNearestPoint(container.Splines[branch.splineId], agentLocalPos, out _, out float t,
                NearestPointResolution, NearestPointIterations);
            float distanceOnSpline = t * length;

            for (int dir = 0; dir < 2; dir++)
            {
                bool forward = dir == 0;
                float room = forward ? length - distanceOnSpline : distanceOnSpline;
                // an exit needs a full look-ahead of track: this drops both dead
                // directions and the short stub of a spline ending at a merge, so
                // the Morpho transfers onto the road that actually continues
                if (room < junctionLookAhead.Value)
                    continue;

                float lookT = (distanceOnSpline + (forward ? junctionLookAhead.Value : -junctionLookAhead.Value)) / length;
                Vector3 lookPos = container.EvaluatePosition(branch.splineId, lookT);

                // one-way switch: an exit is only valid facing the Morpho's travel
                // direction; anything heading back the way it came is ignored
                if (Vector3.Dot(lookPos - agentPos, agentForward) < 0f)
                    continue;

                _exits.Add(new Exit
                {
                    SplineId = branch.splineId,
                    Dir = forward ? Direction.Forward : Direction.Backward,
                    T = t,
                    Length = length,
                    LookPos = lookPos
                });
            }
        }

        if (_exits.Count == 0)
        {
            // no forward-facing exit (or none matching the hint) — continue straight;
            // a dead end ahead will trigger the turnaround and bring us back through
            return;
        }

        Exit chosen = hintMode ? PickTowardsDestination(agentPos) : PickRandom();

        _currentSplineIndex = chosen.SplineId;
        _direction = chosen.Dir;
        _currentLength = chosen.Length;
        CurrentDistance.Value = chosen.T * chosen.Length;
    }

    private Exit PickRandom() => _exits[UnityEngine.Random.Range(0, _exits.Count)];

    // ponytail: hints only pick the branch, not the travel direction — we keep
    // momentum. Author junctions so the hinted branch exits forward.
    private Exit PickTowardsDestination(Vector3 agentPos)
    {
        Vector3 agentForward = Agent.Value.transform.forward;
        Exit best = _exits[0];
        float bestScore = float.MinValue;
        foreach (var exit in _exits)
        {
            float score = Vector3.Dot((exit.LookPos - agentPos).normalized, agentForward);
            if (score > bestScore)
            {
                bestScore = score;
                best = exit;
            }
        }
        return best;
    }

    private static bool MatchesHint(in Intersection.Branch branch, string hint)
    {
        if (branch.hints != null)
        {
            for (int i = 0; i < branch.hints.Count; i++)
                if (branch.hints[i] == hint)
                    return true;
        }
        return branch.name != null && branch.name.Contains(hint);
    }

    protected override Status OnStart()
    {
        if (Agent.Value == null || Track.Value == null || Track.Value.Splines.Count == 0)
            return Status.Failure;

        _turningAround = false;
        if (IntersectionChannel.Value != null)
            IntersectionChannel.Value.Event += OnJunctionReached;

        // select the nearest spline on start
        var container = Track.Value;
        float3 agentLocalPos = container.transform.InverseTransformPoint(Agent.Value.transform.position);
        float bestDistance = float.MaxValue;
        float bestT = 0f;

        for (var i = 0; i < container.Splines.Count; i++)
        {
            SplineUtility.GetNearestPoint(container.Splines[i], agentLocalPos, out float3 nearest, out float t,
                NearestPointResolution, NearestPointIterations);
            float newDistance = math.distancesq(agentLocalPos, nearest);
            if (newDistance < bestDistance)
            {
                bestDistance = newDistance;
                bestT = t;
                _currentSplineIndex = i;
            }
        }
        _currentLength = container.CalculateLength(_currentSplineIndex);
        CurrentDistance.Value = bestT * _currentLength;

        // head wherever the agent already faces
        float3 tangent = container.EvaluateTangent(_currentSplineIndex, bestT);
        _direction = math.dot(tangent, (float3)Agent.Value.transform.forward) >= 0
            ? Direction.Forward
            : Direction.Backward;

        return Status.Running;
    }

    float AdjustSpeedBasedOnCurvature(float t, float3 tangentAtT)
    {
        float currentDistance = t * _currentLength;

        // looking ahead (in the travel direction) to anticipate a corner
        float sign = _direction == Direction.Forward ? 1f : -1f;
        float nextT = Mathf.Clamp01((currentDistance + sign * lookAheadDistance.Value) / _currentLength);

        float3 tangentB = Track.Value.EvaluateTangent(_currentSplineIndex, nextT);

        // corner angle?
        float angle = Vector3.Angle(tangentAtT, tangentB);

        return Mathf.Lerp(MaxSpeed.Value, MinSpeed.Value, angle * curveSensitivity);
    }

    protected override Status OnUpdate()
    {
        if (_currentLength <= 0f)
            return Status.Failure;

        if (_turningAround)
            return TurnAround();

        float t = Mathf.Clamp01(CurrentDistance.Value / _currentLength);
        float3 tangent = Track.Value.EvaluateTangent(_currentSplineIndex, t);
        float remaining = _direction == Direction.Forward
            ? _currentLength - CurrentDistance.Value
            : CurrentDistance.Value;

        float targetSpeed;
        switch (CruiseMode.Value)
        {
            case MorphoCruiseMode.Normal:
                targetSpeed = AdjustSpeedBasedOnCurvature(t, tangent);
                // ease into dead ends instead of snapping into a U-turn
                targetSpeed = Mathf.Min(targetSpeed, MaxSpeed.Value * (remaining / deadEndSlowDistance.Value));
                _currentSmoothTime = speedSmoothTime.Value;

                // dead end reached: turn around on the spot
                if (remaining <= 0.5f && CurrentSpeed.Value < 1f)
                {
                    _direction = _direction == Direction.Forward ? Direction.Backward : Direction.Forward;
                    _turningAround = true;
                }
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

        float step = CurrentSpeed.Value * Time.deltaTime;
        CurrentDistance.Value = Mathf.Clamp(
            CurrentDistance.Value + (_direction == Direction.Forward ? step : -step),
            0f, _currentLength);

        // position
        float3 position = Track.Value.EvaluatePosition(_currentSplineIndex, t);
        Agent.Value.transform.position = Vector3.MoveTowards(
            Agent.Value.transform.position,
            position,
            CurrentSpeed.Value * 1.5f * Time.deltaTime
        );

        // rotation — face the travel direction, not the spline's authored direction.
        // Skipped while stationary: a parked Morpho re-aligning to the tangent at 540°/s
        // would overpower MorphoFaceTarget's slow body twist toward its shooting target.
        float3 facing = _direction == Direction.Forward ? tangent : -tangent;
        if (CurrentSpeed.Value > 0.5f && !facing.Equals(float3.zero))
        {
            var targetRotation =
                Quaternion.LookRotation(facing, Track.Value.EvaluateUpVector(_currentSplineIndex, t));
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

    // rotate in place at a dead end until facing the new travel direction
    private Status TurnAround()
    {
        float t = Mathf.Clamp01(CurrentDistance.Value / _currentLength);
        float3 tangent = Track.Value.EvaluateTangent(_currentSplineIndex, t);
        Vector3 up = Track.Value.EvaluateUpVector(_currentSplineIndex, t);
        Vector3 targetForward = _direction == Direction.Forward ? (Vector3)tangent : -(Vector3)tangent;
        targetForward.Normalize();

        var agentTransform = Agent.Value.transform;
        float angle = Vector3.Angle(agentTransform.forward, targetForward);
        float step = turnAroundSpeed.Value * Time.deltaTime;
        if (angle <= Mathf.Abs(step))
        {
            agentTransform.rotation = Quaternion.LookRotation(targetForward, up);
            _turningAround = false;
        }
        else
        {
            agentTransform.rotation = Quaternion.AngleAxis(step, up) * agentTransform.rotation;
        }

        CurrentSpeed.Value = 0f;
        if (Animator.Value != null)
            Animator.Value.speed = walkMaxAnimationSpeed.Value * 0.2f; // slow shuffle while turning

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (IntersectionChannel.Value != null)
            IntersectionChannel.Value.Event -= OnJunctionReached;
    }
}
