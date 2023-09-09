using System;
using Gameplay.Quests;
using Gameplay.Quests.Tasks;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public class QuestWindow : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _questDescription;
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
            _questDescription.text = "";
            foreach (var task in quest.Tasks)
            {
                _questDescription.text += $"{task}\n";
            }
            quest.OnTaskProgressChanged += OnTaskProgressUpdate;

        }
        
        private void OnTaskProgressUpdate(Task task)
        {
            _questDescription.text = "";
            foreach (var t in task.Owner.Tasks)
            {
                _questDescription.text += $"{t}\n";
            }
        }
    }
}