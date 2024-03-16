using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Units;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class WaveLegionSpawner : MonoBehaviour
{
    public float spawnRadius = 20f;
    public float delayBeforeFirstWave = 0;
    public AnimationCurve waveInterval;
    public AnimationCurve unitCountPerWave;
    public UnitType legionType;
    public Faction faction = Faction.Legion;
    public DemoParameters demoParameters;
    public bool autoStart = false;
    public int maxTotalUnit = 200;
    [Tooltip("Where the enemies are headed after spawning")]public Transform initialGoal;
    public GameObject prefab;

    private int _waveNumber = 0; // increments at every wave and use waveInterval to know when the next wave happen

    private List<Unit> legionUnits;

    private void Start()
    {
        legionUnits = Factions.GetMembers(faction);
        if (autoStart)
            StartWave();
    }

    public void StartWave()
    {
        StartCoroutine(Wave());
    }

    private void SetInitialGoal(AIAgent agent)
    {
        agent.AddDestinationGoal(initialGoal);
    }

    public void SpawnLegion(GameObject prefab)
    {
        var point = transform.position + Random.insideUnitSphere * spawnRadius;
        if (NavMesh.SamplePosition(point, out var hit, 500, -1))
        {
            var unit = Instantiate(prefab, hit.position, Quaternion.Euler(0, Random.Range(0, 360), 0));
            if (initialGoal && unit.TryGetComponent(out AIAgent agent))
            {
                SetInitialGoal(agent);
            }
        }
        else
            SpawnLegion(prefab);
    }

    public void SpawnLegionType(UnitType type)
    {
        switch (type)
        {
            case UnitType.None:
                break;
            case UnitType.Ameise:
                SpawnLegion(demoParameters.ameisePrefab);
                break;
            case UnitType.Lowe:
                SpawnLegion(demoParameters.lowePrefab);
                break;
            case UnitType.Juggernaut:
                break;
            case UnitType.Scavenger:
                SpawnLegion(prefab);
                break;
            case UnitType.Legion:
                break;
            case UnitType.Republic:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public IEnumerator SpawnNLegion(int n, UnitType type)
    {
        for (int i = 0; i < n; ++i)
        {
            if (legionUnits.Count >= maxTotalUnit) // avoid spawning more unit if there are already more than 200
                yield break;
            SpawnLegionType(type);
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Wave()
    {
        yield return new WaitForSeconds(delayBeforeFirstWave);

        while (true)
        {

            int unitCount = (int)unitCountPerWave.Evaluate(_waveNumber);
            StartCoroutine(SpawnNLegion(unitCount, legionType));
            
            float waveIntervalTime = waveInterval.Evaluate(_waveNumber);
            yield return new WaitForSeconds(waveIntervalTime);
            _waveNumber++;
        }
        
    }
    
      private void OnDrawGizmosSelected()
      {
          Gizmos.color = Color.red;
          Gizmos.DrawWireSphere(transform.position, spawnRadius);
      }
}