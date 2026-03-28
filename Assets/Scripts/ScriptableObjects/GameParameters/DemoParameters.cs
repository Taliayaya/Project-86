using System;
using Gameplay.Units;
using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "DemoParameters", menuName = "Scriptable Objects/GameParameters/DemoParameters")]
    public class DemoParameters : GameParameters
    {
        [Range(1, 30)] public int spawnCount = 10;
        [Range(20, 1000)] public float spawnRadius = 100f;
        [Range(100, 1000)] public int ameiseHealth = 200;
        [Range(100, 1000)] public int grauwolfHealth = 200;
        [Range(100, 1500)] public int loweHealth = 500;
        [Range(100, 1500)] public int dinosauriaHealth = 5000;
        [Range(0, 100)] public int ameiseLoweRatio = 66;
        public GameObject ameisePrefab;
        public GameObject grauwolfPrefab;
        public GameObject lowePrefab;
        public GameObject dinosauriaPrefab;
        public override string GetParametersName => "Demo Settings";

        public int GetBaseHealth(UnitType unit)
        {
            return unit switch
            {
                UnitType.Ameise => ameiseHealth,
                UnitType.Lowe => loweHealth,
                UnitType.Dinosauria => dinosauriaHealth,
                UnitType.Grauwolf => grauwolfHealth,
                // UnitType.Skorpion => 0,
                // UnitType.None => 0,
                // UnitType.Juggernaut => 0,
                // UnitType.Scavenger => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
            };
        }
    }
}