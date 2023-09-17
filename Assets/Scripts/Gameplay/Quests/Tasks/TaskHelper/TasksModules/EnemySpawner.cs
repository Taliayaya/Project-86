using System;
using System.Collections.Generic;
using Gameplay.Units;
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

        [Header("Advanced settings")] [SerializeField]
        private bool areaAsGoal = false;
        [SerializeField] private Transform enemyGoal;
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
            } while (i > 0 && NavMesh.SamplePosition(position, out hit, 100, NavMesh.AllAreas));
            
            var enemy = Instantiate(unitPrefab, hit.position , Quaternion.Euler(0, Random.Range(0, 360), 0));
            return enemy;
        }
        private void SpawnEnemies()
        {
            _enemies = new GameObject[spawnCount];
            for (int i = 0; i < enemiesPrefabs.Count; i++)
            {
                for (int j = 0; j < spawnCount / enemiesPrefabs.Count; j++)
                {
                    _enemies[i * (spawnCount / enemiesPrefabs.Count) + j] = SpawnUnit(enemiesPrefabs[i], spawnRadius);
                }
            }
            
            if (areaAsGoal)
                SetEnemiesGoal(_enemies);
            if (!killOnComplete)
                _enemies = null;
        }
        
        public void SetEnemiesGoal(GameObject[] enemies)
        {
            foreach (var enemy in enemies)
            {
                enemy.GetComponent<AIAgent>().AddDestinationGoal(enemyGoal.position);
            }
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