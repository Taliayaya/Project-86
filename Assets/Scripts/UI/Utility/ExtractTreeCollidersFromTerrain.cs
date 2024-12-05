using System.Linq;
using UnityEngine;

namespace UI.Utility
{
    [RequireComponent(typeof(Terrain))]
    public class ExtractTreeCollidersFromTerrain : MonoBehaviour
    {
        [ContextMenu("Extract")]
        public void Extract()
        {
            Terrain terrain = GetComponent<Terrain>();
            Transform[] transforms = terrain.GetComponentsInChildren<Transform>();

            //Skip the first, since its the Terrain Collider
            ClearCache();

            Debug.Log("terrain.terrainData.treePrototypes.Length: " + terrain.terrainData.treePrototypes.Length);
            for (int i = 0; i < terrain.terrainData.treePrototypes.Length; i++)
            {
                TreePrototype tree = terrain.terrainData.treePrototypes[i];

                //Get all instances matching the prefab index
                TreeInstance[] instances = terrain.terrainData.treeInstances.Where(x => x.prototypeIndex == i).ToArray();

                Debug.Log("instances.Length: " + instances.Length);
                for (int j = 0; j < instances.Length; j++)
                {
                    //Un-normalize positions so they're in world-space
                    instances[j].position = Vector3.Scale(instances[j].position, terrain.terrainData.size);
                    instances[j].position += terrain.GetPosition();

                    //Fetch the collider from the prefab object parent
                    BoxCollider prefabCollider = tree.prefab.GetComponent<BoxCollider>();
                    if(!prefabCollider) continue;
                    Debug.Log("prefabCollider: " + prefabCollider);

                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = tree.prefab.name + j;

                    if (terrain.preserveTreePrototypeLayers) obj.layer = tree.prefab.layer;
                    else obj.layer = terrain.gameObject.layer;

                    obj.transform.localScale = prefabCollider.size * prefabCollider.transform.localScale.magnitude;
                    obj.transform.position = instances[j].position;
                    obj.transform.position += prefabCollider.size.y * Vector3.up;
                    obj.transform.parent = terrain.transform;
                    obj.isStatic = true;
                }
            }
            Debug.Log("<color=green>Tree colliders extracted from terrain</color>");
        }

        [ContextMenu("Clear Cache")]
        public void ClearCache()
        {
            Terrain terrain = GetComponent<Terrain>();
            Transform[] transforms = terrain.GetComponentsInChildren<Transform>();
            for (int i = 1; i < transforms.Length; i++)
            {
                //Delete all previously created colliders first
                DestroyImmediate(transforms[i].gameObject);
            }

        }
    }
}