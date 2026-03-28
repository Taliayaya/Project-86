using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Units;
using Unity.AI.Navigation;
using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Gameplay.Quests.Tasks.TaskHelper.TasksModules
{
    [Serializable]
    public class EnemySpawnerDebug
    {
        public bool showArea;
        public Color areaColor = Color.red;
    }

    /// <summary>
    /// Spawn enemies in a given area. It spawns an even number of enemies for each type.
    /// </summary>
    public class EnemySpawner : TaskModule
    {
        [Range(1, 200)]
        [SerializeField] private float spawnRadius = 50;
        [Range(1, 20)] // better to have multiple spawners than a lot of enemies in one place
        [SerializeField] private int spawnCount = 5;
        [SerializeField] private bool killOnComplete = false;
        
        [SerializeField] private List<GameObject> enemiesPrefabs = new List<GameObject>();

        [Header("Advanced settings")]
        [SerializeField] private List<GameObject> patrolPoints;
        [SerializeField] private float delayBeforeSpawn = 0;

        [Header("Debug")]
        [SerializeField] private EnemySpawnerDebug debug = new EnemySpawnerDebug();
        
        private GameObject[] _enemies;

        public override void Activate(Task task)
        {
            base.Activate(task);
            Invoke(nameof(SpawnEnemies), delayBeforeSpawn);
        }

        public GameObject SpawnUnit(GameObject unitPrefab, float radius)
        {
            NavMeshHit hit = new NavMeshHit();
            Vector3 position;
            int i = 30;
            do
            {
                i--;
                position = transform.position + UnityEngine.Random.insideUnitSphere * radius;
            } while (i > 0 && !NavMesh.SamplePosition(position, out hit, 100, NavMesh.AllAreas));

            NavMeshSurface nav;
            var enemy = Instantiate(unitPrefab, hit.position , Quaternion.Euler(0, Random.Range(0, 360), 0));
            Debug.Log("[EnemySpawner]: Spawned unit.");
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("[EnemySpawner]: Spawned unit to network.");
                enemy.GetComponent<NetworkObject>().Spawn(true);
            }
            return enemy;
        }

        private IEnumerator SpawnEnemiesCoroutine()
        {
            _enemies = new GameObject[spawnCount];
            for (int i = 0; i < enemiesPrefabs.Count; i++)
            {
                for (int j = 0; j < spawnCount / enemiesPrefabs.Count; j++)
                {
                    var unit = SpawnUnit(enemiesPrefabs[i], spawnRadius);
                    _enemies[i * (spawnCount / enemiesPrefabs.Count) + j] = unit;
                    if (patrolPoints.Count > 0)
                        unit.GetComponent<BehaviorGraphAgent>().SetVariableValue("PatrolPoints", patrolPoints);
                    yield return null;
                }
            }

            if (!killOnComplete)
                _enemies = null;
        }

        public void SpawnEnemies()
        {
            if (!NetworkManager.Singleton.IsHost)
                return;
            StartCoroutine(SpawnEnemiesCoroutine());
        }
        
        public override void OnComplete(Task task)
        {
            Debug.Log("Enemies killed");
            base.OnComplete(task);
            if (killOnComplete)
                foreach (var enemy in _enemies)
                    if (enemy)
                        Destroy(enemy);
            _enemies = null;
        }

        private void OnDrawGizmos()
        {
            if (debug.showArea)
            {
                Gizmos.color = debug.areaColor;
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = debug.areaColor;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
        
        
    }
}