using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Units;
using ScriptableObjects.GameParameters;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class LegionSpawner : NetworkBehaviour
{
    public DemoParameters demoParameters;
    // Update is called once per frame
    
    public void SpawnLegion(GameObject prefab)
    {
        if (!IsSpawned || !HasAuthority)
            return;
        var point = transform.position + Random.insideUnitSphere * demoParameters.spawnRadius;
        if (NavMesh.SamplePosition(point, out var hit, 500, -1))
        {
            var unit = Instantiate(prefab, hit.position, Quaternion.Euler(0, Random.Range(0, 360), 0));
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                unit.GetComponent<NetworkObject>().Spawn(true);
            }
        }
        else
            SpawnLegion(prefab);
    }

    public void SpawnLegion(int n)
    {
        if (!IsSpawned || !HasAuthority)
            return;
        int ameise = (int)(n * (float) demoParameters.ameiseLoweRatio / 100);
        for (int i = 0; i < ameise; i++)
            SpawnLegion(demoParameters.ameisePrefab);
        for (int i = 0; i < n - ameise; i++)
            SpawnLegion(demoParameters.lowePrefab);
    }
    [ContextMenu("SpawnLegions")]
    public void SpawnLegions()
    {
        KillAllLegions();
        SpawnLegion(demoParameters.spawnCount);
    }

    [ContextMenu("SpawnAmeise")]
    public void SpawnAmeise()
    {
        SpawnLegion(demoParameters.ameisePrefab);
    }
    
    public void SpawnOneDinosauria()
    {
        SpawnLegion(demoParameters.dinosauriaPrefab);
    }
    
    private void KillAllLegions()
    {
        if (!IsSpawned || !HasAuthority)
            return;
        foreach (var legion in new List<Unit>(Factions.GetMembers(Faction.Legion)))
        {
            legion.Die();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, demoParameters.spawnRadius);
    }
}
