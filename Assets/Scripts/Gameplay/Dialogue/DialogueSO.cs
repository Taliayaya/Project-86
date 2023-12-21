using System;
using UnityEngine;

namespace Gameplay.Dialogue
{
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Scriptable Objects/Dialogue")]
    public class DialogueSO : ScriptableObject
    {
        public AudioClip voice;
        public string[] dialogues;
        public float[] dialoguesEndTime;
        public string speaker;
    }
}