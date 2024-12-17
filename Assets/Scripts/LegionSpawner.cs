using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Units;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class LegionSpawner : MonoBehaviour
{
    public DemoParameters demoParameters;
    // Update is called once per frame
    
    public void SpawnLegion(GameObject prefab)
    {
        var point = transform.position + Random.insideUnitSphere * demoParameters.spawnRadius;
        if (NavMesh.SamplePosition(point, out var hit, 500, -1))
            Instantiate(prefab, hit.position , Quaternion.Euler(0, Random.Range(0, 360), 0));
        else
            SpawnLegion(prefab);
    }

    public void SpawnLegion(int n)
    {
        int ameise = (int)(n * (float) demoParameters.ameiseLoweRatio / 100);
        for (int i = 0; i < ameise; i++)
            SpawnLegion(demoParameters.ameisePrefab);
        for (int i = 0; i < n - ameise; i++)
            SpawnLegion(demoParameters.lowePrefab);
    }

    public void SpawnLegions()
    {
        KillAllLegions();
        SpawnLegion(demoParameters.spawnCount);
    }
    
    public void SpawnOneDinosauria()
    {
        SpawnLegion(demoParameters.dinosauriaPrefab);
    }
    
    private void KillAllLegions()
    {
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
