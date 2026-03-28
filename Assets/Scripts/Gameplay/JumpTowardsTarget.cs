using System;
using Gameplay.Units;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Gameplay
{
    public class JumpTowardsTarget : NetworkBehaviour
    {
        public float gravity;
        public float jumpHeight;
        public float forceMultiplier = 1f;
        
        public bool isJumping;
        
        [SerializeField] private Rigidbody rb;
        [SerializeField] private NavMeshAgent agent;
        
        [SerializeField] private Transform target;

        public UnityEvent onJumpDone;
        

        [ContextMenu("Jump")]
        public void Jump()
        {
            if (isJumping)
                return;
            agent.enabled = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            Vector3 velocity = CalculateLaunchVelocity(target.position);
            rb.AddForce(velocity * forceMultiplier, ForceMode.Impulse);
            isJumping = true;
            Invoke(nameof(SetIsJumping), 0.5f);
        }

        public void SetIsJumping()
        {
            isJumping = true;
        }

        public void JumpDone()
        {
            if (!isJumping) return;
            isJumping = false;
            agent.enabled = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            
            if (IsSpawned)
                onJumpDone?.Invoke();
        }

        private Vector3 CalculateLaunchVelocity(Vector3 targetPos)
        {
            Vector3 posTarget = targetPos;
            Vector3 position = transform.position;
            float displacementY = posTarget.y - position.y;
            Vector3 displacementXZ = new Vector3(posTarget.x - position.x, 0, posTarget.z - position.z);
            
            Vector3 velocityY = Vector3.up * MathF.Sqrt(-2 * gravity * jumpHeight);
            Vector3 velocityXZ = displacementXZ / (MathF.Sqrt(-2 * jumpHeight / gravity) +
                                                   MathF.Sqrt(2 * (displacementY - jumpHeight) / gravity));
            return velocityXZ + velocityY;
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        public void SetTarget(Unit unit)
        {
            SetTarget(unit.transform);
        }

        public void SetTarget(TargetInfo info)
        {
            if (info == null || info.Unit == null) return;
            SetTarget(info.Unit.transform);
        }

        public void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Default") ||
                other.gameObject.layer == LayerMask.NameToLayer("Damageable"))
            {
                JumpDone();
            }
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Default") ||
                other.gameObject.layer == LayerMask.NameToLayer("Damageable"))
            {
                JumpDone();
            }
        }
    }
}