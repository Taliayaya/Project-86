using Gameplay;
using UnityEngine;
using UnityEngine.Events;
namespace BladesCombat
{
    public class BladeCollisionTrigger : MonoBehaviour
    {
        
        public bool IsLeftBlade;

        public UnityEvent<Collider, bool> OnBladeTriggerEnter { get; } = new UnityEvent<Collider, bool>();
        public UnityEvent<Collider, bool> OnBladeTriggerStay { get; } = new UnityEvent<Collider, bool>();
        public UnityEvent<Collider, bool> OnBladeTriggerExit { get; } = new UnityEvent<Collider, bool>();
        public bool IsActive { get; set; } = false;

        private void OnTriggerEnter(Collider other)
        {
            // if (!IsActive) return;
            
            if (other.gameObject.TryGetComponent(out IHealth _) || other.gameObject.layer == LayerMask.NameToLayer("Slicable"))
            {
                if (other.CompareTag("NonHitbox"))
                {
                    // DeflectBullet(other);
                    return;
                }
            }
            OnBladeTriggerEnter.Invoke(other, IsLeftBlade);
        }
        private void OnTriggerStay(Collider other)
        {
            // if (!IsActive) return;
            if (other.gameObject.TryGetComponent(out IHealth _) || other.gameObject.layer == LayerMask.NameToLayer("Slicable"))
            {
                if (other.CompareTag("NonHitbox"))
                {
                    // DeflectBullet(other);
                    return;
                }
            }

            OnBladeTriggerStay.Invoke(other, IsLeftBlade);
        }

        private void OnTriggerExit(Collider other)
        {
            // if (!IsActive) return;
            if (other.gameObject.TryGetComponent(out IHealth _) || other.gameObject.layer == LayerMask.NameToLayer("Slicable"))
            {
                if (other.CompareTag("NonHitbox"))
                {
                    // DeflectBullet(other);
                    return;
                }
                OnBladeTriggerExit.Invoke(other, IsLeftBlade);
            }
        }
    }
}