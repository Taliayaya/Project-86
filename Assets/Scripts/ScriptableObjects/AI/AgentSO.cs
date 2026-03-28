using System;
using Gameplay;
using UnityEngine;

namespace ScriptableObjects.AI
{
    // ReSharper disable once InconsistentNaming
    [CreateAssetMenu(fileName = "Agent", menuName = "Scriptable Objects/AI/Agent")]
    public class AgentSO : ScriptableObject
    {
        [Range(0, 360)] public float fieldOfViewAngle = 180;
        // how far can the unit detect targets
        [Range(0, 10000)] public float viewDistance = 10;
        // how far can the target be to shoot
        [Range(0, 10000)] public float combatDistance = 10;
        public float rotationSpeed = 1f;
        [Range(0, 10000)] public float idealDistanceFromEnemy = 40;
        
        public float speed = 10;
        public Faction faction;

        public int minXRotation = -30;
        public int maxXRotation = 30;

        public GameObject deathEffect;
        public GameObject deadPrefab;

    }
}