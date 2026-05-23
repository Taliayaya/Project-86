using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace AI
{
    using UnityEngine;

    public class FallingRock : MonoBehaviour
    {
        [SerializeField] private Rigidbody rock;
        private bool hasFallen = false;

        [SerializeField] private GameObject obstacle;
        [SerializeField] private VisualEffect dust;
        [SerializeField] private float multiplier = 4000;
        
        void Awake()
        {
            TriggerFall();
        }

        public void TriggerFall()
        {
            if (hasFallen) return;

            hasFallen = true;
            rock.isKinematic = false;
            StartCoroutine(Fall()); 
        }

        IEnumerator Fall()
        {
            float y = transform.position.y;
            Vector3 direction = obstacle.transform.position - (rock.transform.position);
            // rock.AddForce(direction * multiplier, ForceMode.Impulse);
            rock.AddTorque(rock.transform.forward * multiplier, ForceMode.VelocityChange);
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => rock.angularVelocity.magnitude < 0.1f);
            obstacle.SetActive(true);
            rock.gameObject.SetActive(false);
            dust.enabled = true;
            dust.Play();
            
        }
        
        void Explode()
        {
            // Ici tu peux instancier des particules de poussière/cailloux
            // et détruire ou désactiver le gros rocher
            Destroy(gameObject, 0.1f);
        }

        public void OnTriggerEnter(Collider other)
        {
            Debug.Log($"OnTriggerEnter with {other.gameObject.name} ({other.attachedRigidbody.tag})");
            if (other.attachedRigidbody != null)
            {
                if (other.attachedRigidbody.gameObject.CompareTag("Morpho"))
                {
                    var agent = other.attachedRigidbody.GetComponent<BehaviorGraphAgent>();
                    agent.SetVariableValue("CruiseMode", MorphoCruiseMode.Braking);
                    agent.SetVariableValue("Target", obstacle.transform);
                }
            }
        }

        public void OnGrapplePull()
        {
            Debug.Log("OnGrapplePull");
            TriggerFall();
        }
    }
}