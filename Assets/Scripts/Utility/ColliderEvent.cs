using System;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    [RequireComponent(typeof(Collider))]
    public class ColliderEvent : MonoBehaviour
    {
        public UnityEvent<Collision> onCollisionEnter;
        public UnityEvent<Collision> onCollisionExit;
        public UnityEvent<Collider> onTriggerEnter;
        public UnityEvent<Collider> onTriggerExit;

        public string tagFilter;

        private Rigidbody _rigidbody;
        private void Start()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            onCollisionEnter?.Invoke(collision);
        }
        
        protected virtual void OnCollisionExit(Collision collision)
        {
            onCollisionExit?.Invoke(collision);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody == _rigidbody)
                return;
            if (!string.IsNullOrEmpty(tagFilter) && !other.CompareTag(tagFilter))
                return;
            onTriggerEnter?.Invoke(other);
        }
        
        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody == _rigidbody)
                return;
            if (tagFilter != null && !other.CompareTag(tagFilter))
                return;
            onTriggerExit?.Invoke(other);
        }
    }
}