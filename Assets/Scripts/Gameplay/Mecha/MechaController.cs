using System;
using System.Collections;
using Cinemachine;
using Gameplay.Units;
using ScriptableObjects;
using ScriptableObjects.GameParameters;
using UI.HUD;
using UnityEngine.Events;
using UnityEngine.Serialization;

using UnityEngine;
using Unity.VisualScripting;
using Unity.Mathematics;
using static UnityEngine.LightAnchor;


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
        [SerializeField] private float maxFloorAngle = 40f;
        [SerializeField] private GameObject[] feetEnds;

        [SerializeField]
        private Transform modelTransform;
        public bool canRotate = true;
        [SerializeField] private Camera zoomCamera;
        [SerializeField] private Camera tpsZoomCamera;
        [SerializeField] private CinemachineVirtualCamera vCamera;
        [SerializeField] private CinemachineVirtualCamera tpsCamera;
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        [SerializeField] private LayerMask forwardMask;
        //[SerializeField] private GameObject[] feetArray;

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

        private Vector2 _lastMovement;

        private float _xRotation;

        private float _yRotation;
        private float _yVelocity;

        private Zoom _zoom = Zoom.Default;
        private bool _isRunning;
        // Fox's var's unorganised

        private bool isDashing = false;                      
        private bool canDash = true;                 
        private Coroutine dashCoroutine = null;

        private bool canJump = true;

        // Movement Overhaul

        private Ray groundRay;
        private RaycastHit groundHitData;
        private Vector3 up_direction = Vector3.up; // is more stable than transfom.up
        private Vector3 rotationLeft = Vector3.zero;
        private float floorAngle = 0f;
        private bool _isInclineStable = true;

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
                freeLookCamera.gameObject.SetActive(value == View.FreeLook);
                tpsCamera.gameObject.SetActive(value == View.ThirdPerson);
                onCameraViewChanged?.Invoke(value == View.FirstPerson ? vCamera : tpsCamera);
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
                        Debug.Log("You reached me");
                        MovementSpeed = juggernautParameters.walkSpeed;
                        break;
                    case MovementMode.Running :
                        MovementSpeed = juggernautParameters.runSpeed;
                        break;
                    case MovementMode.Slowed:
                        MovementSpeed = 10f; // juggernautParameters.slowSpeed
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
                        zoomCameraUsed.enabled = false;
                        break;
                    case Zoom.X2:
                        zoomCameraUsed.enabled = true;
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
            EventManager.TriggerEvent("OnUpdateHealth", 1f);
            EventManager.TriggerEvent(Constants.TypedEvents.OnToggleCockpitView, juggernautParameters.toggleCockpitView);
            PlayerManager.Player = this;
        }

        protected override void OnEnable()
        {
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
        }


        protected override void OnDisable()
        {
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
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            CheckGround(false);
            ApplyGravity();
            LimitSpeed();
            CheckDistanceForward();
            UpdateFloorRays();

            MatchGroundAngle();

            if (State == UnitState.Default)
            {
                RotateJuggernaut();
                MoveJuggernaut();

            }
        }

        protected override void Start()
        {
            base.Start();
            EventManager.TriggerEvent("RegisterMinimapTarget", transform);
            Cursor.lockState = CursorLockMode.Locked;
            groundRay = new Ray(transform.position, Vector3.down);
        }

        #endregion

        #region Movement and Camera

        private void MatchGroundAngle()
        {
            Vector3 normal = Vector3.Slerp(transform.up, groundHitData.normal, 0.2f);
            if (normal != null) { up_direction = Vector3.Slerp(up_direction, normal, 0.1f); } 
            else { up_direction = Vector3.Slerp(up_direction, Vector3.up, 0.1f); }

            Vector3 right = Vector3.Cross(up_direction, transform.forward);
            Vector3 forward = Vector3.Cross(right, up_direction);
            //Vector3.OrthoNormalize(ref normal, ref forward);
            transform.LookAt(transform.position + forward, up_direction);
            floorAngle = Vector3.Angle(Vector3.up, up_direction);

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
        
        public void UpdateFloorRays()
        {
            // Scans the surrounding terrain and creates an averaged normal of it
            

            // Overhaul code, above should be integrated into it
            int bodyRayOutwardCount = 8;
            int bodyRayInwardCount = 8; // these can hit the player's collider, make sure they don't do that by changing their angle and/or distance from the main body
            // Ground ray + Velocity ray + local down ray + body rays
            int bodyRayCount = 1 + 1 + 1+ bodyRayOutwardCount + bodyRayInwardCount;

            Vector3[] groundNormals = new Vector3[feetEnds.Length + bodyRayCount];

            RaycastHit hitData;

            // Gathering normals of the surrounding terrain 
            Vector3 direction = transform.forward;
            direction = Quaternion.AngleAxis(-30.0f, transform.right) * direction;
            for (int i = 0; i < bodyRayOutwardCount; i++)
            {
                Physics.Raycast(transform.position, Quaternion.AngleAxis(i * (360.0f / bodyRayOutwardCount), transform.up) * direction, out hitData, 3.0f);
                groundNormals[i] = hitData.normal;
            }

            direction = transform.forward;
            direction = Quaternion.AngleAxis(-105.0f, transform.right) * direction;
            for (int i = 0; i < bodyRayInwardCount; i++)
            {
                Vector3 offset = Quaternion.AngleAxis(i * (360.0f / bodyRayInwardCount), transform.up) * transform.forward * 1.5f;
                // 31 (bx011111) is a layer mask containing everything, but "damageables", change it to everything but the "player" layer mask once it is created
                Physics.Raycast(transform.position + offset, Quaternion.AngleAxis(i * (-360.0f / bodyRayInwardCount), transform.up) * direction, out hitData, 3.0f, 31); 
                groundNormals[bodyRayOutwardCount + i] = hitData.normal;
                //Debug.Log(hitData.collider);
            }

            // Gathering unique rays
            groundRay.origin = transform.position;
            groundRay.direction = Vector3.down * 10;
            Physics.Raycast(groundRay, out groundHitData);
            groundNormals[bodyRayCount - 1] = groundHitData.normal;

            direction = -transform.up * 3;
            Physics.Raycast(transform.position, direction, out hitData, 5.0f);
            groundNormals[bodyRayCount - 2] = hitData.normal * 3;

            direction = transform.forward * (_lastMovement.y) + transform.right * (_lastMovement.x);
            Physics.Raycast(transform.position, direction, out hitData, 5.0f);
            groundNormals[bodyRayCount - 3] = hitData.normal * 3;



            // Gathering normals where feet hit the ground
            int count = 0;
            for (int i = 0; i < feetEnds.Length; i++)
            {
                Physics.Raycast(feetEnds[i].transform.position, -feetEnds[i].transform.up, out hitData, 2.0f);
                groundNormals[bodyRayCount + i] = hitData.normal * 1.5f;
            }


            Vector3 avg_normal = Vector3.zero;
            count = 0;
            for (int i = 0; i < groundNormals.Length; i++)
            {
                if (groundNormals[i] != null)
                {
                    avg_normal += groundNormals[i];
                    count++;
                }
            }
            if (count > 0) { 
                avg_normal /= count;
                groundHitData.normal = avg_normal;
            }
            
            }

        public void CheckGround(bool isGrounded)
        {
            // _isGrounded = Physics.Raycast(transform.position, Vector3.down, out var hit, 3f, forwardMask);
            _isGrounded = groundHitData.distance <= airborneHeight;
            _isInclineStable = floorAngle < maxFloorAngle;
            Debug.Log(groundHitData.distance);
            if (_isGrounded)
                _rigidbody.linearDamping = groundDrag;
            else
                _rigidbody.linearDamping = 0.1f;
        }

        private void MoveJuggernaut()
        {
            // If dashing, don't apply any other movement mode logic // this made the dash feel stiff
            //if (isDashing)
            //    return;

            if (!_isGrounded || !_isInclineStable)
                return;

            //Debug.Log(_movementmode);

            var move = transform.forward * (_lastMovement.y) + transform.right * (_lastMovement.x);
            move = move.normalized;
            if (isDashing) { move *= 0.5f; }
            
            _rigidbody.AddForce(move * (MovementSpeed * 1000f), UnityEngine.ForceMode.Force);
            //_rigidbody.MovePosition(_rigidbody.position + move * Time.fixedDeltaTime);

        }


        private void ApplyGravity()
        {
            _yVelocity += - gravity * gravity * Time.fixedDeltaTime;
            if (_isGrounded && _isInclineStable)
                _yVelocity = 0; //gravity;
            _rigidbody.AddForce(Vector3.up * (_yVelocity), UnityEngine.ForceMode.Acceleration);
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
            _xRotation -= pos.y * juggernautParameters.MouseSensitivity * Time.fixedDeltaTime;
            _yRotation += pos.x * juggernautParameters.MouseSensitivity * Time.fixedDeltaTime;

            var _yRotationChange = pos.x * juggernautParameters.MouseSensitivity * Time.fixedDeltaTime;
            rotationLeft += up_direction * _yRotationChange; 

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
            Debug.Log("Jump attempted");


            StartCoroutine(JumpCoroutine());
        }

        private IEnumerator JumpCoroutine()
        {
            if(!canJump || !_isGrounded)
                yield break;

            // jump is now off for CD
            canJump = false;

            // applies a jump force
            _rigidbody.AddForce(Vector3.up * juggernautParameters.jumpPower, UnityEngine.ForceMode.Impulse);
            yield return null;

            // starts the cd
            yield return new WaitForSeconds(juggernautParameters.jumpCooldown);
            canJump = true;

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

            Debug.Log("Dash input received!");
           StartCoroutine(DashCoroutine());
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
            Debug.Log("Dash started!");

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

            while (elapsedTime < juggernautParameters.dashDuration)
            {
                // Best reccomendation for elapsedtime..
                float t = elapsedTime / juggernautParameters.dashDuration;

                // Gradually reduce speed using a smooth easing function
                float currentSpeed = Mathf.Lerp(dashSpeed, 30f, EaseOutQuad(t));  // Lerp from full speed to 0, will freeze you momentarily


                Vector3 currentVelocity = dashDirection * currentSpeed;
                //_rigidbody.AddForce(Vector3.down * test, UnityEngine.ForceMode.Acceleration);
                //Debug.Log(($"Y value: {currentVelocity}"));

                // Velocity based on current speed
                _rigidbody.linearVelocity = dashDirection * currentSpeed;

                elapsedTime += Time.deltaTime;  
                yield return null;  
            }

            // Reset after dash is finished
            isDashing = false;
            Debug.Log("Dash ended!");

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

        public override void OnTakeDamage(DamagePackage damagePackage)
        {
            base.OnTakeDamage(damagePackage);
            EventManager.TriggerEvent("OnTakeDamage", Health);
            EventManager.TriggerEvent("OnUpdateHealth", Health/MaxHealth);
        }

        public override void Die()
        {
            base.Die();
            var deathData = new DeathData
            {
                DeathPosition = transform.position,
                Faction = Faction
            };
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
