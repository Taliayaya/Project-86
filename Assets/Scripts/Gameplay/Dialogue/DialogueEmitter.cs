using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Dialogue
{
    public class DialogueEmitter : MonoBehaviour
    {
        [SerializeField] private List<DialogueSO> _dialogueQueue = new List<DialogueSO>();
        [SerializeField] private List<UnityEvent<DialogueSO>> dialogueEvents = new List<UnityEvent<DialogueSO>>();

        private int _currentDialogueIndex = 0;
        
        public void PlayOne()
        {
            if (_currentDialogueIndex < _dialogueQueue.Count)
            {
                var dialogue = _dialogueQueue[_currentDialogueIndex];
                dialogue.dialogueStartedEvent = dialogueEvents[_currentDialogueIndex]; // dangerous
                EventManager.TriggerEvent(EventsList.DialogueRequested, dialogue);
                _currentDialogueIndex++;
            }
        }

        public void Play(int number)
        {
            for (int i = 0; i < number; ++i)
                PlayOne();
        }
        
        public void PlayAll()
        {
            while (_currentDialogueIndex < _dialogueQueue.Count)
            {
                var dialogue = _dialogueQueue[_currentDialogueIndex];
                dialogue.dialogueStartedEvent = dialogueEvents[_currentDialogueIndex]; // dangerous
                EventManager.TriggerEvent(EventsList.DialogueRequested, dialogue);
                _currentDialogueIndex++;
            }
        }
        
    }
}