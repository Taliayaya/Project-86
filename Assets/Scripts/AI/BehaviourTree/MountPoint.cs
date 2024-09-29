using UnityEditor;
using UnityEngine;


public class MountPoint : MonoBehaviour
{
    [Range(0, 360f)]
    public float angleLimit = 90f;
    [Range(0, 360f)]
    public float aimTolerance = 1f;
    public float turnSpeed = 90f;

    Transform turret;

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        var range = 20f;
        var hardpoint = transform;
        var from = Quaternion.AngleAxis(-angleLimit / 2, hardpoint.up) * hardpoint.forward;

        Handles.color = new Color(0, 1, 0, .2f);
        Handles.DrawSolidArc(hardpoint.position, hardpoint.up, from, angleLimit, range);
#endif
    }

    void Awake()
    {
        turret = transform.GetChild(0);
    }

    public bool Aim(Vector3 targetPoint)
    {
        return Aim(targetPoint, out _);
    }

    public bool Aim(Vector3 targetPoint, out bool reachAngleLimit)
    {
        reachAngleLimit = default;
        var hardpoint = transform;
        var los = targetPoint - hardpoint.position;
        var halfAngle = angleLimit / 2;
        var losOnPlane = Vector3.ProjectOnPlane(los, hardpoint.up);
        var deltaAngle = Vector3.SignedAngle(hardpoint.forward, losOnPlane, hardpoint.up);

        if (Mathf.Abs(deltaAngle) > halfAngle)
        {
            reachAngleLimit = true;
            losOnPlane = hardpoint.rotation * Quaternion.Euler(0, Mathf.Clamp(deltaAngle, -halfAngle, halfAngle), 0) * Vector3.forward;
        }

        var targetRotation = Quaternion.LookRotation(losOnPlane, hardpoint.up);
        var aimed = !reachAngleLimit && Quaternion.Angle(turret.rotation, targetRotation) < aimTolerance;
        turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRotation, turnSpeed * Time.deltaTime);

        return aimed;
    }
}