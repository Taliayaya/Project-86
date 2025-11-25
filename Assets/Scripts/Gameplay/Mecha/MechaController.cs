using System;
using System.Collections;
using Armament.Shared;
using Cinemachine;
using Gameplay.Units;
using Networking;
using Networking.Widgets.Session.Session;
using NUnit.Framework;
using ScriptableObjects;
using ScriptableObjects.GameParameters;
using UI.HUD;
using UnityEngine.Events;
using UnityEngine.Serialization;

using UnityEngine;
using Unity.VisualScripting;
using Unity.Mathematics;
using Unity.Services.Authentication;
using UnityEngine.Rendering.Universal;
using static UnityEngine.LightAnchor;
using UnityEngine.InputSystem;


namespace Gameplay.Mecha
{
    [RequireComponent(typeof(Rigidbody))]
    public class MechaController : Gameplay.Units.Unit
    {
        public enum Zoom
        {
            Default,
            X2,
            X4,
            X8,
        }

        public enum View
        {
            FirstPerson,
            ThirdPerson,
            FreeLook
        }




        #region Serialized Fields

        public JuggernautParameters juggernautParameters;
        [SerializeField] private float gravity = -9.81f;

        [Header("Movement")] 
        [SerializeField] private float groundDrag;
        //[SerializeField] private float groundCleareance = 1.5f;
        [SerializeField] private float airborneHeight = 2.2f;
        [SerializeField] private float gripLossAngle = 20f;
        [SerializeField] private float maxFloorAngle = 40f;
        [SerializeField] private float fallGravityMult = 2.5f;
        [SerializeField] private float lowJumpGravityMult = 2f;
        [SerializeField] private float wallFallingMult = 2.5f;
        [SerializeField] private float dashingMaxUpwardSpeed = 2.5f;
        [SerializeField] private bool allowMoveOnStart = true;
        [SerializeField] public GrapplingModule grappleModule = null;

        [SerializeField]
        private Transform modelTransform;
        public bool canRotate = true;
        [SerializeField] private Camera zoomCamera;
        [SerializeField] private Camera tpsZoomCamera;
        [SerializeField] private CinemachineVirtualCamera vCamera;
        [SerializeField] private CinemachineVirtualCamera tpsCamera;
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        [SerializeField] private LayerMask forwardMask;

        [Header("Main Recoil")]
        [SerializeField] private float recoilStrength = 1f;
        [SerializeField] private float recoilDuration = 0.1f;
        [SerializeField] private float recoilCounterForce = 1f;


        [Header("Events")] [SerializeField] private UnityEvent<CinemachineVirtualCamera> onCameraViewChanged;
        #endregion

        #region Private Fields

        private Vector2 _lastMouseUpdate;
        private Rigidbody _rigidbody;
        private bool _isGrounded;
        public override bool IsGrounded => _isGrounded;
        private bool _isOnWall;

        private Vector2 _lastMovement;

        private float _xRotation;

        private float _yRotation;
        private float _yVelocity;

        private Zoom _zoom = Zoom.Default;
        private bool _isRunning;
        // Fox's var's unorganised

        private bool isDashing = false;
        private bool isJumping = false;
        private bool _isGrappling = false;
        private bool canDash = true;
        private bool isAirborne = false;
        //private bool canGrapple = true;
        private Coroutine dashCoroutine = null;

        private bool canJump = true;

        // instead of using transform.up, use surfaceAlignment.upDirection, it's less erratic since its lerping value is much lower (Nova)
        private Ray groundRay;
        private RaycastHit groundHitData;
        private Vector3 rotationLeft = Vector3.zero;
        private float floorAngle = 0f;
        private bool _isInclineStable = true;
        private SurfaceNormalAlignment surfaceAlignment;
        private Vector3 grappleAttachPoint = Vector3.zero;

        #endregion

        #region Properties

