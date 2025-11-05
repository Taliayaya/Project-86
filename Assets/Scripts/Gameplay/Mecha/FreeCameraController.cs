using System;
using Gameplay.Units;
using ScriptableObjects.GameParameters;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = Unity.Mathematics.Random;

namespace Gameplay.Mecha
{
    public class FreeCameraController : MonoBehaviour
    {
        [SerializeField] private float speed = 5;

        private Vector2 _movement;
        [SerializeField] private float _movementSpeedMultiplier = 1;
        [SerializeField] private float _maxMovementSpeed = 10;
        [SerializeField] private float _rotationSpeed = 0.5f;
        [SerializeField] private DemoParameters demoParameters;
        
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

            if (NetworkManager.Singleton.IsHost)
            {
                EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.PauseLegion, OnPauseLegion);
                EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.SpawnAmeise, OnSpawnAmeise);
                EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.SpawnDinosauria, OnSpawnDinosauria);
                EventManager.AddListener(Constants.TypedEvents.Inputs.FreeCamera.SpawnLowe, OnSpawnLowe);
            }
        }

        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnMoveFreeCamera, OnMove);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnSpeedFreeCamera, OnRun);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnLookAroundFreeCamera, OnLookAround);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnGoDownFreeCamera, OnGoDown);
            EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.OnGoUpFreeCamera, OnGoUp);
            if (NetworkManager.Singleton.IsHost)
            {
                EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.PauseLegion, OnPauseLegion);
                EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.SpawnAmeise, OnSpawnAmeise);
                EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.SpawnDinosauria, OnSpawnDinosauria);
                EventManager.RemoveListener(Constants.TypedEvents.Inputs.FreeCamera.SpawnLowe, OnSpawnLowe);

                Factions.Pause(Faction.Legion, false);
            }
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
        private void OnSpawnAmeise(object arg0)
        {
            SpawnUnitBelow(UnitType.Ameise);
        }

        private void OnSpawnDinosauria(object arg0)
        {
            SpawnUnitBelow(UnitType.Dinosauria);
        }

        private void OnSpawnLowe(object arg0)
        {
            SpawnUnitBelow(UnitType.Lowe);
        }

        private void OnPauseLegion(object arg0)
        {
            if (!NetworkManager.Singleton.IsHost)
                return;
            // if paused, we unpause, and vice versa
            Factions.Pause(Faction.Legion, !Factions.IsPaused(Faction.Legion));
        }

        private void SpawnUnitBelow(UnitType type)
        {
            Quaternion rotation = transform.rotation;
            Vector3 position = transform.position;

            switch (type)
            {
                case UnitType.Ameise:
                    SpawnUnit(demoParameters.ameisePrefab, position, rotation);
                    break;
                case UnitType.Lowe:
                    SpawnUnit(demoParameters.lowePrefab, position, rotation);
                    break;
                case UnitType.Dinosauria:
                    SpawnUnit(demoParameters.dinosauriaPrefab, position, rotation);
                    break;
            }
        }
        
        public GameObject SpawnUnit(GameObject unitPrefab, Vector3 position, Quaternion rotation)
        {
            NavMeshHit hit = new NavMeshHit();
            int i = 30;
            if (!NavMesh.SamplePosition(position, out hit, 100, NavMesh.AllAreas))
                return null;
            
            var enemy = Instantiate(unitPrefab, hit.position, rotation);
            Debug.Log("[EnemySpawner]: Spawned unit.");
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("[EnemySpawner]: Spawned unit to network.");
                enemy.GetComponent<NetworkObject>().Spawn(true);
            }
            return enemy;
        }
    }
}