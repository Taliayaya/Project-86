using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "ScavengerParameters", menuName = "Scriptable Objects/GameParameters/ScavengerParameters")]
    public class ScavengerParameters: GameParameters
    {
        public string scavengerName = "Fido";
        [Range(100, 1000)] public float maxHealth = 100;
        
        [Header("On Death")]
        public GameObject deathExplodePrefab;
        public float deathExplodeRadius;
        public AnimationCurve deathExplodeDamage;
        public LayerMask deathExplodeLayerMask;
        
        public float Damage(float distance)
        {
            return deathExplodeDamage.Evaluate(distance);
        }
        public override string GetParametersName { get; } = "Scavenger";
    }
}