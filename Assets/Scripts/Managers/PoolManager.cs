using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    internal struct Pool
    {
        public Queue<GameObject> Queue;
        
        public bool IsEmpty => Queue.Count == 0;
    }
    public class PoolManager : Singleton<PoolManager>
    {
        private Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();
        private Dictionary<int, int> _objectToPrefab = new Dictionary<int, int>();

        public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(prefab.GetInstanceID(), out var pool))
            {
                _pools[prefab.GetInstanceID()] = new Pool()
                {
                    Queue = new Queue<GameObject>()
                };
                pool = _pools[prefab.GetInstanceID()];
            }

            if (pool.IsEmpty)
            {
                var instance = GameObject.Instantiate(prefab, position, rotation);
                _objectToPrefab[instance.GetInstanceID()] = prefab.GetInstanceID();
                return instance;
            }
            else
            {
                var instance = pool.Queue.Dequeue();
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.SetActive(true);
                return instance;
            }
        }

        public void BackToPool(GameObject instance)
        {
            instance.SetActive(false);
            int prefabId = _objectToPrefab[instance.GetInstanceID()];
            _pools[prefabId].Queue.Enqueue(instance);
        }
    }
}