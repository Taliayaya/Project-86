using Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace BladesCombat
{
    public enum BladeDirection
    {
        Left,
        Right
    }

    public class BladeCollisionTrigger : MonoBehaviour
    {
        [SerializeField] private BladeDirection bladeDirection;

        public UnityEvent<Collider, bool> OnBladeTriggerEnter { get; } = new UnityEvent<Collider, bool>();
        public UnityEvent<Collider, bool> OnBladeTriggerStay { get; } = new UnityEvent<Collider, bool>();
        public UnityEvent<Collider, bool> OnBladeTriggerExit { get; } = new UnityEvent<Collider, bool>();
        public bool IsActive { get; set; } = false;

        private void OnTriggerEnter(Collider other)
        {
            // if (!IsActive) return;
            
            if (other.gameObject.TryGetComponent(out IHealth _) || other.gameObject.layer == LayerMask.NameToLayer("Sliceable"))
            {
                if (other.CompareTag("NonHitbox"))
                {
                    // DeflectBullet(other);
                    return;
                }
                OnBladeTriggerEnter.Invoke(other, bladeDirection == BladeDirection.Left);
            }
        }
        private void OnTriggerStay(Collider other)
        {
            // if (!IsActive) return;
            if (other.gameObject.TryGetComponent(out IHealth _) || other.gameObject.layer == LayerMask.NameToLayer("Sliceable"))
            {
                if (other.CompareTag("NonHitbox"))
                {
                    // DeflectBullet(other);
                    return;
                }
                OnBladeTriggerStay.Invoke(other, bladeDirection == BladeDirection.Left);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // if (!IsActive) return;
            if (other.gameObject.TryGetComponent(out IHealth _) || other.gameObject.layer == LayerMask.NameToLayer("Sliceable"))
            {
                if (other.CompareTag("NonHitbox"))
                {
                    // DeflectBullet(other);
                    return;
                }
                OnBladeTriggerExit.Invoke(other, bladeDirection == BladeDirection.Left);
            }
        }
    }
}