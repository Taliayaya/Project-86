using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "SceneData", menuName = "Scriptable Objects/Scene Data")]
    public class SceneData : ScriptableObject, INetworkSerializable
    {
        public string inputActionMap;
        public CursorLockMode cursorLockMode;

        public string SceneName = "";
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SceneName);
            serializer.SerializeValue(ref inputActionMap);
            serializer.SerializeValue(ref cursorLockMode);
        }
    }
}