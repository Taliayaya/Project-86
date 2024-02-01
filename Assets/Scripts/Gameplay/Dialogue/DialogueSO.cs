using System;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Dialogue
{
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Scriptable Objects/Dialogue")]
    public class DialogueSO : ScriptableObject
    {
        public AudioClip voice;
        [TextArea] public string[] dialogues;
        public float[] dialoguesEndTime;
        public string speaker;
        public bool ignorePreviousDialogue = false; // this dialogue will be played even if there is a dialogue playing
        
        public UnityEvent<DialogueSO> dialogueStartedEvent = new UnityEvent<DialogueSO>();
    }
}