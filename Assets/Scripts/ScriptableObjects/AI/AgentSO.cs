using System;
using UnityEngine;

namespace ScriptableObjects.AI
{
    // ReSharper disable once InconsistentNaming
    [CreateAssetMenu(fileName = "Agent", menuName = "Scriptable Objects/AI/Agent")]
    public class AgentSO : ScriptableObject
    {
        [Range(0, 360)] public float fieldOfViewAngle = 180;
        [Range(0, 10000)] public float viewDistance = 10;
        public float rotationSpeed = 1f;
        [Range(0, 10000)] public float idealDistanceFromEnemy = 40;
        [Range(0, 10000)] public int shareInformationMaxDistance = 100;
        
        public GameObject deathEffect;

    }
}