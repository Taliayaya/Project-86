using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Mecha
{
    public class RotatingTurretDinosauria : MonoBehaviour
    {
        [Header("Rotating Weapon Settings")] [SerializeField]
        private Transform target;

        [SerializeField] private Transform turret;
        [SerializeField] private Transform turretBase;
        [SerializeField] private float rotationSpeed = 10;

        [Header("Rotation Limits Y")]
        // relative to the initial rotation
        [SerializeField]
        Vector2 rotationLimitsY = new Vector2(-90, 90);

        [SerializeField] private bool reverseRotationY;

        [Header("Rotation Limits X")] [SerializeField]
        Vector2 rotationLimitsX = new Vector2(-45, 45);

        [SerializeField] private bool reverseRotationX;


        private Vector3 _initialRotation;

        private void Awake()
        {
            _initialRotation = turret.localEulerAngles;
            Debug.Log("Initial Rotation: " + _initialRotation);
            StartCoroutine(RotateToTarget());
        }

        private static float ReverseClamp(float val, float low, float high)
        {
            if (val > low && val < high)
            {
                float mid = (high - low) / 2 + low;
                return val < mid ? low : high;
            }

            return val;
        }

        private delegate float ClampFunction(float val, float low, float high);

        private static (float lower, float upper) GetBoundsAngle(Vector2 limits, float initialRotationAxis)
        {
            float lower = (initialRotationAxis + limits.x + 360) % 360;
            float upper = (initialRotationAxis + limits.y + 360) % 360;
            if (lower > upper)
                (lower, upper) = (upper, lower);
            return (lower, upper);
        }

        /// <summary>
        /// Constantly rotates the turret to the target
        /// Go back to initial position if there is no target
        /// Takes into account the rotation limits
        /// </summary>
        /// <returns></returns>
        private IEnumerator RotateToTarget()
        {
            var initialRotation = Quaternion.Euler(_initialRotation);
            // we need to work with angle between 0 and 360
            (float lowerEulerY, float upperEulerY) = GetBoundsAngle(rotationLimitsY, _initialRotation.y);
            (float lowerEulerX, float upperEulerX) = GetBoundsAngle(rotationLimitsX, _initialRotation.x);

            // Perhaps we want the outer angle and not the inner
            ClampFunction clampFunctionY = reverseRotationY ? ReverseClamp : Mathf.Clamp;
            ClampFunction clampFunctionX = reverseRotationX ? ReverseClamp : Mathf.Clamp;

            while (true)
            {
                if (target)
                {
                    var targetDirection = target.position - turret.position;
                    var rotation = Quaternion.LookRotation(targetDirection);

                    var stepRotation =
                        Quaternion.Slerp(turret.rotation, rotation, Time.deltaTime * rotationSpeed);
                    stepRotation = stepRotation * Quaternion.Inverse(turretBase.rotation);
                    var dir = stepRotation * Vector3.forward;
                    var rot = Quaternion.LookRotation(dir.normalized);
                    var euler = rot.eulerAngles;
                    euler.y = _initialRotation.y; //clampFunctionY(euler.y, lowerEulerY, upperEulerY);
                    euler.z = _initialRotation.z; // clampFunctionX(euler.x, lowerEulerX, upperEulerX);
                    turret.localRotation = Quaternion.Euler(euler);
                }
                else
                {
                    turret.localRotation = Quaternion.Slerp(turret.localRotation, initialRotation,
                        Time.deltaTime * rotationSpeed);
                }

                yield return null;
            }
        }

        [SerializeField] private Vector3 angle;

        private Vector3 _angle;


        private void Update()
        {
            transform.localRotation = Quaternion.Euler(_angle + angle);
        }

    }
}