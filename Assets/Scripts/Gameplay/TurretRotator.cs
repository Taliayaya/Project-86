using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class CannonController : MonoBehaviour
{
    // A struct to pair each turret with its target
    [System.Serializable]
    public class Angle
    {
        public float min;
        public float max;

        public Angle(float _min, float _max)
        {
            min = _min;
            max = _max;
        }

    }

    [System.Serializable]
    public class TurretData
    {
        public Transform turret;
        public Transform target;
        public float rotationSpeed = 5f;
        [FormerlySerializedAs("xAngle")] public Angle yAngle = new Angle(-360, 360);
        [FormerlySerializedAs("yAngle")] public Angle xAngle = new Angle(-90, 90);
        public bool invert = false;

        [NonSerialized] public Quaternion defaultRotation;

        public Vector3 Position => turret.position;

        public Quaternion Rotation
        {
            get => turret.rotation;
            set => turret.rotation = value;
        }

    }

    // A list of all turrets and their respective targets
    public List<TurretData> turrets;

    private void Start()
    {
        foreach (var turretData in turrets)
        {
            turretData.defaultRotation = turretData.turret.localRotation;
        }
    }

    void Update()
    {
        // Rotate each turret towards its respective target individually
        foreach (var turretData in turrets)
        {
            if (turretData.target != null)
            {
                AimTurretAtTarget(turretData, turretData.target);
            }
        }
    }

    private void AimTurretAtTarget(TurretData turret, Transform target)
    {
        // Calculate the direction to the target
        Vector3 directionToTarget = target.position - turret.Position;

        // Calculate the desired rotation towards the target
        Quaternion targetRotation = target == null ? transform.parent.rotation * turret.defaultRotation : Quaternion.LookRotation(directionToTarget);

        // Interpolate smoothly towards the target rotation
        turret.Rotation =
            Quaternion.RotateTowards(turret.Rotation, targetRotation, turret.rotationSpeed * Time.deltaTime);
        Vector3 clampedEulerAngle = turret.turret.localEulerAngles;
        if (turret.invert)
        {
            clampedEulerAngle.y = ClampAngle(clampedEulerAngle.y, turret.yAngle.min, turret.yAngle.max);
            clampedEulerAngle.x = ClampAngle(clampedEulerAngle.x, turret.xAngle.min, turret.xAngle.max);
        }
        else
        {
            clampedEulerAngle.y = ClampAngleOuter(clampedEulerAngle.y, turret.yAngle.min, turret.yAngle.max);
            clampedEulerAngle.x = ClampAngle(clampedEulerAngle.x, turret.xAngle.min, turret.xAngle.max);
        }

        turret.turret.localEulerAngles = clampedEulerAngle;
    }

    // inner angle arc
    private float ClampAngle(float angle, float min, float max)
    {
        angle = angle % 360;
        if (angle < min)
            return angle;
        else if (angle > max)
            return angle;
        else if ((min + max) / 2 > angle)
            return min;
        else
            return max;
    }

    // outer angle 
    private float ClampAngleOuter(float angle, float min, float max)
    {
        angle = angle % 360;
        return Mathf.Clamp(angle, min, max);
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        foreach (var turretData in turrets)
        {
            Handles.color = Color.green;
            Handles.DrawWireArc(turretData.Position, Vector3.up,
                Quaternion.Euler(0, turretData.xAngle.min, 0) * Vector3.forward,
                turretData.xAngle.max - turretData.xAngle.min, 1, 2);
            Handles.color = Color.blue;
            Handles.DrawWireArc(turretData.Position, Vector3.forward,
                Quaternion.Euler(turretData.yAngle.min, 0, 0) * Vector3.forward,
                turretData.yAngle.max - turretData.yAngle.min, 1, 2);
        }
#endif
    }

    public void SetTurretTarget(Transform target, int index)
    {
        turrets[index].target = target;
    }

    public void SetTurret0Target(Unit target)
    {
        turrets[0].target = target.transform;
    }
    public void SetTurret0Target(TargetInfo target)
    {
        turrets[0].target = target?.Unit.transform;
    }

    public void SetTurret1Target(Unit target)
    {
        turrets[1].target = target == null ? null : target.transform;
    }

    public void SetTurret2Target(Unit target)
    {
        turrets[2].target = target == null ? null : target.transform;
    }
}