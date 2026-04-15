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
        private Dictionary<EntityId, Pool> _pools = new Dictionary<EntityId, Pool>();
        private Dictionary<EntityId, EntityId> _objectToPrefab = new Dictionary<EntityId, EntityId>();

        public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(prefab.GetEntityId(), out var pool))
            {
                _pools[prefab.GetEntityId()] = new Pool()
                {
                    Queue = new Queue<GameObject>()
                };
                pool = _pools[prefab.GetEntityId()];
            }

            if (pool.IsEmpty)
            {
                var instance = GameObject.Instantiate(prefab, position, rotation);
                _objectToPrefab[instance.GetEntityId()] = prefab.GetEntityId();
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
            instance.transform.SetParent(null);
            
            // if we can't find the parent, just destroy the object
            // it can happen during a network sync
            if (!_objectToPrefab.TryGetValue(instance.GetEntityId(), out var prefabId))
            {
                Destroy(instance);
                return;
            }
            // otherwise, put it back in the pool
            _pools[prefabId].Queue.Enqueue(instance);
        }
    }
}