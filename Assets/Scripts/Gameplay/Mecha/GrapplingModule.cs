using System;
using System.Collections.Generic;
using Cinemachine;
using ScriptableObjects.GameParameters;
using UI;
using UI.HUD;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Components;
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
        [SerializeField] private JuggernautParameters juggernautParameters;
        
        [Header("Rope Parameters")]
        [SerializeField] private int quality = 100;
        [SerializeField] private float waveHeight = 2f;
        [SerializeField] private int waveCount;
        [SerializeField] AnimationCurve effectCurve;
        [SerializeField] private float strength = 600f;
        [SerializeField] private float damper = 14f;
        
        [SerializeField] private float velocity = 15f;

        private float springLowDistance = 0.25f;
        private float springHighDistance = 1.25f;

        [SerializeField] private List<Collider> collidersToIgnore;
        
    
        public GameObject grapplePoint;
        //private float _starting_distance = 0f;

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
            grapplePoint = new GameObject();
            grapplePoint.transform.SetParent(transform);
        }

        private void OnEnable()
        {
            if (!IsSpawned || !HasAuthority)
                return;
            EventManager.AddListener("OnGrapplingThrow", OnGrapplingThrow);
        }
        
        private void OnDisable()
        {
            if (!IsSpawned || !HasAuthority)
                return;
            EventManager.RemoveListener("OnGrapplingThrow", OnGrapplingThrow);
            if (isMainModule)
                EventManager.TriggerEvent("GrapplingModule", new ModuleData
                {
                    name = "GrapplingModule",
                    cooldown = juggernautParameters.grapplingCd,
                    status = ModuleStatus.Disabled,
                });
            
        }

        public override void OnNetworkSpawn()
        {
            if (!IsSpawned || !HasAuthority)
                return;
            OnEnable();
            base.OnNetworkSpawn();
        }

        private bool _isPressed;
        private void OnGrapplingThrow(object arg0)
        {
            _isPressed = (bool) arg0;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (!IsSpawned || !HasAuthority)
            {
                if (_isGrappling)
                    DrawRope();
                return;
            }

            // happens if the target gets destroyed while the player is attached to it
            if (grapplePoint == null)
            {
                grapplePoint = new GameObject();
                StopGrapple();
            }
            if (_isGrappling && _canPull)
            {
                MaintainGrapple();
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
            if (isMainModule) {
                ModuleData data = new ModuleData
                {
                    name = "GrapplingModule",
                    cooldown = juggernautParameters.grapplingCd,
                    status = ModuleStatus.Active,
                };
                EventManager.TriggerEvent("GrapplingModule", data);
                EventManager.TriggerEvent("GrapplingModuleStart", data);
            }
            
            StartGrappleRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void StartGrappleRpc()
        {
            lr.SetPosition(quality, gunTip.position);
            _isGrappling = true;
            RaycastHit hit;
            if (RaycastIgnoringSelf(cam.position, cam.forward, out hit, juggernautParameters.maxGrappleDistance, whatIsGrappleable))
            {
                TransferGrapplePoint(hit.transform, hit.point);
                Invoke(nameof(ExecuteGrapple), grappleDelayTime);
            }
            else
            {
                TransferGrapplePoint(transform, cam.position + cam.forward * maxGrappleDistance);
                Invoke(nameof(StopGrappleFail), grappleDelayTime);
            }
            
            lr.enabled = true;
        }
        
        private void TransferGrapplePoint(Transform targetTransform, Vector3 point)
        {
            grapplePoint.transform.SetParent(targetTransform, true);
            grapplePoint.transform.position = point;
        }

        private SpringJoint _joint;
        private void AddSwing(Vector3 swingPoint)
        {
            _joint = rb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = swingPoint;
        
            float distanceFromPoint = Vector3.Distance(rb.position, swingPoint);
        
            // The distance grapple will try to keep from grapple point.
            _joint.maxDistance = distanceFromPoint * springHighDistance;
            _joint.minDistance = distanceFromPoint * springLowDistance;
        
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
            AddSwing(grapplePoint.transform.position);
            _canPull = true;
            //_starting_distance = Vector3.Distance(rb.position, _grapplePoint);
        }

        private RaycastHit[] _hits = new RaycastHit[8];
        public bool RaycastIgnoringSelf(Vector3 origin, Vector3 direction, out RaycastHit hit, float range, LayerMask layerMask)
        {
            var size = Physics.RaycastNonAlloc(origin, direction, _hits, range, layerMask);
            hit = default;
            float closest = Mathf.Infinity;
            bool found = false;

            for (int i = 0; i < size; i++)
            {
                RaycastHit h = _hits[i];
                if (collidersToIgnore.Contains(h.collider)) continue; // skip self
                if (h.collider.CompareTag("FxTemporaire")) continue; // skip non-grappleable objects

                if (h.distance < closest)
                {
                    closest = h.distance;
                    hit = h;
                    found = true;
                }
            }

            return found;
        }

        private void MaintainGrapple()
        {
            _joint.connectedAnchor = grapplePoint.transform.position;
            _joint.minDistance = MathF.Min(_joint.minDistance, Vector3.Distance(rb.position, grapplePoint.transform.position));
        }

        private void PullBody()
        {
            _recast = true;
            var direction = (grapplePoint.transform.position - rb.position).normalized;
            var distance = Vector3.Distance(rb.position, grapplePoint.transform.position);
            //var multiplier = 10;//Math.Min(1, distance / 2f);
            rb.AddForce(direction * (1000 * juggernautParameters.grapplePullSpeed), ForceMode.Force);

            //var distance_change = Vector3.ClampMagnitude(direction * multiplier * Time.deltaTime, math.max(distance, 0.5f));
            //rb.transform.position = (rb.transform.position + distance_change);
            //rb.AddForce(direction * juggernautParameters.grapplePullSpeed * Time.fixedDeltaTime * 4, ForceMode.VelocityChange);

            _joint.maxDistance = distance; // 0.25f;
            // _joint.maxDistance = math.clamp(math.min(_joint.maxDistance - grapplePullSpeed * Time.fixedDeltaTime, distance), 0.25f, maxGrappleDistance);
            _joint.minDistance = distance * 0.25f; // 0.25f;
        }
        

        public void DrawRope()
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

            var newPoint = Vector3.Lerp(lr.GetPosition(quality), grapplePoint.transform.position, Time.deltaTime * 4f);
            
             var up = Quaternion.LookRotation(grapplePoint.transform.position - gunTip.position).normalized * Vector3.up;
             for (int i = 0; i < quality + 1 ; i++)
             {
                 var delta = i / (float) quality;
                 var offset = up * (waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * _spring.Value * effectCurve.Evaluate(delta));
                 lr.SetPosition(i, Vector3.Lerp(gunTip.position, newPoint, delta) + offset);
             }
        }

        private void StopGrapple(bool success = true)
        {
            var grapplingCooldown = juggernautParameters.grapplingCd;
            if (isMainModule && IsOwner)
            {
                ModuleData data = new ModuleData
                {
                    name = "GrapplingModule",
                    cooldown = success ? grapplingCooldown : grapplingCooldown * 0.2f,
                    status = ModuleStatus.Cooldown,
                };
                EventManager.TriggerEvent("GrapplingModule", data);
                EventManager.TriggerEvent("GrapplingModuleStop", data);
            }
           
            StopGrappleRpc(success);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void StopGrappleRpc(bool success)
        { 
            var grapplingCooldown = juggernautParameters.grapplingCd;
            RemoveSwing();
            _isGrappling = false;
            _grapplingCdTimer = success ? grapplingCooldown : grapplingCooldown * 0.2f;
            _canPull = false;
            _recast = false;
            lr.SetPosition(1, gunTip.position);
            lr.enabled = false;
            TransferGrapplePoint(transform, Vector3.zero);
        }
        
        private void StopGrappleFail()
        {
            StopGrapple(false);
        }
        
        public void SetCamera(CinemachineVirtualCamera vcam)
        {
            cam = vcam.transform;
        }
    }
}
