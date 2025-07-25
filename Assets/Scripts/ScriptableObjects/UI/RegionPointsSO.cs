using System;
using Gameplay.Units;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScriptableObjects.UI
{
    [CreateAssetMenu(fileName = "RegionPoints", menuName = "Scriptable Objects/UI/RegionPoints")]
    public class RegionPointsSO : ScriptableObject, INetworkSerializable
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
        public bool isMultiplayer;

        [TextArea]
        public string description;
        
        public Status status = Status.Locked;

        public RotationPoint rotation = RotationPoint.Up;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref regionName);
            serializer.SerializeValue(ref position);
            
            serializer.SerializeValue(ref enemyType);
            serializer.SerializeValue(ref description);
            serializer.SerializeValue(ref scene);
        }
    }
}