        private View _cameraView = View.FirstPerson;
        private View _cameraPreviousView = View.FirstPerson;
        public View CameraView
        {
            get => _cameraView;
            set
            {
                if (_cameraView == value)
                    return;
                _cameraPreviousView = _cameraView;
                _cameraView = value;
                vCamera.gameObject.SetActive(value == View.FirstPerson);
                zoomCamera.gameObject.SetActive(value == View.FirstPerson);
                freeLookCamera.gameObject.SetActive(value == View.FreeLook);
                tpsCamera.gameObject.SetActive(value == View.ThirdPerson);
                tpsZoomCamera.gameObject.SetActive(value == View.ThirdPerson);
                
                onCameraViewChanged?.Invoke(value == View.FirstPerson ? vCamera : tpsCamera);
                CameraZoom = Zoom.Default;
                EventManager.TriggerEvent(Constants.TypedEvents.OnToggleCockpitView, value == View.FirstPerson && juggernautParameters.toggleCockpitView);
                
            }

        }

        // public float MovementSpeed => _isRunning ? juggernautParameters.runSpeed : juggernautParameters.walkSpeed;
        public enum MovementMode // basically the equivalant of "modes"
        {
            Walking,
            Running,
            Slowed, // terrain difficulties
            Dashing
            // potentially DamagedWalking (and equivalant for other types)

            // Enum is a read only
        }


        private float _slowStrength;
        private float _slowDuration;
        public float MovementSpeed { get; private set; }
        private MovementMode _movementmode;

        public MovementMode CurrentMovementMode
        {
            get => _movementmode;
            set
            {
                Console.WriteLine($"Setting movement mode to: {value}");
                _movementmode = value; // Set the new value to the private backing field first
                switch (value) // <-- Use "value" here instead of "_movementmode"
                {
                    case MovementMode.Walking:
                        MovementSpeed = juggernautParameters.walkSpeed;
                        break;
                    case MovementMode.Running :
                        MovementSpeed = juggernautParameters.runSpeed;
                        break;
                    case MovementMode.Slowed:
                        MovementSpeed = _slowStrength; // juggernautParameters.slowSpeed
                        break;
                    case MovementMode.Dashing:
                        MovementSpeed = juggernautParameters.dashSpeed; // made it useful, just a small tweak
                        break;
                }
            }
        }
        


