using System;
using System.Linq;
using UnityEngine;

namespace Gameplay
{
    public struct RespawnData
    {
        public Vector3 DeathPosition;
    }
    public class RespawnManager : Singleton<RespawnManager>
    {
        private GameObject[] _respawnPoints;
        
        [SerializeField] private GameObject playerPrefab;

        private void Awake()
        {
            _respawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        }

        private void OnEnable()
        {
            EventManager.AddListener("OnRespawn", OnRespawn);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnRespawn", OnRespawn);
        }

        public void Respawn(GameObject prefab, GameObject spawnPoint)
        {
            Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
        }
        
        public void Respawn(GameObject prefab, int spawnPointIndex)
        {
            Instantiate(prefab, _respawnPoints[spawnPointIndex].transform.position, _respawnPoints[spawnPointIndex].transform.rotation);
        }

        public static Vector3 GetClosestRespawnPoint(Vector3 origin)
        {
            return Instance._GetClosestRespawnPointPos(origin);
        }

        private Vector3 _GetClosestRespawnPointPos(Vector3 origin)
        {
            var closestSpawnPoint = _respawnPoints.Min(x => Vector3.Distance(x.transform.position, origin));
            var spawnPoint = _respawnPoints.First(x => Math.Abs(Vector3.Distance(x.transform.position, origin) - closestSpawnPoint) < 0.5);
            return spawnPoint.transform.position;
        }

        public void Respawn(GameObject prefab, Vector3 position)
        {
            if (_respawnPoints.Length == 0)
            {
                Debug.LogError("No respawn points found!");
                return;
            }
            var closestSpawnPoint = _respawnPoints.Min(x => Vector3.Distance(x.transform.position, position));
            var spawnPoint = _respawnPoints.First(x => Math.Abs(Vector3.Distance(x.transform.position, position) - closestSpawnPoint) < 0.5);
            Respawn(prefab, spawnPoint);
        }

        public void OnRespawn(object arg)
        {
            if (_respawnCD) return;
            _respawnCD = true;
            Invoke(nameof(ResetCD), 1f);
            var respawnData = (RespawnData) arg;
            Respawn(playerPrefab, respawnData.DeathPosition);
        }

        private bool _respawnCD = false;

        private void ResetCD()
        {
            _respawnCD = false;
        }
    }
}