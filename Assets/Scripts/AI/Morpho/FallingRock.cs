using System.Collections;
using Unity.Netcode;
using UnityEngine.VFX;

namespace AI
{
    using UnityEngine;

    public class FallingRock : NetworkBehaviour
    {
        [SerializeField] private Rigidbody rock;
        private bool hasFallen = false;

        [SerializeField] private GameObject obstacle;
        [SerializeField] private VisualEffect dust;
        [SerializeField] private float multiplier = 4000;

        [SerializeField] private MorphoObstacleChannel morphoObstacleChannel;
        [SerializeField] private bool fallOnAwake;

        void Awake()
        {
            if (fallOnAwake)
                FallLocal();
        }

        public void TriggerFall()
        {
            if (hasFallen) return;
            TriggerFallRpc();
        }

        // the grapple pull only happens on the pulling player's client;
        // the rock has to fall for everyone
        [Rpc(SendTo.Everyone)]
        private void TriggerFallRpc() => FallLocal();

        // ponytail: fall physics simulates locally on every client, same accepted
        // simplification as CollapsePoint — the obstacle's final pose may differ slightly
        private void FallLocal()
        {
            if (hasFallen) return;

            hasFallen = true;
            rock.isKinematic = false;
            StartCoroutine(Fall());
        }

        IEnumerator Fall()
        {
            rock.AddTorque(rock.transform.forward * multiplier, ForceMode.VelocityChange);
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => rock.angularVelocity.magnitude < 0.1f);
            obstacle.SetActive(true);
            rock.gameObject.SetActive(false);
            dust.enabled = true;
            dust.Play();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null)
            {
                if (other.attachedRigidbody.gameObject.CompareTag("Morpho"))
                {
                    morphoObstacleChannel.SendEventMessage(obstacle.transform);
                }
            }
        }

        public void OnGrapplePull()
        {
            TriggerFall();
        }
    }
}
