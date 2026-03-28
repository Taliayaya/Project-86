using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace Gameplay.Mecha
{
    public class RocketController : MonoBehaviour
    {
        
        [Header("References")]
        [SerializeField] private Rigidbody rb;

        [SerializeField] private Transform model;
        [SerializeField] private VisualEffect explosion;
        [SerializeField] private Collider hitbox;
        
        [Header("Launch Parameters")]
        public AnimationCurve altitudeByDistanceCurve;
        public float launchSpeed = 100f;
        public float launchDuration = 1f;
        public float redirectionDuration = 3f;
        public float redirectionSpeed = 10f;

        [SerializeField] private UnityEvent onExplode;
        
        private void Awake()
        {
            if (!rb)
                rb = GetComponent<Rigidbody>();
        }
        public float GetDesiredAltitude(Vector3 origin, float distance)
        {
            float desiredAltitude = altitudeByDistanceCurve.Evaluate(distance);
            desiredAltitude -= transform.position.y - origin.y;
            if (desiredAltitude < 0)
                return 0;
            return desiredAltitude;
            
        }
        private Vector3 CalculateLaunchVelocity(Vector3 origin, Vector3 targetPos, float desiredAltitude)
        {
            float gravity = Physics.gravity.y;
            float displacementY = targetPos.y - origin.y;
            Vector3 displacementXZ = new Vector3(targetPos.x - origin.x, 0, targetPos.z - origin.z);
            
            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * desiredAltitude);
            Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * desiredAltitude / gravity) +
                                                   Mathf.Sqrt(2 * (displacementY - desiredAltitude) / gravity));
            return velocityXZ + velocityY;
        } 
        
        public void Launch(Vector3 launchDirection, Vector3 target)
        {
            Activate();
            StartCoroutine(LaunchCoroutine(launchDirection, target));
        }

        IEnumerator LaunchCoroutine(Vector3 launchDirection, Vector3 target)
        {
            Vector3 launchOrigin = transform.position;
            rb.AddForce(launchDirection * launchSpeed, ForceMode.VelocityChange);
            yield return new WaitForSeconds(launchDuration);
            yield return new WaitForFixedUpdate();
            hitbox.enabled = true;
            
            float timeRedirectionStart = Time.fixedTime;
            float desiredAltitude = GetDesiredAltitude(launchOrigin, Vector3.Distance(transform.position, target));
            var targetDirection = CalculateLaunchVelocity(transform.position, target, desiredAltitude);
            while (Time.fixedTime < timeRedirectionStart + redirectionDuration)
            {
                float timeElapsed = Time.fixedTime - timeRedirectionStart;
                float blendFactor = Mathf.Clamp01(timeElapsed / redirectionDuration);

                Vector3 desiredVelocity = Vector3.Lerp(rb.linearVelocity, targetDirection, blendFactor);
        
                rb.linearVelocity = Vector3.MoveTowards(
                    rb.linearVelocity,
                    desiredVelocity,
                    redirectionSpeed * Time.fixedDeltaTime 
                );
                // desiredAltitude = GetDesiredAltitude(launchOrigin, Vector3.Distance(transform.position, target));
                // var targetDirection = CalculateLaunchVelocity(transform.position, target, desiredAltitude);
                // rb.AddForce(targetDirection * redirectionSpeed, ForceMode.Force);
                yield return new WaitForFixedUpdate();
            }

            // ignore the altitude already climbed
            desiredAltitude = GetDesiredAltitude(launchOrigin, Vector3.Distance(transform.position, target));
            
            // calculate the velocity remaining to go straight to the target
            Vector3 velocity = CalculateLaunchVelocity(transform.position, target, desiredAltitude);
            rb.linearVelocity = velocity;
        }

        private bool _hasHit;
        private void OnCollisionEnter(Collision other)
        {
            if (_hasHit) return;
            _hasHit = true;
            DeActivate();
            
            explosion.enabled = true;
            explosion.transform.rotation = Quaternion.FromToRotation(Vector3.up, other.contacts[0].normal);
            explosion.Play();
            
            onExplode?.Invoke();
        }
        
        public void Activate()
        {
            _hasHit = false;
            model.gameObject.SetActive(true);
            hitbox.enabled = false;
            explosion.enabled = false;
            rb.constraints = RigidbodyConstraints.None;
        }
        
        private void DeActivate()
        {
            rb.linearVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            model.gameObject.SetActive(false);
        }
    }
}