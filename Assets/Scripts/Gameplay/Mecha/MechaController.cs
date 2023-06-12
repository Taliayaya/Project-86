using System;
using Cinemachine;
using UnityEngine;

namespace Gameplay.Mecha
{
    [RequireComponent(typeof(Rigidbody))]
    public class MechaController : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera fpCamera;
        [SerializeField] private Transform mechaTransform;
        [SerializeField] private Transform mainGunTransform;
        
        private Vector2 _lastMouseUpdate;
        private float _xRotation;
        private Rigidbody _rigidbody;

        [SerializeField] private float movementSpeed;

        private Vector2 _lastMovement;
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
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnLookAround", OnLookAround);
            EventManager.RemoveListener("OnMove", OnMove);
        }

        private void FixedUpdate()
        {
            MoveJuggernaut();
            ApplyGravity();
        }
        

        #endregion

        private void MoveJuggernaut()
        {
            var move = _rigidbody.transform.forward * (_lastMovement.y * movementSpeed) + Vector3.right * (_lastMovement.x * movementSpeed);
                                    
            _rigidbody.MovePosition(_rigidbody.position + move * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            
        }

        #region Input Callbacks

        private void OnLookAround(object data)
        {
            if (data is not Vector2 pos)
                return;
            float rotation = pos.x * Time.deltaTime;
            float verticalRotationAngle = pos.y * Time.deltaTime;
            

            Quaternion deltaRotation = Quaternion.Euler(Vector3.up * rotation + Vector3.left * verticalRotationAngle);
            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);

        }

        private void OnMove(object data)
        {
            if (data is not Vector2 movement)
                return;
            _lastMovement = movement;
            Debug.Log("OnMove " + movement);
        }

        

        #endregion
    }
}
