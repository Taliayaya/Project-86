using Gameplay;
using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "JuggernautParameters", menuName = "Scriptable Objects/GameParameters/JuggernautParameters")]
    public class JuggernautParameters : GameParameters
    {
        [Range(1, 100), DefaultValue(10f)]
        public float mouseSensitivity = 10f;
        [Range(10, 100), DefaultValue(30)]
        public float movementSpeed = 30f;

        public float stepHeight = 0.8f;
        public Faction faction = Faction.Republic;
        
        [Range(1, 3000), DefaultValue(100f)] public float health = 100f;
        
        //[] 
        [DefaultValue(100), Range(10, 500)] public float maxGrappleDistance = 100f;
        [DefaultValue(20), Range(2, 60)] public float grapplePullSpeed = 20f;
        [DefaultValue(2), Range(0.1f, 10)] public float grapplingCd = 2;
                


        public float MouseSensitivity => mouseSensitivity / 4;
        
        public override string GetParametersName { get; } = "Juggernaut";
    }
}