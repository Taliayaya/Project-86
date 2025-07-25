using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Sound;
using JetBrains.Annotations;
using ScriptableObjects.UI;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Quests
{
    public class QuestManager : Singleton<QuestManager>
    {
        [Serializable]
        public struct QuestMission
        {
            public RegionPointsSO mission;
            public List<Quest> quests;
            [Tooltip("Start this mission automatically (for testing purposes)")]
            public bool defaultMission;
        }
        [SerializeField] private AudioClip questStartSound;
        protected override void OnAwake()
        {
            base.OnAwake();
            EventManager.AddListener(Constants.TypedEvents.OnSceneLoadingCompleted, OnSceneLoaded);   
        }

        private void OnEnable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.OnSceneLoadingCompleted, OnSceneLoaded);
            EventManager.AddListener(Constants.TypedEvents.OnSceneLoadingCompleted, OnSceneLoaded);
        }

        private void OnDisable()
        {
             EventManager.RemoveListener(Constants.TypedEvents.OnSceneLoadingCompleted, OnSceneLoaded);
        }

        public List<QuestMission> questMissions;

        public static List<Quest> Quests => Instance._quests;
        private List<Quest> _quests = new List<Quest>();

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
            foreach (var questMission in questMissions)
                if (questMission.defaultMission)
                {
                    _quests = questMission.quests;
                    break;
                }
            SelectFirstQuestAndRegister();
        }

        private void SelectFirstQuestAndRegister()
        {
            foreach (var quest in _quests)
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
            Instance._quests.Add(quest);
        }
        
        public void OnQuestStatusChanged(QuestStatus oldStatus, Quest quest)
        {
            EventManager.TriggerEvent("QuestStatusChanged", quest);
            if (quest.IsCompleted)
            {
                quest.OnStatusChanged -= OnQuestStatusChanged;
                if (quest == CurrentQuest)
                {
                    SoundManager.PlayOneShot(questStartSound);
                    CurrentQuest = null;
                    foreach (var q in _quests)
                    {
                        if (q.Activate())
                        {
                            CurrentQuest = q;
                            break;
                        }
                    }
                }
            }
            if (CurrentQuest == null)
            {
                EventManager.TriggerEvent(Constants.Events.Analytics.LevelFinished);
            }
        }
        
        private void OnSceneLoaded(object arg0)
        {
            if (arg0 is not RegionPointsSO mission)
                return;
            SelectMission(mission);
        }

        private void SelectMission(RegionPointsSO mission)
        {
            foreach (var questMission in questMissions)
            {
                if (questMission.mission.regionName == mission.regionName)
                {
                    _quests = questMission.quests;
                    SelectFirstQuestAndRegister();
                    return;
                }
            }
        }
    }
}