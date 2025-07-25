using System;
using System.Collections.Generic;
using DefaultNamespace;
using Gameplay.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay
{
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
                EventManager.TriggerEvent("PlayerChanged", value);
            }
        }
        
        private Dictionary<ulong, GameObject> _playerObjects = new Dictionary<ulong, GameObject>(); // <PlayerId, PlayerObject>
        public static Dictionary<ulong, GameObject> PlayerObjects => Instance._playerObjects;

        public static Vector3 PlayerPosition => Player ? Player.transform.position : Vector3.negativeInfinity;
    }
}