using Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "JuggernautParameters", menuName = "Scriptable Objects/GameParameters/JuggernautParameters")]
    public class JuggernautParameters : GameParameters
    {
        
        public float dashSpeed = 100f;   // Dash force applied during dash
        public float dashDuration = 0.5f;  // How long dash lasts
        public float dashCooldown = 1f;    // CD time before next dash

        public float jumpPower = 1000f;
        public float maxJumpDuration = 0.5f;
        public float jumpCooldown = 1f;
 

        public float mouseSensitivity = 10f;
        public float scrollSensitivity = 25f;
        public float walkSpeed = 30f;
        public float runSpeed = 60f;

        public float stepHeight = 0.8f;
        public Faction faction = Faction.Republic;
        
        [Range(1, 3000)] public float health = 100f;
        
        //[] 
        [Range(10, 500)] public float maxGrappleDistance = 100f;
        [Range(2, 60)] public float grapplePullSpeed = 20f;
        [Range(0.1f, 10)] public float grapplingCd = 2;

        public bool toggleCockpitView = true;
                


        public float MouseSensitivity => mouseSensitivity / 4;
        
        public override string GetParametersName { get; } = "Juggernaut";
    }
}