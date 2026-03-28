using System.Collections;
using UnityEngine;

namespace Animations
{
    public class ShellSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject shellPrefab;
        [SerializeField] private Transform shellSpawnPoint;
        [SerializeField] private float shellEjectForceZ;
        [SerializeField] private float shellEjectForceY;
        [SerializeField] private float shellEjectTorqueForce;
        [SerializeField] private float shellLifetime = 5;
        [SerializeField] private float shellAlignRotationStrength = 5;
        [SerializeField] private float shellAlignThreshold = 1;
        
        public void Eject()
        {
            Debug.Log("Ejecting shell");
            var shell = Instantiate(shellPrefab, shellSpawnPoint.position, shellSpawnPoint.rotation);
            Rigidbody rb = shell.GetComponent<Rigidbody>();
            shell.SetActive(true);
            
            rb.AddForce(shellSpawnPoint.forward * shellEjectForceZ, ForceMode.Impulse);
            
            // Add random torque for spinning
            Vector3 randomTorque = Random.insideUnitSphere * shellEjectTorqueForce;
            rb.AddTorque(randomTorque, ForceMode.Impulse);

            StartCoroutine(EjectCoroutine(rb));
            
            Destroy(shell, shellLifetime);
        }

        IEnumerator EjectCoroutine(Rigidbody rb)
        {
            while (rb != null)
            {
                if (rb.linearVelocity.sqrMagnitude < shellAlignThreshold)
                {
                    yield return new WaitForFixedUpdate();
                    continue;
                }

                Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity.normalized, transform.up);

                // Compute rotation difference
                Quaternion delta = targetRotation * Quaternion.Inverse(rb.rotation);
                delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
                if (angleDeg > 180f) angleDeg -= 360f;

                // Apply torque proportional to angle difference
                Vector3 torque = axis.normalized * (angleDeg * Mathf.Deg2Rad * shellAlignRotationStrength); 
                rb.AddTorque(torque, ForceMode.Acceleration);

                yield return new WaitForFixedUpdate();
            }
        }
    }
}