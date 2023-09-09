using System;
using System.Collections.Generic;
using Gameplay.Quests;
using Gameplay.Quests.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.HUD
{
    public class QuestWindow : MonoBehaviour
    {
        [FormerlySerializedAs("_questDescriptionTransform")] [SerializeField]
        private Transform questDescriptionTransform;
        [FormerlySerializedAs("_questDescriptionPrefab")] [SerializeField] private GameObject questDescriptionPrefab;
        
        private List<TMP_Text> _tasksTexts = new List<TMP_Text>();
        private void Awake()
        {
            
        }

        private void OnEnable()
        {
            EventManager.AddListener("QuestChanged", OnQuestChanged);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("QuestChanged", OnQuestChanged);
        }

        private void OnQuestChanged(object questArg)
        {
            Quest quest = (Quest) questArg;
            if (quest == null)
            {
                transform.GetChild(0).gameObject.SetActive(false);
                return;
            }
            transform.GetChild(0).gameObject.SetActive(true);
            DestroyTasksTexts();
            foreach (var task in quest.Tasks)
            {
                TMP_Text text = Instantiate(questDescriptionPrefab, questDescriptionTransform).GetComponent<TMP_Text>();
                text.gameObject.name = task.name;
                _tasksTexts.Add(text);
                if (task.importance == TaskImportance.Optional)
                    text.text += $"[Optional] ";
                text.text += task;
                SetTextColorDependingStatus(task.Status, text);
            }
            quest.OnTaskProgressChanged += OnTaskProgressUpdate;
            quest.OnTaskStatusChanged += OnTaskStatusChanged;

        }

        private void OnTaskStatusChanged(TaskStatus oldstatus, Task task)
        {
            for (int i = 0; i < _tasksTexts.Count; i++)
            {
                var text = _tasksTexts[i];
                var currentTask = task.Owner.Tasks[i];
                SetTextColorDependingStatus(currentTask.Status, text);
            }
        }

        private void OnTaskProgressUpdate(Task task)
        {
            for (int i = 0; i < _tasksTexts.Count; i++)
            {
                var text = _tasksTexts[i];
                var currentTask = task.Owner.Tasks[i];
                text.text = currentTask.ToString();
                SetTextColorDependingStatus(currentTask.Status, text);
            }
            
        }

        private void SetTextColorDependingStatus(TaskStatus status, TMP_Text text)
        {
            switch (status)
            {
                case TaskStatus.Active:
                    text.color = Color.white;
                    break;
                case TaskStatus.Inactive:
                    text.color = Color.gray;
                    break;
                case TaskStatus.Completed:
                    text.color = Color.green;
                    break;
                case TaskStatus.Failed:
                    text.color = Color.gray;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void DestroyTasksTexts()
        {
            foreach (var text in _tasksTexts)
            {
                Destroy(text.gameObject);
            }
            _tasksTexts.Clear();
        }
    }
}