using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NLegProceduralAnimation : MonoBehaviour
{
    [SerializeField] private Transform[] legTargets;
    
    [Header("Settings")]
    [SerializeField] private float stepSize = 1f;
    [SerializeField] private float stepHeight = 0.1f;
    [SerializeField] private int smoothness = 1;
    [SerializeField] private bool bodyOrientation = true;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] stepSounds;
    

    private Vector3[] _defaultLegPositions;
    private Vector3[] _lastLegPositions;
    private Vector3[] _desiredLegPositions;

    private Vector3 _lastBodyUp;
    private int _nbLegs;
    
    private Vector3 _velocity;
    private Vector3 _lastVelocity;
    private Vector3 _lastBodyPosition;
    
    private float _velocityMultiplier = 5f;
    private bool _legMoving = false;

    private static Vector3[] MatchToSurfaceFromAbove(Vector3 point, float halfRange, Vector3 up, LayerMask layerMask)
    {
        Vector3[] res = new Vector3[2];
        RaycastHit hit;
        Ray ray = new Ray(point + halfRange * up, -up);
        if (Physics.Raycast(ray, out hit, 2 * halfRange, layerMask))
        {
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
        _nbLegs = legTargets.Length;
        _defaultLegPositions = new Vector3[_nbLegs];
        _desiredLegPositions = new Vector3[_nbLegs];
        _lastLegPositions = new Vector3[_nbLegs];
        for (int i = 0; i < _nbLegs; i++)
        {
            _defaultLegPositions[i] = legTargets[i].localPosition;
            _lastLegPositions[i] = legTargets[i].position;
        }

        _lastBodyPosition = transform.position;
    }

    private void FixedUpdate()
    {
        UpdateVelocity();
            
        int legToMove = LegToMove();
        StickLegsToGround(legToMove);
        MoveLeg(legToMove);
        
        _lastBodyPosition = transform.position;
        if (_nbLegs > 3 && bodyOrientation)
            ApplyBodyOrientation();
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

    private int LegToMove()
    {
        float maxDistance = stepSize;
        int legToMove = -1;
        for (int i = 0; i < _nbLegs; i++)
        {
            _desiredLegPositions[i] = transform.TransformPoint(_defaultLegPositions[i]);
            float distance = Vector3.ProjectOnPlane(_desiredLegPositions[i] - _lastLegPositions[i], transform.up)
                .magnitude;
            if (distance > maxDistance)
            {
                maxDistance = distance;
                legToMove = i;
            }
        }

        return legToMove;
    }

    private void ApplyBodyOrientation()
    {
        Vector3 v1 = legTargets[0].position - legTargets[1].position;
        Vector3 v2 = legTargets[2].position - legTargets[3].position;
        Vector3 normal = Vector3.Cross(v1, v2).normalized;
        Debug.Log("normal is " + normal);
        Vector3 up = Vector3.Lerp(_lastBodyUp, normal, 1f / (float)(smoothness + 1f));
        transform.up = up;
        _lastBodyUp = up;
    }

    private const float RaycastRange = 5f;

    private void MoveLeg(int legToMove)
    {
        if (legToMove == -1 || _legMoving)
            return;
        Vector3 targetPoint = _desiredLegPositions[legToMove] +
                              //Mathf.Clamp(_velocity.magnitude * _velocityMultiplier, 0f, 1.5f) *
                              //(_desiredLegPositions[legToMove] - legTargets[legToMove].position) + 
                              _velocity * _velocityMultiplier;
        _legMoving = true;
        Vector3[] positionAndNormal = MatchToSurfaceFromAbove(targetPoint, RaycastRange, transform.up, groundLayer);
        StartCoroutine(MoveLegCoroutine(legToMove, positionAndNormal[0]));
    }

    private void StickLegsToGround(int legToIgnore)
    {
        for (int i = 0; i < _nbLegs; i++)
        {
            if (i != legToIgnore)
            {
                legTargets[i].position = _lastLegPositions[i];
            }
        }
    }

    private int _soundQueue = 0;
    
    private IEnumerator MoveLegCoroutine(int legToMove, Vector3 targetPoint)
    {
        _soundQueue++;
        Vector3 startPos = _lastLegPositions[legToMove];

        for (int i = 1; i <= smoothness; i++)
        {
            legTargets[legToMove].position = Vector3.Lerp(startPos, targetPoint, (float)i / (float)( smoothness + 1f));
            legTargets[legToMove].position += transform.up * (Mathf.Sin((float)i / (smoothness + 1f) * Mathf.PI) * stepHeight);
            yield return new WaitForFixedUpdate();
        }
        if (Physics.Raycast(legTargets[legToMove].position, Vector3.down, out RaycastHit hit, 1f, groundLayer))
            audioSource.PlayOneShot(stepSounds[Random.Range(0, stepSounds.Length)], 1 / (float)_soundQueue );
        legTargets[legToMove].position = targetPoint;
        _lastLegPositions[legToMove] = targetPoint;
        _legMoving = false;
        _soundQueue--;
    }

    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < _nbLegs; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(legTargets[i].position, 0.5f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.TransformPoint(_defaultLegPositions[i]), 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_defaultLegPositions[i], stepSize);
        }
    }
}
