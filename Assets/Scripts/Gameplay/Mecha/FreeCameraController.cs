using System;
using UnityEngine;

namespace Gameplay.Mecha
{
    public class FreeCameraController : MonoBehaviour
    {
        [SerializeField] private float speed = 5;

        private Vector2 _movement;
        [SerializeField] private float _movementSpeedMultiplier = 1;
        [SerializeField] private float _maxMovementSpeed = 10;
        [SerializeField] private float _rotationSpeed = 0.5f;
        
        private bool _isRunning;
        private bool _isGoingUp;
        private bool _isGoingDown;

        private void Awake()
        {
            EventManager.AddListener(Constants.Events.Inputs.FreeCamera.OnExitPhotoMode, OnExitPhotoMode);
        }

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.OnMoveFreeCamera, OnMove);
            EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.OnSpeedFreeCamera, OnRun);
            EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.OnLookAroundFreeCamera, OnLookAround);
            EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.OnGoDownFreeCamera, OnGoDown);
            EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.OnGoUpFreeCamera, OnGoUp);
            
        }


        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnMoveFreeCamera, OnMove);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnSpeedFreeCamera, OnRun);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnLookAroundFreeCamera, OnLookAround);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnGoDownFreeCamera, OnGoDown);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnGoUpFreeCamera, OnGoUp);
            
        }
        
        private void OnExitPhotoMode()
        {
            EventManager.RemoveListener(Constants.Events.Inputs.FreeCamera.OnExitPhotoMode, OnExitPhotoMode);
            Destroy(gameObject);
        }

        private void OnRun(object arg0)
        {
            _isRunning = (bool) arg0;
            if (_isRunning)
            {
                _movementSpeedMultiplier = 1;
            }
        }

        private void OnMove(object arg0)
        {
            _movement = (Vector2) arg0;
        }
        
        private void OnLookAround(object arg0)
        {
            var data = (Vector2) arg0;
            var euler = transform.localEulerAngles;
            euler.x -= data.y * _rotationSpeed * Time.deltaTime;
            euler.y += data.x * _rotationSpeed * Time.deltaTime;
            euler.z = 0;
            transform.localEulerAngles = euler;
        }
        
        private void OnGoDown(object arg0)
        {
            _isGoingDown = (bool) arg0;
        }
        
        private void OnGoUp(object arg0)
        {
            _isGoingUp = (bool) arg0;
        }

        private void Update()
        {
            if (_isRunning && _movementSpeedMultiplier < _maxMovementSpeed)
            {
                _movementSpeedMultiplier += Time.deltaTime * 2;
            }

            transform.position += (transform.forward * _movement.y + transform.right * _movement.x) * (_movementSpeedMultiplier * speed * Time.deltaTime);
            if (_isGoingDown)
            {
                transform.position -= transform.up * (2 * speed * Time.deltaTime);
            }
            if (_isGoingUp)
            {
                transform.position += transform.up * (2 * speed * Time.deltaTime);
            }
        }
    }
}