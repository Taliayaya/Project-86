using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class NLegProceduralAnimation : MonoBehaviour
{
    [SerializeField] private Transform[] legTargets;
    public bool isGrounded;
    
    [Header("Settings")]
    [SerializeField] private float stepSize = 1f;
    [SerializeField] private float stepHeight = 0.1f;
    [SerializeField] private int smoothness = 1;
    [SerializeField] private bool bodyOrientation = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Vector3 legOffset = new Vector3(0, 0.0f, 0);
    [SerializeField] private float groundedRaycastRange = 0.3f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] stepSounds;

    [Header("Events")] public UnityEvent<bool> onIsGrounded = new UnityEvent<bool>();
    
    

    private Vector3[,] _defaultLegPositions;
    private Vector3[,] _lastLegPositions;
    private Vector3[,] _desiredLegPositions;

    private Vector3 _lastBodyUp;
    private int _nbLegs;
    
    private Vector3 _velocity;
    private Vector3 _lastVelocity;
    private Vector3 _lastBodyPosition;
    
    [SerializeField] private float velocityMultiplier = 20f;
    private bool[,] _legMoving;

    private int _groundedCount = 0;

    private static Vector3[] MatchToSurfaceFromAbove(Vector3 point, float halfRange, Vector3 up, LayerMask layerMask)
    {
        Vector3[] res = new Vector3[2];
        RaycastHit hit;
        Ray ray = new Ray(point + halfRange * up, -up);
        if (Physics.Raycast(ray, out hit, 3 * halfRange, layerMask) && !hit.collider.CompareTag("NonHitbox"))
        {
            Debug.Log($"leghit: {hit.collider.gameObject.name} on {hit.transform.name}");
            res[0] = hit.point;
            res[1] = hit.normal;
        }
        else
        {
            res[0] = point;
        }

        return res;
    }

    private void Start()
    {
        _lastBodyUp = transform.up;
        _nbLegs = legTargets.Length / 2;
        
        _legMoving = new bool[_nbLegs, 2];
        _defaultLegPositions = new Vector3[_nbLegs, 2];
        _desiredLegPositions = new Vector3[_nbLegs, 2];
        _lastLegPositions = new Vector3[_nbLegs, 2];
        for (int i = 0; i < _nbLegs; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                _defaultLegPositions[i, j] = transform.InverseTransformPoint(legTargets[i * 2 + j].position);
                _lastLegPositions[i, j] = legTargets[i * 2 + j].position;
            }
        }

        _lastBodyPosition = transform.position;
    }

    private void FixedUpdate()
    {
        _velocity = rb ? rb.velocity : agent.velocity;
        //UpdateVelocity();

        (int legToMove, int leftLeg) = LegToMove();

        MoveLeg(legToMove, leftLeg);
        if (_groundedCount > 0)
            StickLegsToGround();

        _lastBodyPosition = transform.position;
        if (_nbLegs > 3 && bodyOrientation)
            ApplyBodyOrientation();
        CheckGround();
    }

    private void UpdateVelocity()
    {
        _velocity = transform.position - _lastBodyPosition;
        _velocity = (_velocity + smoothness * _lastVelocity) / (smoothness + 1f);

        if (_velocity.magnitude < 0.000025f)
            _velocity = _lastVelocity;
        else
            _lastVelocity = _velocity;
    }

    private (int, int) LegToMove()
    {
        float maxDistance = stepSize;
        int legToMove = -1;
        int leftLeg = 0;
        for (int i = 0; i < _nbLegs; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (_legMoving[i, j])
                    continue;
                _desiredLegPositions[i, j] = transform.TransformPoint(_defaultLegPositions[i, j]) + legOffset;

                Vector3 direction = Vector3.ProjectOnPlane(
                    _desiredLegPositions[i, j] + _velocity * velocityMultiplier - _lastLegPositions[i, j],
                    transform.up);
                float distance = direction.magnitude;
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    legToMove = i;
                    leftLeg = j;
                }
            }
        }

        return (legToMove, leftLeg);
    }

    private void ApplyBodyOrientation()
    {
        Vector3 v1 = legTargets[0].position - legTargets[1].position;
        Vector3 v2 = legTargets[^2].position - legTargets[^1].position;
        Vector3 normal = Vector3.Cross(v1, v2).normalized;
        Vector3 up = Vector3.Lerp(_lastBodyUp, normal, 1f / (float)(smoothness + 1f));
        transform.up = up;
        _lastBodyUp = up;
    }

    private const float RaycastRange = 5f;

    private void MoveLeg(int legToMove, int leftLeg)
    {
        if (legToMove == -1 || _legMoving[legToMove, leftLeg] ||
            legToMove - 1 >= 0 && _legMoving[legToMove - 1, leftLeg] ||
            legToMove + 1 < _nbLegs && _legMoving[legToMove + 1, leftLeg] ||
            _legMoving[legToMove, 1 - leftLeg])
        {
            return;
        }

        _legMoving[legToMove, leftLeg] = true;
        if (_groundedCount == 0)
        {
            StartCoroutine(MoveLegCoroutine(legToMove, leftLeg,
                transform.TransformPoint(_defaultLegPositions[legToMove, leftLeg])));
            return;
        }

        Vector3 targetPoint = _desiredLegPositions[legToMove, leftLeg] +
                              Mathf.Clamp(_velocity.magnitude * velocityMultiplier, 0f, 1.5f) *
                              (_desiredLegPositions[legToMove, leftLeg] - legTargets[legToMove * 2 + leftLeg].position) + 
                              _velocity * velocityMultiplier;
        Vector3[] positionAndNormal = MatchToSurfaceFromAbove(targetPoint, RaycastRange, transform.up, groundLayer);
        StartCoroutine(MoveLegCoroutine(legToMove, leftLeg, positionAndNormal[0] + legOffset));
    }

    private void StickLegsToGround()
    {
        
        for (int i = 0; i < _nbLegs; i++)
        {
            for (int j = 0; j < 2; j++)
                if (!_legMoving[i, j])
                {
                    legTargets[i * 2 + j].position = _lastLegPositions[i, j];
                }
        }
    }

    private int _soundQueue = 0;
    
    private IEnumerator MoveLegCoroutine(int legToMove, int leftLeg, Vector3 targetPoint)
    {
        _soundQueue++;
        Vector3 startPos = _lastLegPositions[legToMove, leftLeg];

        int legToMoveIndex = legToMove * 2 + leftLeg;
        for (int i = 1; i <= smoothness; i++)
        {
            legTargets[legToMoveIndex].position = Vector3.Lerp(startPos, targetPoint, (float)i / (float)( smoothness + 1f));
            legTargets[legToMoveIndex].position += transform.up * (Mathf.Sin((float)i / (smoothness + 1f) * Mathf.PI) * stepHeight);
            yield return new WaitForFixedUpdate();
        }

        if (Physics.Raycast(legTargets[legToMoveIndex].position, Vector3.down, out RaycastHit hit, 1.5f, groundLayer))
        {
            audioSource.PlayOneShot(stepSounds[Random.Range(0, stepSounds.Length)], 1 / (float)_soundQueue);
        }

        legTargets[legToMoveIndex].position = targetPoint;
        _lastLegPositions[legToMove, leftLeg] = targetPoint;
        _legMoving[legToMove, leftLeg] = false;
        _soundQueue--;
    }

    private void CheckGround()
    {
        _groundedCount = 0;
        for (int i = 0; i < legTargets.Length; i++)
        {
            bool grounded = Physics.CheckSphere(legTargets[i].position - legOffset, groundedRaycastRange, groundLayer);
            if (grounded)
                _groundedCount++;
        }

        isGrounded = (_groundedCount * 2 >= legTargets.Length); // 50% of legs on ground == grounded
        onIsGrounded?.Invoke(isGrounded);
    }


    private void OnDrawGizmos()
    {
        for (int i = 0; i < _nbLegs; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(legTargets[i * 2 + j].position, 0.5f);
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(transform.TransformPoint(_defaultLegPositions[i, j]), 0.5f);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_desiredLegPositions[i, j], 0.5f);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_lastLegPositions[i, j], 0.5f);
                Gizmos.DrawWireSphere(legTargets[i * 2 + j].position - legOffset, groundedRaycastRange);
            }
        }
    }
}
