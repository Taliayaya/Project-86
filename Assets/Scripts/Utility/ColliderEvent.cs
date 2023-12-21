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

        protected virtual void OnCollisionEnter(Collision collision)
        {
            onCollisionEnter?.Invoke(collision);
        }
        
        protected virtual void OnCollisionExit(Collision collision)
        {
            onCollisionExit?.Invoke(collision);
        }

        protected void OnTriggerEnter(Collider other)
        {
            onTriggerEnter?.Invoke(other);
        }
        
        protected void OnTriggerExit(Collider other)
        {
            onTriggerExit?.Invoke(other);
        }
    }
}