using UnityEngine;

namespace ScriptableObjects.Tutorial
{
    [CreateAssetMenu(fileName = "GameplayTutorial", menuName = "Scriptable Objects/Tutorial/Gameplay Tutorial")]
    public class GameplayTutorialSO : ScriptableObject
    {
        public string title;
        [TextArea] public string text;
    }
}