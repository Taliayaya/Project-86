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
        public Transform parent;
        public Transform target;
        public float rotationSpeed = 5f;
        public bool lockZAxis = false;
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

    void FixedUpdate()
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

    
    private float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a < 0) a += 360f;
        return a;
    }
    
    private float ClampAngleRange(float angle, float min, float max)
    {
        angle = NormalizeAngle(angle);
        min = NormalizeAngle(min);
        max = NormalizeAngle(max);

        bool wraps = min > max;

        if (!wraps)
        {
            // Normal case (example: min=20, max=70)
            return Mathf.Clamp(angle, min, max);
        }

        // Wraparound case (example: min=300, max=45)
        bool isInside = (angle > min || angle < max);

        if (isInside)
            return angle;

        // Clamp to closest boundary
        float dMin = Mathf.Abs(Mathf.DeltaAngle(angle, min));
        float dMax = Mathf.Abs(Mathf.DeltaAngle(angle, max));

        return (dMin < dMax) ? min : max;
    }

    private void AimTurretAtTarget(TurretData turret, Transform target)
    {
        // Calculate the direction to the target
        Vector3 directionToTarget = target.position - turret.Position;

        // Calculate the desired rotation towards the target
        Quaternion targetRotation = target == null ? transform.parent.rotation * turret.defaultRotation : Quaternion.LookRotation(directionToTarget);
        
        // Interpolate smoothly towards the target rotation
        var rotation =
            Quaternion.RotateTowards(turret.Rotation, targetRotation, turret.rotationSpeed * Time.fixedDeltaTime);
        
        // convert it to local space of parent to clamp it
        Quaternion localRot = Quaternion.Inverse(turret.parent.rotation) * rotation;
        
        Vector3 clampedEulerAngle = localRot.eulerAngles;
        if (turret.invert)
        {
            // if max == min, then we dont need to clamp the y angle
            if (!Mathf.Approximately(turret.yAngle.max, turret.yAngle.min))
                clampedEulerAngle.y = ClampAngle(clampedEulerAngle.y, turret.yAngle.min, turret.yAngle.max);
            clampedEulerAngle.x = ClampPitch(clampedEulerAngle.x, turret.xAngle.min, turret.xAngle.max);
            
            if (turret.lockZAxis)
                clampedEulerAngle.z = 0;
        }
        // else
        // {
        //     clampedEulerAngle.y = ClampAngleOuter(clampedEulerAngle.y, turret.yAngle.min, turret.yAngle.max);
        //     clampedEulerAngle.x = ClampAngle(clampedEulerAngle.x, turret.xAngle.min, turret.xAngle.max);
        // }
        
        // convert it back to world space
        Quaternion finalRot = turret.parent.rotation * Quaternion.Euler(clampedEulerAngle);

        turret.turret.rotation = finalRot;
    }

    // inner angle arc
    private float ClampAngle(float angle, float min, float max)
    {
        // Normalize all angles to 0..360
        angle = (angle % 360 + 360) % 360;
        min = (min % 360 + 360) % 360;
        max = (max % 360 + 360) % 360;

        // Check if range crosses 0
        if (min <= max)
            return Mathf.Clamp(angle, min, max);
        else
        {
            // Range wraps around 0
            if (angle >= min || angle <= max)
                return angle; // inside the wrap-around range
            // Outside range → clamp to nearest boundary
            float distToMin = Mathf.DeltaAngle(angle, min);
            float distToMax = Mathf.DeltaAngle(angle, max);
            return Mathf.Abs(distToMin) < Mathf.Abs(distToMax) ? min : max;
        }
    }
    
    float ClampPitch(float xAngle, float min, float max)
    {
        // Normalize 0..360
        xAngle = (xAngle % 360 + 360) % 360;

        // If angle > 180, treat as negative
        if (xAngle > 180f) xAngle -= 360f;

        // Clamp -45 → 45
        xAngle = Mathf.Clamp(xAngle, min, max);

        return xAngle;
    }

    // outer angle 
    private float ClampAngleOuter(float angle, float min, float max)
    {
        angle = angle % 360;
        return Mathf.Clamp(angle, min, max);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        foreach (var turretData in turrets)
        {
            var pos = turretData.Position;
            var turretRot = turretData.turret.rotation;
            
            // ---- Yaw (green) ----
            Handles.color = Color.green;
            Vector3 yawFrom = turretRot * Vector3.forward; // forward at min yaw
            Handles.DrawWireArc(
                pos,
                turretData.parent.up, // horizontal plane
                Quaternion.Euler(0, turretData.yAngle.min, 0) * turretData.parent.forward,
                turretData.yAngle.max - turretData.yAngle.min,
                4f // radius
            );

            // ---- Pitch (blue) ----
            Handles.color = Color.blue;
            Vector3 pitchFrom = turretRot * Vector3.forward; // forward at min pitch
            Handles.DrawWireArc(
                pos,
                turretData.parent.right, // vertical plane
                Quaternion.Euler(turretData.xAngle.min, 0, 0) * turretData.parent.forward,
                turretData.xAngle.max - turretData.xAngle.min,
                2.5f // radius
            );
            // Handles.color = Color.green;
            // Handles.DrawWireArc(turretData.Position, Vector3.up,
            //     Quaternion.Euler(0, turretData.xAngle.min, 0) * Vector3.forward,
            //     turretData.xAngle.max - turretData.xAngle.min, 1, 2);
            // Handles.color = Color.blue;
            // Handles.DrawWireArc(turretData.Position, Vector3.forward,
            //     Quaternion.Euler(turretData.yAngle.min, 0, 0) * Vector3.forward,
            //     turretData.yAngle.max - turretData.yAngle.min, 1, 2);
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