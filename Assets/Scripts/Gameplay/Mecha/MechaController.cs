using System;
using Cinemachine;
using Gameplay.Units;
using ScriptableObjects;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Mecha
{
    [RequireComponent(typeof(Rigidbody))]
    public class MechaController : Unit
    {
        public enum Zoom
        {
            Default,
            X2,
            X4,
            X8,
        }


        

        #region Serialized Fields

        public JuggernautParameters juggernautParameters;
        [SerializeField] private float gravity = -9.81f;

        [Header("Movement")] 
        [SerializeField] private float groundDrag;

        [SerializeField]
        private Transform modelTransform;
        [SerializeField] private Camera zoomCamera;
        [SerializeField] private CinemachineVirtualCamera vCamera;
        [SerializeField] private LayerMask forwardMask;

        [Header("Ground Check")] 
        [SerializeField]
        private Transform[] ground;
        [SerializeField] private LayerMask groundMask;
        
        [Header("Main Recoil")]
        [SerializeField] private float recoilStrength = 1f;
        [SerializeField] private float recoilDuration = 0.1f;
        [SerializeField] private float recoilCounterForce = 1f;
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
        
        #endregion

        #region Properties
        
        public float MovementSpeed => _isRunning ? juggernautParameters.runSpeed : juggernautParameters.walkSpeed;

        private Zoom CameraZoom
        {
            get => _zoom;
            set
            {
                _zoom = value;
                float zoomValue;
                switch (_zoom)
                {
                    case Zoom.Default:
                        zoomValue = 60;
                        zoomCamera.enabled = false;
                        break;
                    case Zoom.X2:
                        zoomCamera.enabled = true;
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

                zoomCamera.fieldOfView = zoomValue;
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
            PlayerManager.Player = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!_rigidbody)
                _rigidbody = GetComponent<Rigidbody>();
            EventManager.AddListener("OnLookAround", OnLookAround);
            EventManager.AddListener("OnMove", OnMove);
            EventManager.AddListener("OnZoomIn", OnZoomIn);
            EventManager.AddListener("OnZoomOut", OnZoomOut);
            EventManager.AddListener("OnRun", OnRun);
            EventManager.AddListener("OnShoot:Primary", OnShoot);
        }

        

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.RemoveListener("OnLookAround", OnLookAround);
            EventManager.RemoveListener("OnMove", OnMove);
            EventManager.RemoveListener("OnZoomIn", OnZoomIn);
            EventManager.RemoveListener("OnZoomOut", OnZoomOut);
            EventManager.RemoveListener("OnRun", OnRun);
            EventManager.RemoveListener("OnShoot:Primary", OnShoot);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            CheckGround();
            ApplyGravity(); // Currently using the rigidbody gravity
            LimitSpeed();
            CheckDistanceForward();
            if (State == UnitState.Default)
            {
                MoveJuggernaut();

                RotateJuggernaut();
            }
        }

        protected override void Start()
        {
            base.Start();
            EventManager.TriggerEvent("RegisterMinimapTarget", transform);
            Cursor.lockState = CursorLockMode.Locked;
        }

        #endregion

        #region Movement and Camera

        private void CheckDistanceForward()
        {
            if (Physics.Raycast(vCamera.transform.position, vCamera.transform.forward, out var hit, 2000, forwardMask))
            {
                EventManager.TriggerEvent("OnDistanceForward", hit.distance);
            }
            else
                EventManager.TriggerEvent("OnDistanceForward", float.NaN);
        }
        

        private void CheckGround()
        {
            int groundNumber = 0;
            for (int i = 0; i < ground.Length; i++)
            {
                bool grounded = Physics.CheckSphere(ground[i].position, 0.3f, groundMask);
                if (grounded)
                    groundNumber++;
            }

            _isGrounded = (groundNumber * 2 >= ground.Length); // 50% of legs on ground == grounded
            
            
            if (_isGrounded)
                _rigidbody.drag = groundDrag;
            else
                _rigidbody.drag = 1;
        }

        private void MoveJuggernaut()
        {
            var move = _rigidbody.transform.forward * (_lastMovement.y) + _rigidbody.transform.right * (_lastMovement.x);
                                    
            //_rigidbody.MovePosition(_rigidbody.position + move * Time.fixedDeltaTime);
            _rigidbody.AddForce(move.normalized * (MovementSpeed * 1000f), ForceMode.Force);
        }

        private void ApplyGravity()
        {
            _yVelocity += gravity * Time.fixedDeltaTime;
            if (_isGrounded)
                _yVelocity = gravity / 2;
            _rigidbody.AddForce(Vector3.up * (_yVelocity), ForceMode.Force);
        }

        private void RotateJuggernaut()
        {
            var rotation = Quaternion.Euler(Vector3.up * _yRotation);
            var modelRotation = Quaternion.Euler(Vector3.right * _xRotation);

            //rotation = Quaternion.Lerp(_rigidbody.rotation, rotation, juggernautParameters.mouseSensitivity * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(rotation);
            modelTransform.localRotation = modelRotation;
        }

        private void LimitSpeed()
        {
            var flatVel = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
            if (flatVel.magnitude > MovementSpeed)
            {
                var limitedVel = flatVel.normalized * MovementSpeed;
                _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
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
            if (data is not Vector2 pos)
                return;
            _xRotation -= pos.y * juggernautParameters.MouseSensitivity * Time.fixedDeltaTime;
            _yRotation += pos.x * juggernautParameters.MouseSensitivity * Time.fixedDeltaTime;
            EventManager.TriggerEvent("OnUpdateCompass", _yRotation);

            _xRotation = Mathf.Clamp(_xRotation, MinXRotation, MaxXRotation);
            EventManager.TriggerEvent("OnUpdateXRotation", _xRotation);
            

        }

        private void OnMove(object data)
        {
            if (data is not Vector2 movement)
                return;
            _lastMovement = movement;
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
            if (data is not bool run)
                return;
            _isRunning = run;
        }

        #endregion

        private void ResetZoomCd()
        {
            _zoomCd = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < ground.Length; i++)
            {
                Gizmos.DrawSphere(ground[i].position, 2f);
            }

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
