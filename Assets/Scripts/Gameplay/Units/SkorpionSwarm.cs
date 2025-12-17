using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Units
{
    struct StrikeRequest
    {
        public Vector3 strikePosition;
        public Action<StrikeResponse> strikeCallback;
    }

    public enum StrikeResponse
    {
        SUCCESS,
        ON_COOLDOWN,
        TOO_CLOSE
    }

    public class SkorpionSwarm : NetworkBehaviour
    {
        private static Queue<Vector3> _strikeHappenings = new Queue<Vector3>();
        private static double _lastStrike;

        [SerializeField] private float strikeDuration = 5f;
        [SerializeField] private float strikeRadius = 100f;
        [SerializeField] private float strikePerSecond = 2f;
        [SerializeField] private float delay = 2f;
        [SerializeField] private float betweenStrikeCooldown = 15f;
        [SerializeField] private GameObject strikePrefab;
        [SerializeField] private float strikeYOffset = 700f;

        [Tooltip("Cooldown in seconds between strikes allowed")] [SerializeField]
        private float cooldown = 45f;

        private bool _onCooldown;

        private void Awake()
        {
            _strikeHappenings.Clear();
        }

        private void OnEnable()
        {
            _onCooldown = false;
            EventManager.AddListener(Constants.TypedEvents.StrikeRequest, OnStrikeRequested);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.StrikeRequest, OnStrikeRequested);
        }

        private void OnStrikeRequested(object arg0)
        {
            if (arg0 is not StrikeRequest strikeRequest)
                return;

            if (_onCooldown)
            {
                strikeRequest.strikeCallback?.Invoke(StrikeResponse.ON_COOLDOWN);
                return;
            }

            if (!ShouldAllowStrike(strikeRequest.strikePosition))
            {
                strikeRequest.strikeCallback?.Invoke(StrikeResponse.TOO_CLOSE);
                return;
            }

            SummonStrike(strikeRequest.strikePosition);
            strikeRequest.strikeCallback?.Invoke(StrikeResponse.SUCCESS);
        }

        private bool ShouldAllowStrike(Vector3 strikePosition)
        {
            if (NetworkManager.ServerTime.Time - _lastStrike < betweenStrikeCooldown)
                return false;
            foreach (var strikeHappening in _strikeHappenings)
            {
                float distance = Vector3.Distance(strikePosition, strikeHappening);
                if (distance < strikeRadius)
                {
                    return false;
                }
            }

            return true;
        }

        private void SummonStrike(Vector3 strikePosition)
        {
            uint seed = (uint) DateTime.Now.Ticks;
            SummonStrikeRpc(strikePosition, seed);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SummonStrikeRpc(Vector3 strikePosition, uint seed)
        {
            SummonStrikeCallees(strikePosition, seed);
        }

        private void SummonStrikeCallees(Vector3 strikePosition, uint seed)
        {
            _onCooldown = true;
            var strike = Instantiate(strikePrefab, strikePosition + strikeYOffset * Vector3.up, Quaternion.identity);
            var strikeScript = strike.GetComponentInChildren<ArtilleryStrike>();
            strikeScript.SendStrike(strikeDuration, strikePerSecond, strikeRadius, delay, seed);
            strikeScript.DestroyAfter(strikeDuration + 20);
            
            _strikeHappenings.Enqueue(strikePosition);
            _lastStrike = NetworkManager.ServerTime.Time;

            Invoke(nameof(OnStrikeFinished), strikeDuration);
            Invoke(nameof(ResetCooldown), cooldown);
        }

        private void OnStrikeFinished()
        {
            _strikeHappenings.Dequeue();
        }

        private void ResetCooldown()
        {
            _onCooldown = false;
        }
    }
}