using UnityEngine;

namespace ScriptableObjects.AI
{
    [CreateAssetMenu(fileName = "Squad", menuName = "Scriptable Objects/AI/Squad")]
    public class SquadSO : ScriptableObject
    {
        public int sprintDistance = 15;
        public int squadMergeDistance = 75;
        public int priority = 0;
        public int maxPriority = 5;
        public int spacing = 10;
        public int maxSquadSize = 20;
        public float positionUpdateFrequency = 1;
        public bool canChaseEnemies = false;
        public int minUnitBeforeChase = 3;
    }
}