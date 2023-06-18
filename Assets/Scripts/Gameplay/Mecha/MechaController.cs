using System;
using Cinemachine;
using ScriptableObjects;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Mecha
{
    [RequireComponent(typeof(Rigidbody))]
    public class MechaController : MonoBehaviour, IHealth
    {
        public enum Zoom
        {
            Default,
            X2,
            X4,
            X8,
        }
        public JuggernautParameters juggernautParameters;
        private Vector2 _lastMouseUpdate;
        private Rigidbody _rigidbody;


        [SerializeField] private float gravity = -9.81f;

        [Header("Movement")] 
        [SerializeField] private float groundDrag;

        [SerializeField]
        private Transform modelTransform;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;

        [Header("Ground Check")] 
        [SerializeField]
        private Transform[] ground;
        [SerializeField] private LayerMask groundMask;

        [SerializeField] private Transform gunTransform;
        private bool _isGrounded;

        private Vector2 _lastMovement;

        private float _xRotation;

        private float _yRotation;

        private Zoom _zoom = Zoom.Default;
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
                        break;
                    case Zoom.X2:
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

                virtualCamera.m_Lens.FieldOfView = zoomValue;
                EventManager.TriggerEvent("OnZoomChange", _zoom);
            }
        } 

        // Start is called before the first frame update

        #region Unity Callbacks

        void Awake()
        {
            
            GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            if (!_rigidbody)
                _rigidbody = GetComponent<Rigidbody>();
            EventManager.AddListener("OnLookAround", OnLookAround);
            EventManager.AddListener("OnMove", OnMove);
            EventManager.AddListener("OnZoomIn", OnZoomIn);
            EventManager.AddListener("OnZoomOut", OnZoomOut);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnLookAround", OnLookAround);
            EventManager.RemoveListener("OnMove", OnMove);
            EventManager.RemoveListener("OnZoomIn", OnZoomIn);
            EventManager.RemoveListener("OnZoomOut", OnZoomOut);
        }

        private void FixedUpdate()
        {
            MoveJuggernaut();
            CheckGround();
            ApplyGravity();
            RotateJuggernaut();
            LimitSpeed();
        }


        #endregion

        #region Movement and Camera

        

        private void CheckGround()
        {
            int groundNumber = 0;
            for (int i = 0; i < ground.Length; i++)
            {
                bool grounded = Physics.CheckSphere(ground[i].position, 2, groundMask);
                if (grounded)
                    groundNumber++;
            }

            _isGrounded = (groundNumber * 2 >= ground.Length); // 50% of legs on ground == grounded
            
            
            if (_isGrounded)
                _rigidbody.drag = groundDrag;
            else
                _rigidbody.drag = 0;
        }

        private void MoveJuggernaut()
        {
            var move = _rigidbody.transform.forward * (_lastMovement.y) + _rigidbody.transform.right * (_lastMovement.x);
                                    
            //_rigidbody.MovePosition(_rigidbody.position + move * Time.fixedDeltaTime);
            _rigidbody.AddForce(move.normalized * (juggernautParameters.movementSpeed * 10f), ForceMode.Force);
        }

        private void ApplyGravity()
        {
            _rigidbody.AddForce(Vector3.up * (gravity * Time.fixedDeltaTime), ForceMode.Force);
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
            if (flatVel.magnitude > juggernautParameters.movementSpeed)
            {
                var limitedVel = flatVel.normalized * juggernautParameters.movementSpeed;
                _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.velocity.y, limitedVel.z);
            }
        }

        
        #endregion
        
        #region Input Callbacks
        
        public const float MinXRotation = -25f;
        public const float MaxXRotation = 25f;
        

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

        private void OnZoomIn(object data)
        {
            if (data is not float mouseScrollY || _zoomCd)
                return;
            if (CameraZoom != Zoom.X8)
                CameraZoom++;
            _zoomCd = true;
            Invoke(nameof(ResetZoomCd), 0.25f);
        }

        private void OnZoomOut(object data)
        {
            if (data is not float mouseScrollY || _zoomCd)
                return;
            if (CameraZoom != Zoom.Default)
                CameraZoom--;
            _zoomCd = true;
            Invoke(nameof(ResetZoomCd), 0.25f);

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

        public float Health { get; set; }

        public float MaxHealth { get; set; }

        public void OnTakeDamage()
        {
            EventManager.TriggerEvent("OnTakeDamage", Health);
        }

        void IHealth.Die()
        {
            EventManager.TriggerEvent("OnDeath");
        }
        
        #endregion
    }
}
