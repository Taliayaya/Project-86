using System;
using UI;
using UnityEngine;

namespace Gameplay.Mecha
{
    public class GrapplingModule : Module
    {
        [Header("Parameters")] [SerializeField, Tooltip("Uncheck this if there is several module using the same icon (thrown together) to avoid sending multiple time the same message")] private bool isMainModule;
        [Header("References")] [SerializeField]
        private Transform cam;
        [SerializeField] private Rigidbody rb;

        [SerializeField] private LineRenderer lr;
        [SerializeField] private Transform gunTip;
        [SerializeField] private LayerMask whatIsGrappleable;
    
        [Header("Grappling Parameters")] [SerializeField]
        private float maxGrappleDistance = 100f;
        [SerializeField] private float grappleDelayTime = 0.1f;
        [SerializeField] private float grapplePullSpeed = 10f;
        
        [Header("Rope Parameters")]
        [SerializeField] private int quality = 100;
        [SerializeField] private float waveHeight = 2f;
        [SerializeField] private int waveCount;
        [SerializeField] AnimationCurve effectCurve;
        [SerializeField] private float strength = 600f;
        [SerializeField] private float damper = 14f;
        
        [SerializeField] private float velocity = 15f;
        
    
        private Vector3 _grapplePoint;

        [Header("Cooldown")] [SerializeField] private float grapplingCd;
        private float _grapplingCdTimer;
    
        private bool _isGrappling;
        private bool _canPull;
        private bool _recast;

        private Spring _spring;

        private void Awake()
        {
            lr.positionCount = quality + 1;
            _spring = new Spring
            {
                Target = 0f
            };
        }

        private void OnEnable()
        {
            EventManager.AddListener("OnGrapplingThrow", OnGrapplingThrow);
            
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnGrapplingThrow", OnGrapplingThrow);
            if (isMainModule)
                EventManager.TriggerEvent("GrapplingModule", new ModuleData
                {
                    name = "GrapplingModule",
                    cooldown = grapplingCd,
                    status = ModuleStatus.Disabled,
                });
            
        }

        private bool _isPressed;
        private void OnGrapplingThrow(object arg0)
        {
            _isPressed = (bool) arg0;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (_isGrappling && _canPull)
            {
                if (_isPressed)
                    PullBody();
                else if (_recast)
                    StopGrapple();
            }
            else if (_isPressed && !_isGrappling)
                StartGrapple();
        
            
            UpdateCooldown();
            DrawRope();
        }

        private void UpdateCooldown()
        {
            if (_grapplingCdTimer > 0)
                _grapplingCdTimer -= Time.fixedDeltaTime;
        }

        private void StartGrapple()
        {
            if (_grapplingCdTimer > 0)
                return;
            if (isMainModule)
                EventManager.TriggerEvent("GrapplingModule", new ModuleData
                {
                    name = "GrapplingModule",
                    cooldown = grapplingCd,
                    status = ModuleStatus.Active,
                });
            
            lr.SetPosition(quality, gunTip.position);
            _isGrappling = true;
            RaycastHit hit;
            if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
            {
                _grapplePoint = hit.point;
                Invoke(nameof(ExecuteGrapple), grappleDelayTime);
            }
            else
            {
                _grapplePoint = cam.position + cam.forward * maxGrappleDistance;
                Invoke(nameof(StopGrappleFail), grappleDelayTime);
            }
            
            lr.enabled = true;
        }

        private SpringJoint _joint;
        private void AddSwing(Vector3 swingPoint)
        {
            _joint = rb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = swingPoint;
        
            float distanceFromPoint = Vector3.Distance(rb.position, swingPoint);
        
            // The distance grapple will try to keep from grapple point.
            _joint.maxDistance = distanceFromPoint * 1;
            _joint.minDistance = distanceFromPoint * 0.25f;
        
            // Adjust these values to fit needs.
            _joint.spring = 500f;
            _joint.damper = 30f;
            _joint.massScale = 4.5f;
        
        
        }
    
        private void RemoveSwing()
        {
            Destroy(_joint);
        }

        private void ExecuteGrapple()
        {
            AddSwing(_grapplePoint);
            _canPull = true;
        }

        private void PullBody()
        {
            _recast = true;
            var direction = (_grapplePoint - rb.position).normalized;
            rb.AddForce(direction * (1000 * grapplePullSpeed), ForceMode.Force);
            var distance = Vector3.Distance(rb.position, _grapplePoint);
            _joint.maxDistance = distance;
            _joint.minDistance = distance * 0.25f;
        }
        

        private void DrawRope()
        {
            if (!_isGrappling)
            {
                _spring.Reset();
                _spring.Velocity = velocity;
                return;
            }

            _spring.Damper = damper;
            _spring.Strength = strength;
            _spring.Update(Time.deltaTime);

            var newPoint = Vector3.Lerp(lr.GetPosition(quality), _grapplePoint, Time.deltaTime * 4f);
            
             var up = Quaternion.LookRotation(_grapplePoint - gunTip.position).normalized * Vector3.up;
             for (int i = 0; i < quality + 1 ; i++)
             {
                 var delta = i / (float) quality;
                 var offset = up * (waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * _spring.Value * effectCurve.Evaluate(delta));
                 lr.SetPosition(i, Vector3.Lerp(gunTip.position, newPoint, delta) + offset);
             }
             
             
             
        }

        private void StopGrapple(bool success = true)
        {
            if (isMainModule)
                EventManager.TriggerEvent("GrapplingModule", new ModuleData
                {
                    name = "GrapplingModule",
                    cooldown = success ? grapplingCd : grapplingCd * 0.2f,
                    status = ModuleStatus.Cooldown,
                });
            RemoveSwing();
            _isGrappling = false;
            _grapplingCdTimer = success ? grapplingCd : grapplingCd * 0.2f;
            _canPull = false;
            _recast = false;
            lr.SetPosition(1, gunTip.position);
            lr.enabled = false;
        }
        
        private void StopGrappleFail()
        {
            StopGrapple(false);
        }
    }
}