        private Zoom CameraZoom
        {
            get => _zoom;
            set
            {
                Camera zoomCameraUsed = CameraView == View.FirstPerson ? zoomCamera : tpsZoomCamera;
                _zoom = value;
                float zoomValue;
                switch (_zoom)
                {
                    case Zoom.Default:
                        zoomValue = 60;
                        zoomCameraUsed.gameObject.SetActive(false);
                        zoomCameraUsed.enabled = false;
                        break;
                    case Zoom.X2:
                        zoomCameraUsed.enabled = true;
                        zoomCameraUsed.gameObject.SetActive(true);
                        zoomValue = 30;
                        break;
                    case Zoom.X4:
                        zoomValue = 15;
                        break;
                    case Zoom.X8:
                        zoomValue = 7.5f;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                zoomCameraUsed.fieldOfView = zoomValue;
                EventManager.TriggerEvent("OnZoomChange", _zoom);
            }
        } 
        
        #endregion

        
        public override UnitState State
        {
            get => _state;
            set
            {
                _state = value;
                EventManager.TriggerEvent("OnMechaStateChange", _state);
            }
        }
        
        // Start is called before the first frame update

        #region Unity Callbacks

        public override void Awake()
        {
            base.Awake();
            Health = juggernautParameters.health;
            MaxHealth = juggernautParameters.health;
            _rigidbody = GetComponent<Rigidbody>();
            vCamera.gameObject.SetActive(IsOwner);
            _xRotation = modelTransform.localEulerAngles.x;
            if (!IsOwner)
            {
                return;
            }
            EventManager.TriggerEvent("OnUpdateHealth", 1f);
            EventManager.TriggerEvent(Constants.TypedEvents.OnToggleCockpitView, juggernautParameters.toggleCockpitView);
            PlayerManager.Player = this;
        }

        public void ReInit()
        {
            Debug.Log("ReInit");
            if (IsOwner)
            {
                EventManager.TriggerEvent("OnUpdateHealth", 1f);
                EventManager.TriggerEvent(Constants.TypedEvents.OnToggleCockpitView,
                    juggernautParameters.toggleCockpitView);
                PlayerManager.Player = this;
            }

            vCamera.gameObject.SetActive(IsOwner);
            OnDisable();
            OnEnable();
        }
        protected override void OnEnable()
        {
            if (!IsOwner)
                return;
            
            // these events are local
            base.OnEnable();
            if (!_rigidbody)
                _rigidbody = GetComponent<Rigidbody>();
            EventManager.AddListener(Constants.TypedEvents.Inputs.OnLookAround, OnLookAround);
            EventManager.AddListener("OnMove", OnMove);
            //EventManager.AddListener(Constants.TypedEvents.Inputs.OnDash, OnDash);
            EventManager.AddListener(Constants.TypedEvents.Inputs.OnJump, OnJump);
            EventManager.AddListener("OnZoomIn", OnZoomIn);
            EventManager.AddListener("OnZoomOut", OnZoomOut);
            EventManager.AddListener("OnRun", OnRun);
            EventManager.AddListener("OnShoot:Primary", OnShoot);
            EventManager.AddListener(Constants.TypedEvents.Inputs.OnFreeLook, OnFreeLook);
            EventManager.AddListener(Constants.Events.Inputs.OnChangeView, OnChangeView);
            EventManager.AddListener("GrapplingModuleStart", OnGrappleStart);
            EventManager.AddListener("GrapplingModuleStop", OnGrappleStop);
        }


        protected override void OnDisable()
        {
            if (!IsOwner)
                return;
            // these events are local
            base.OnDisable();
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.OnLookAround, OnLookAround);
            EventManager.RemoveListener("OnMove", OnMove);
            //EventManager.RemoveListener("OnDash", OnDash);
            EventManager.RemoveListener("OnZoomIn", OnZoomIn);
            EventManager.RemoveListener("OnZoomOut", OnZoomOut);
            EventManager.RemoveListener("OnRun", OnRun);
            EventManager.RemoveListener("OnShoot:Primary", OnShoot);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.OnFreeLook, OnFreeLook);
            EventManager.RemoveListener(Constants.Events.Inputs.OnChangeView, OnChangeView);
            EventManager.RemoveListener("GrapplingModule", OnGrappleStart);
            EventManager.RemoveListener("GrapplingModule", OnGrappleStop);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!HasAuthority)
            {
                // if not owner, this component is "useless"
                // everything will be synchronized by the server
                // -- 
                // nevermind we need it active to listen to RPC
                //enabled = false;
                return;
            }
            ReInit();
        }

        protected override void FixedUpdate()
        {
            if (!HasAuthority || !IsSpawned)
            {
                return;
            }

            base.FixedUpdate();
            CheckGround(false);
            ApplyGravity();
            LimitSpeed();
            CheckDistanceForward();
            CheckGrappleSurfaceAlignment();

            if (State == UnitState.Default)
            {
                RotateJuggernaut();
                MoveJuggernaut();

            }
        }

        protected override void Start()
        {
            base.Start();
            if (!IsOwner)
                return;
            Debug.Log("[MechaController] Start");
            EventManager.TriggerEvent("RegisterMinimapTarget", transform);
            Cursor.lockState = CursorLockMode.Locked;
            groundRay = new Ray(transform.position, Vector3.down);
            surfaceAlignment = GetComponent<SurfaceNormalAlignment>();
            surfaceAlignment.discardAngle = maxFloorAngle;
            if (allowMoveOnStart) { _movementmode = MovementMode.Walking; }
            StartCoroutine(LandingCoroutine());

        }

        #endregion

        #region Movement and Camera

        private void CheckGrappleSurfaceAlignment() { 
            if (_isGrappling ) { surfaceAlignment.useRawNormal = false; } // && isAirborne
            else if (!_isGrappling){ surfaceAlignment.useRawNormal = false;}
        }


        private void CheckDistanceForward()
        {
            if (Physics.Raycast(vCamera.transform.position, vCamera.transform.forward, out var hit, 5000, forwardMask))
            {
                EventManager.TriggerEvent("OnDistanceForward", hit.distance);
            }
            else
                EventManager.TriggerEvent("OnDistanceForward", float.NaN);
        }
        
        public void CheckGround(bool isGrounded)
        {

            _isGrounded = surfaceAlignment.groundDistance <= airborneHeight;
            _isInclineStable = surfaceAlignment.floorAngle < maxFloorAngle;

            if (_isGrounded)
            {
                _rigidbody.linearDamping = groundDrag;
                _isOnWall = Vector3.Dot(transform.up, Vector3.up) < 0.8f;
                if (_isOnWall && !_isGrappling)
                    _isGrounded = false;
            }
            else
                _rigidbody.linearDamping = 0.3f;
        }

        private void MoveJuggernaut()
        {
            var move = transform.forward * (_lastMovement.y) + transform.right * (_lastMovement.x);
            move = move.normalized;
            bool isMovingUp = Vector3.Dot(Vector3.up, move) > 0;
            // gradually decreses speed once GripLossAngle is reached, completely stopping at maxFloorAngle
            float floorAngleMultiplier = math.clamp((maxFloorAngle - floorAngle) / gripLossAngle, 0, 1);

            // Wall walking allowance code
            

            if (!_isGrappling) // if isn't grappling
            {
                if (!_isGrounded || (!_isInclineStable && isMovingUp)) { return; }
                if (isDashing) { move *= 0.5f; }
            }
            else if (_isOnWall && _isGrappling) { // if is grappling
                

                Vector3 grapplePoint = grappleModule.grapplePoint.transform.position;
                float distance = Vector3.Distance(grapplePoint, transform.position);
                float dot = Vector3.Dot(Vector3.up, (grapplePoint - transform.position).normalized);

                

                if (dot < 0.85f && distance > 10f) {
                    move *= MathF.Max(0, dot / 2f);
                    Debug.Log("HARD GRAPPLE");
                    Debug.Log(dot);
                }

                floorAngleMultiplier = 0.75f;
                //var surfaceAlignedVector = Vector3.ProjectOnPlane(move + Vector3.up * (_lastMovement.y), surfaceAlignment.rawNormal).normalized;
                //Debug.Log(surfaceAlignment.rawNormal);
                //move = surfaceAlignedVector;
                //move = ((transform.forward * 0.5f + Vector3.up * 3) * (_lastMovement.y) + transform.right * _lastMovement.x).normalized;
            }
            // if not grounded while grappling, don't move
            else if (!_isGrounded)
                return;
            
            
            //if (_isGrappling) { floorAngleMultiplier = 0.5};
            
            _rigidbody.AddForce(move * (MovementSpeed * 1000f * floorAngleMultiplier), UnityEngine.ForceMode.Force);
            
        }


        private void ApplyGravity()
        {
            float excessGravity = 0f;
            // Debug.Log("ApplyGravity " + _isGrounded + " " + _isOnWall + " " + _isGrappling + " " + _rigidbody.linearVelocity.y + "");
            if (_isGrounded) excessGravity = gravity;
            else if (_isOnWall && !_isGrappling) excessGravity = gravity * (wallFallingMult - 1);
            else if (_rigidbody.linearVelocity.y < 0) { excessGravity += gravity * (fallGravityMult - 1f); }
            else if (_isGrappling) excessGravity += gravity * (fallGravityMult - 1);
            else if (!isJumping && _rigidbody.linearVelocity.y > 0) { excessGravity += gravity * (lowJumpGravityMult - 1f);  }
            //Debug.Log(_rigidbody.linearVelocity.y);
            _rigidbody.AddForce(Vector3.up * excessGravity, UnityEngine.ForceMode.Acceleration);

            /*
            _yVelocity += gravity;// * gravity; //* Time.fixedDeltaTime; 
            if (_isGrounded && _isInclineStable) 
                _yVelocity = 0;
            _rigidbody.AddForce(Vector3.up * _yVelocity, UnityEngine.ForceMode.Acceleration);
            _yVelocity = 0;
            */
        }

        private void RotateJuggernaut()
        {
            // model is rotated up/down, while the main game object is rotated left/right, otherwise tilting the player to match ground angle wouldn't work
            modelTransform.localRotation = Quaternion.AngleAxis(_xRotation, Vector3.right); // up/down rotation
            transform.Rotate(rotationLeft); // left/right rotation
            rotationLeft = Vector3.zero;
        }

        private void LimitSpeed()
        {
            var flatVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            if (flatVel.magnitude > MovementSpeed)
            {
                var limitedVel = flatVel.normalized * MovementSpeed;
                _rigidbody.linearVelocity = new Vector3(limitedVel.x, _rigidbody.linearVelocity.y, limitedVel.z);
            }
        }
        
        public void CanMove(bool canMove)
        {
            if (!_rigidbody)
                _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = canMove ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;
        }

        
        #endregion
        
        #region Input Callbacks
        
        public const float MinXRotation = -50f;
        public const float MaxXRotation = 50f;
        

        private void OnLookAround(object data)
        {
            if (data is not Vector2 pos || !canRotate)
                return;
            
            float sensitivity = CameraZoom <= Zoom.X2 ? juggernautParameters.MouseSensitivity : juggernautParameters.MouseZoomSensitivity;
            _xRotation -= pos.y * sensitivity * Time.fixedDeltaTime;
            _yRotation += pos.x * sensitivity * Time.fixedDeltaTime;

            var _yRotationChange = pos.x * sensitivity * Time.fixedDeltaTime;
            rotationLeft += surfaceAlignment.upDirection * _yRotationChange; 

            EventManager.TriggerEvent("OnUpdateCompass", _yRotation);

            _xRotation = Mathf.Clamp(_xRotation, MinXRotation, MaxXRotation);
            EventManager.TriggerEvent("OnUpdateXRotation", _xRotation);
        }
        

        private void OnFreeLook(object data)
        {
            if (data is not bool on)
                return;
            canRotate = !on;
            CameraView = on ? View.FreeLook : _cameraPreviousView;

        }
        
        private void OnChangeView()
        {
            // swap between third and first person
            switch (CameraView)
            {
                case View.FirstPerson:
                    CameraView = View.ThirdPerson;
                    break;
                case View.ThirdPerson:
                    CameraView = View.FirstPerson;
                    break;
                case View.FreeLook:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        // Coroutines for these two scripts are not organised yet, jump / dash.
        private void OnJump(object data)
        {
            //Debug.Log("Jump attempted");
            if (canJump) {
                StartCoroutine(JumpCoroutine()); 
            }
        }

        private IEnumerator LandingCoroutine()
        {
            var landingVelocity = Vector3.zero;

            while (true)
            {
                if (!_isGrounded) { isAirborne = true; }
                if (isAirborne && _isGrounded)
                {
                    _rigidbody.linearVelocity = Vector3.ProjectOnPlane(landingVelocity, surfaceAlignment.normal);
                    isAirborne = false;
                }
                landingVelocity = _rigidbody.linearVelocity;
                yield return null;

            }
        }
        private IEnumerator JumpCoroutine()
        {
            if(!canJump || !_isGrounded)
                yield break;

            _rigidbody.AddForce(Vector3.up * juggernautParameters.jumpPower, UnityEngine.ForceMode.Acceleration);

            // jump is now on cooldown
            canJump = false;
            isJumping = true;

            float elapsedTime = 0.0f;
            //float jumpTime = 0.5f;

            while (isJumping)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                
                isJumping = Input.GetKey("space");
                if (!isJumping || elapsedTime > juggernautParameters.maxJumpDuration)
                {
                    break;
                }
            }

            canJump = true;
            isJumping = false;
        }
        
        private void OnMove(object data)
        {
            if (data is not Vector2 movement)
                return;
            _lastMovement = movement;
            // if moving slowly / not moving then run is off
            if (movement.magnitude < 0.1f)
                CurrentMovementMode = MovementMode.Walking;
        }
        private void OnDash(object data)
        {
            if (data is not bool dashInput || !dashInput)
                return;

            StartCoroutine(DashCoroutine());
        }
        private void OnGrappleStart(object data)
        {
            _isGrappling = true;
            //surfaceAlignment.useRawNormal = true;
        }
        private void OnGrappleStop(object data)
        {
            _isGrappling = false;
            //surfaceAlignment.useRawNormal = false;
        }

        public float test = 1000f;

        private IEnumerator DashCoroutine()
        {
            if (!canDash || isDashing || !_isGrounded)
                yield break;

            EventManager.TriggerEvent(Constants.TypedEvents.OnDash,
                new ModuleData()
                {
                    name = "Dash",
                    cooldown = juggernautParameters.dashCooldown,
                    status = ModuleStatus.Active
                });

            // Set the flag for dashing and block further dashes for now
            isDashing = true;
            canDash = false;
            MovementMode previousMovementMode = CurrentMovementMode;
            CurrentMovementMode = MovementMode.Dashing;

            // Disable any changes in movement speed during dash
            //float dashSpeed = MovementSpeed;
            float dashSpeed = juggernautParameters.dashSpeed;

            Vector3 dashDirection = (_rigidbody.transform.forward * _lastMovement.y) +
                                    (_rigidbody.transform.right * _lastMovement.x);
            dashDirection.Normalize();

            // Store the initial velocity of the Mecha to make sure we're overriding it
            Vector3 initialVelocity = _rigidbody.linearVelocity;

            // Time management for smooth deceleration
            float elapsedTime = 0f;

            while (elapsedTime < juggernautParameters.dashDuration && _isGrounded)
            {
                // Best reccomendation for elapsedtime..
                float t = elapsedTime / juggernautParameters.dashDuration;

                // Gradually reduce speed using a smooth easing function
                //float currentSpeed = Mathf.Lerp(dashSpeed, 30f, EaseOutQuad(t));  // Lerp from full speed to 0, will freeze you momentarily


                Vector3 DashVector = dashDirection * (juggernautParameters.dashSpeed * math.max(1f-t, 0.2f));
                //_rigidbody.AddForce(Vector3.down * test, UnityEngine.ForceMode.Acceleration);
                //Debug.Log(($"Y value: {currentVelocity}"));

                // Velocity based on current speed
                //_rigidbody.linearVelocity += dashDirection * currentSpeed * Time.deltaTime;
                _rigidbody.AddForce(DashVector * Time.deltaTime, ForceMode.VelocityChange);
                if (_rigidbody.linearVelocity.y > dashingMaxUpwardSpeed)
                    _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, dashingMaxUpwardSpeed, _rigidbody.linearVelocity.z);

                elapsedTime += Time.deltaTime;  
                yield return null;  
            }

            // Reset after dash is finished
            isDashing = false;

            // Ensure that we return to walking or running after the dash
            CurrentMovementMode = previousMovementMode;  // Only using walk then run since energy cost

            EventManager.TriggerEvent(Constants.TypedEvents.OnDash,
                new ModuleData()
                {
                    name = "Dash",
                    cooldown = juggernautParameters.dashCooldown,
                    status = ModuleStatus.Cooldown
                });
            // Wait for cooldown before allowing another dash
            yield return new WaitForSeconds(juggernautParameters.dashCooldown);
            canDash = true;
        }


        // deacceleration was just kind of messing around tbh
        private float EaseOutQuad(float t)
        {
            return t * (2 - t);  
        }



        private bool _zoomCd = false;
        [SerializeField] private UnitState _state;

        private void OnZoomIn(object data)
        {
            if (data is not float mouseScrollY || _zoomCd)
                return;
            if (CameraZoom != Zoom.X8)
                CameraZoom++;
            _zoomCd = true;
            Invoke(nameof(ResetZoomCd), juggernautParameters.scrollSensitivity / 100);
        }

        private void OnZoomOut(object data)
        {
            if (data is not float mouseScrollY || _zoomCd)
                return;
            if (CameraZoom != Zoom.Default)
                CameraZoom--;
            _zoomCd = true;
            Invoke(nameof(ResetZoomCd), juggernautParameters.scrollSensitivity / 100);

        }

        private void OnRun(object data)
        {
            if (data is not bool run || CurrentMovementMode == MovementMode.Dashing)
                return;
            CurrentMovementMode = MovementMode.Running;
            StartCoroutine(DashCoroutine());
        }


        #endregion

        private void ResetZoomCd()
        {
            _zoomCd = false;
        }

        #region Health Manager

        public override void TakeSlowEffect(DamagePackage damagePackage)
        {
            _slowStrength = damagePackage.Slow.Strength;
            _slowDuration = damagePackage.Slow.Duration;
            // CurrentMovementMode = MovementMode.Slowed;
            StopCoroutine(nameof(SlowDownCoroutine));
            StartCoroutine(nameof(SlowDownCoroutine));
        }

        private IEnumerator SlowDownCoroutine()
        {
            while (_slowDuration > 0)
            {
                float targetSpeed = CurrentMovementMode == MovementMode.Running ? juggernautParameters.runSpeed : juggernautParameters.walkSpeed;

                MovementSpeed = targetSpeed / _slowStrength;
                yield return new WaitForFixedUpdate();
                _slowDuration -= Time.fixedDeltaTime;
            }
        }

        public override void OnTakeDamage(DamagePackage damagePackage)
        {
            base.OnTakeDamage(damagePackage);
            if (!IsOwner)
                return;
            EventManager.TriggerEvent("OnTakeDamage", Health);
            EventManager.TriggerEvent("OnUpdateHealth", Health/MaxHealth);
        }

        [SerializeField] private GameObject deathPrefab;
        public override void Die()
        {
            if (Died)
                return;
            base.Die();
            var deathData = new DeathData
            {
                DeathPosition = transform.position,
                Faction = Faction
            };
            var dead = Instantiate(deathPrefab, transform.position, transform.rotation);
            dead.transform.GetChild(0).rotation = transform.GetChild(0).transform.rotation;
            var decal = dead.GetComponentInChildren<DecalProjector>();
            var thisDecal = GetComponentInChildren<DecalProjector>();
            decal.material = thisDecal.material;

            var thisSwitcher = GetComponentInChildren<ArmamentSwitcher>();
            var switcher = dead.GetComponentInChildren<ArmamentSwitcher>();
            switcher.ChangedArmament(thisSwitcher.CurrentArmament);
            Debug.Log($"CurrentArmament: ${thisSwitcher.CurrentArmament}");
            
            if (!IsOwner)
                return;
            EventManager.TriggerEvent("OnDeath", deathData);
        }
        
        #endregion
        
        [SerializeField] private CinemachineImpulseSource impulseSource;

        private void OnShoot(object arg0)
        {
            Debug.Log("OnShoot");
            impulseSource.GenerateImpulse(Vector3.forward * recoilStrength);
            _rigidbody.AddForce(-transform.forward * recoilCounterForce, ForceMode.Impulse);
        }
    }
}
