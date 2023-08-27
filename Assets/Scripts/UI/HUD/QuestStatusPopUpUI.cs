using System;
using System.Collections;
using Gameplay.Quests;
using ScriptableObjects.Quests;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    internal class QuestStatusPopUpUI : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private TMP_Text questName;
        [SerializeField] private TMP_Text questStatus;
        
        [Header("Quest Status Name")]
        [SerializeField] private string newQuestStatusName = "New Quest";
        [SerializeField] private string missionFailedStatusName = "Mission Failed";
        [SerializeField] private string missionCompletedStatusName = "Mission Completed";
        
        private static readonly int Play = Animator.StringToHash("Play");
        private static readonly int Stop = Animator.StringToHash("Stop");

        private void Awake()
        {
            
            EventManager.AddListener("QuestStatusChanged", OnQuestStatusChange);
        }

        private void OnEnable()
        {
            EventManager.RemoveListener("QuestStatusChanged", OnQuestStatusChange);
            EventManager.AddListener("QuestStatusChanged", OnQuestStatusChange);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("QuestStatusChanged", OnQuestStatusChange);
        }

        private void OnQuestStatusChange(object arg0)
        {
            var quest = (Quest) arg0;
            if (quest.Status is not QuestStatus.Inactive and not QuestStatus.Locked)
            {
                StartCoroutine(PlayingPopUpAnimation(quest));
            }

        }

        private void SetText(Quest quest)
        {
            questName.text = quest.questName;
            switch (quest.Status)
            {
                case QuestStatus.Active:
                    questStatus.text = newQuestStatusName;
                    break;
                case QuestStatus.Inactive:
                    break;
                case QuestStatus.Cancelled:
                    questStatus.text = missionFailedStatusName;
                    break;
                case QuestStatus.Locked:
                    break;
                case QuestStatus.Completed:
                    questStatus.text = missionCompletedStatusName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }
        
        private IEnumerator PlayingPopUpAnimation(Quest quest)
        {
            Debug.Log("PlayingPopUpAnimation");
            SetText(quest);
            animator.SetTrigger(Play);
            yield return new WaitForSeconds(1);
            yield return new WaitForSeconds(1f);
            animator.SetTrigger(Stop);
        }
    }
}