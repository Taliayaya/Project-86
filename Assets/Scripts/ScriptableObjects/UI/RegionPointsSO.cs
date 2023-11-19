using System;
using Gameplay.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScriptableObjects.UI
{
    [CreateAssetMenu(fileName = "RegionPoints", menuName = "Scriptable Objects/UI/RegionPoints")]
    public class RegionPointsSO : ScriptableObject
    {
        public enum Status
        {
            Locked,
            Unlocked,
            Completed,
            Hidden
        }

        public enum RotationPoint
        {
            Up,
            Down,
            ReverseDown,
            ReverseUp,
        }
        
        public string regionName;
        
        public Vector2 position;
        
        public UnitType enemyType;
        
        public SceneData scene;

        [TextArea]
        public string description;
        
        public Status status = Status.Locked;

        public RotationPoint rotation = RotationPoint.Up;
    }
}