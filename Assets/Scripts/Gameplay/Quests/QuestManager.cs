using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Sound;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Quests
{
    public class QuestManager : Singleton<QuestManager>
    {
        [SerializeField] private AudioClip questStartSound;
        protected override void OnAwake()
        {
            base.OnAwake();
            
        }
        
        public List<Quest> quests = new List<Quest>();
        public static List<Quest> Quests => Instance.quests;

        [CanBeNull] private Quest _currentQuest;
        [CanBeNull]
        public static Quest CurrentQuest
        {
            get => Instance._currentQuest;
            set
            {
                if (Instance._currentQuest == value) return;
                
                if (Instance._currentQuest != null)
                    Instance._currentQuest.OnStatusChanged -= Instance.OnQuestStatusChanged;
                Instance._currentQuest = value;
                //if (value != null)
                //    value.OnStatusChanged += Instance.OnQuestStatusChanged;

                EventManager.TriggerEvent("QuestChanged", value);
            }
        }
        

        private void Start()
        {
            SelectFirstQuestAndRegister();
        }

        private void SelectFirstQuestAndRegister()
        {
            foreach (var quest in quests)
            {
                quest.OnStatusChanged += OnQuestStatusChanged; //Already added in CurrentQuest setter
                if (CurrentQuest == null && quest.Activate())
                {
                    SoundManager.PlayOneShot(questStartSound);
                    CurrentQuest = quest;
                }
            }
            
        }

        
        public static void AddQuest(Quest quest)
        {
            Instance.quests.Add(quest);
        }
        
        public void OnQuestStatusChanged(QuestStatus oldStatus, Quest quest)
        {
            EventManager.TriggerEvent("QuestStatusChanged", quest);
            if (quest.IsCompleted)
            {
                if (quest == CurrentQuest)
                {
                    SoundManager.PlayOneShot(questStartSound);
                    CurrentQuest = null;
                    foreach (var q in quests)
                    {
                        if (q.Activate())
                        {
                            CurrentQuest = q;
                            break;
                        }
                    }
                }
            }
        }
    }
}