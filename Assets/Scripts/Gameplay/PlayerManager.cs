using System;
using System.Collections.Generic;
using Armament.Shared;
using Cosmetic;
using DefaultNamespace;
using Gameplay.Units;
using JetBrains.Annotations;
using Networking.Widgets.Session.Session;
using ScriptableObjects.Skins;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Gameplay
{
    using SessionProperties = Constants.Properties.Session;
    struct PlayerInfo
    {
        GameObject mech;
        ulong id;
        string username;
    }

    public class PlayerManager : Singleton<PlayerManager>
    {
        [CanBeNull] private Unit _player;

        [CanBeNull] public static Unit Player
        {
            get => Instance._player;
            set
            {
                if (Instance._player == value) return;
                Instance._player = value;
                EventManager.TriggerEvent(Constants.TypedEvents.OnPlayerChanged, value);
            }
        }
        
        private Dictionary<ulong, GameObject> _playerObjects = new Dictionary<ulong, GameObject>(); // <PlayerId, PlayerObject>
        public static Dictionary<ulong, GameObject> PlayerObjects => Instance._playerObjects;

        public static Vector3 PlayerPosition => Player ? Player.transform.position : Vector3.negativeInfinity;

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.Session.SessionJoined, OnSessionJoined);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.Session.SessionJoined, OnSessionJoined);
        }

        private void OnSessionJoined(object arg0)
        {
            if (arg0 is not ISession session) return;
            
            Debug.Log("[PlayerManager] Saving player data");
            JuggConfigSO juggConfig = LoadPersonalMark.LoadConfig();
            ArmamentConfig armamentConfig = ArmamentConfigManager.GetConfig();
            Debug.Log("[PlayerManager] Loaded personal mark: " + juggConfig.personalMarkFileName);
            Debug.Log("[PlayerManager] Loaded armament: " + armamentConfig.CurrentArmament.ToString());
            session.CurrentPlayer.SetProperties(
                new Dictionary<string, PlayerProperty>()
                {
                    {
                        SessionProperties.JuggernautArmament, new PlayerProperty(armamentConfig.CurrentArmament.ToString())
                    },
                    {
                        SessionProperties.PersonalMark, new PlayerProperty(juggConfig.personalMarkFileName)
                    }
                });
            session.SaveCurrentPlayerDataAsync();
            session.PlayerPropertiesChanged += OnSessionPlayerDataChanged;
        }
        
        private void OnSessionPlayerDataChanged()
        {
            Debug.Log("[PlayerManager] Players data changed");
            EventManager.TriggerEvent(Constants.Events.Session.SessionPlayerDataChanged);
        }
    }
}
