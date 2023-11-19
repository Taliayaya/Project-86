using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "SceneData", menuName = "Scriptable Objects/Scene Data")]
    public class SceneData : ScriptableObject
    {
        public string inputActionMap;
        public CursorLockMode cursorLockMode;

        public string SceneName = "";
    }
}