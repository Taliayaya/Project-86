using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Sound;
using JetBrains.Annotations;
using ScriptableObjects.UI;
using Unity.Netcode;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Quests
{
    public class QuestManager : NetworkSingleton<QuestManager>
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
        private NetworkVariable<NetworkBehaviourReference> _currentQuestRef = new NetworkVariable<NetworkBehaviourReference>(null);
        [CanBeNull]
        public static Quest CurrentQuest
        {
            get => Instance._currentQuest;
            set
            {
                if (!Instance.IsOwner) return;
                if (Instance._currentQuest == value) return;
                
                Instance._currentQuestRef.Value = value;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            _currentQuestRef.OnValueChanged += QuestValueChanged;

            if (!IsOwner)
            {
                if (_currentQuestRef.Value.TryGet(out _currentQuest) && _currentQuest)
                {
                    Debug.Log("[QuestManager]: Quest sync from host: " + _currentQuest.name);
                    SoundManager.PlayOneShot(questStartSound);
                    _currentQuest.Activate();
                }
                return;
            }

            foreach (var questMission in questMissions)
                if (questMission.defaultMission)
                {
                    _quests = questMission.quests;
                    break;
                }
            Invoke(nameof(SelectFirstQuestAndRegister), 1f);
        }
        
        private bool _isStarted = false;
        private void Start()
        {
            _isStarted = true;
        }

        private void SelectFirstQuestAndRegister()
        {
            foreach (var quest in _quests)
            {
                // quest.OnStatusChanged += OnQuestStatusChanged; //Already added in CurrentQuest setter
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
        
        private void QuestValueChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            if (_currentQuest)
                _currentQuest.OnStatusChanged -= OnQuestStatusChanged;
            if (newValue.TryGet(out _currentQuest) && _currentQuest)
            {
                Debug.Log("[QuestManager]: Quest changed: " + _currentQuest.name);
                _currentQuest.OnStatusChanged += OnQuestStatusChanged;
                EventManager.TriggerEvent("QuestChanged", _currentQuest);
                if (!IsOwner)
                {
                    SoundManager.PlayOneShot(questStartSound);
                    _currentQuest.Activate();
                }
            }
        }
        
        public void OnQuestStatusChanged(QuestStatus oldStatus, Quest quest)
        {
            EventManager.TriggerEvent("QuestStatusChanged", quest);
            if (quest.IsCompleted)
            {
                Debug.Log("[QuestManager]: Quest completed: " + quest.name + " " + CurrentQuest?.name);
                quest.OnStatusChanged -= OnQuestStatusChanged;
                if (quest == CurrentQuest)
                {
                    SoundManager.PlayOneShot(questStartSound);
                    CurrentQuest = null;
                    foreach (var q in _quests)
                    {
                        if (q.Activate())
                        {
                            Debug.Log("[QuestManager]: Quest activated: " + q.name);
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