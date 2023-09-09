using System;
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

        public static Vector3 PlayerPosition => Player ? Player.transform.position : Vector3.negativeInfinity;
    }
}