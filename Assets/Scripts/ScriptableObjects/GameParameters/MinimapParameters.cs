using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "MinimapParameters", menuName = "Scriptable Objects/GameParameters/MinimapParameters")]
    public class MinimapParameters : GameParameters
    {
        public override string GetParametersName => "Minimap";
        
        public bool lockCameraToPlayer = true;
        public bool rotateWithPlayer = true;
    }
}