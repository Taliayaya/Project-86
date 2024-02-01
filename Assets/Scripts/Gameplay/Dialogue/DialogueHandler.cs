using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Gameplay.Dialogue
{
    public class DialogueHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private AudioSource audioSource;

        private Queue<DialogueSO> _dialogueQueue = new Queue<DialogueSO>();

        [SerializeField] private float charPerSecond = 33;
        private bool _isPlaying;

        private void OnEnable()
        {
            EventManager.AddListener(EventsList.DialogueRequested, OnDialogueRequested);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener(EventsList.DialogueRequested, OnDialogueRequested);
        }

        IEnumerator PlayCoroutine(DialogueSO dialogue)
        {
            dialogue.dialogueStartedEvent?.Invoke(dialogue);
            dialoguePanel.SetActive(true);
            _isPlaying = true;
            speakerText.text = dialogue.speaker;
            audioSource.clip = dialogue.voice;
            audioSource.Play();

            // for every dialogue lines
            for (int k = 0; k < dialogue.dialogues.Length; ++k)
            {
                string line = dialogue.dialogues[k];
                dialogueText.text = "";
                // display one character every 0.03s
                for (int i = 0; i < line.Length; ++i)
                {
                    dialogueText.text += line[i];
                    yield return new WaitForSeconds(1/charPerSecond);
                }

                yield return new WaitForSeconds(dialogue.dialoguesEndTime[k]);
            }

            dialoguePanel.SetActive(false);
            _isPlaying = false;
            if (_dialogueQueue.TryDequeue(out var nextDialogue))
                yield return StartCoroutine(PlayCoroutine(nextDialogue));
        }

        private void Play(DialogueSO dialogueSo)
        {
            if (dialogueSo.ignorePreviousDialogue)
            {
                _isPlaying = false;
                StopAllCoroutines();
                _dialogueQueue.Clear();
            }
            if (!_isPlaying && _dialogueQueue.Count == 0)
            {
                StartCoroutine(PlayCoroutine(dialogueSo));
                return;
            }
            _dialogueQueue.Enqueue(dialogueSo);
        }

        // triggered when a quest request to play a dialogue
        private void OnDialogueRequested(object arg)
        {
            DialogueSO dialogueSo = (DialogueSO)arg; // may change type (container??)
            Play(dialogueSo);
        }
    }
}