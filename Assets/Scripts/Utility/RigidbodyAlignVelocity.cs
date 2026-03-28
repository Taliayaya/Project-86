using System;
using UnityEngine;

namespace Utility
{
    public class RigidbodyAlignVelocity : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float rotationSpeed;

        private void Awake()
        {
            if (!rb)
                rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (rb.linearVelocity.sqrMagnitude > 1f)
            {
                Vector3 targetDirection = rb.linearVelocity.normalized;

                Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                rb.rotation = Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * rotationSpeed
                );
            }
        }
    }
}