using Gameplay;
using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "JuggernautParameters", menuName = "Scriptable Objects/GameParameters/JuggernautParameters")]
    public class JuggernautParameters : GameParameters
    {
        [Range(1, 100)]
        public float mouseSensitivity = 1f;
        [Range(10, 100)]
        public float movementSpeed = 10f;

        public float stepHeight = 0.8f;
        public Faction faction = Faction.Republic;
        
        [Range(1, 3000)] public float health = 100f;


        public float MouseSensitivity => mouseSensitivity / 4;
        
        public override string GetParametersName { get; } = "Juggernaut";
    }
}