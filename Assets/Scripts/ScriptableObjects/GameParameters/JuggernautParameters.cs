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
 

        [Range(1, 100), DefaultValue(10f)]
        public float mouseSensitivity = 10f;
        [Range(1f, 100f), DefaultValue(25)]
        public float scrollSensitivity = 25f;
        [Range(10, 100), DefaultValue(30)]
        public float walkSpeed = 30f;
        [Range(10, 100), DefaultValue(60)]
        public float runSpeed = 60f;

        public float stepHeight = 0.8f;
        public Faction faction = Faction.Republic;
        
        [Range(1, 3000), DefaultValue(100f)] public float health = 100f;
        
        //[] 
        [DefaultValue(100), Range(10, 500)] public float maxGrappleDistance = 100f;
        [DefaultValue(20), Range(2, 60)] public float grapplePullSpeed = 20f;
        [DefaultValue(2), Range(0.1f, 10)] public float grapplingCd = 2;

        public bool toggleCockpitView = true;
                


        public float MouseSensitivity => mouseSensitivity / 4;
        
        public override string GetParametersName { get; } = "Juggernaut";
    }
}