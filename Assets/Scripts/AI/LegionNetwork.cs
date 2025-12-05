using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Gameplay.Units;
using Unity.Netcode;
using UnityEngine;

namespace AI
{
    public struct SharedTarget : INetworkSerializable,IEquatable<SharedTarget>
    {
        public NetworkObjectReference Target;
        public TargetInfo.VisibilityStatus Visibility;
        public Vector3 SpotPosition;
        // NetworkTime::Time field
        public double LastSpotTime;
        public double FirstSpotTime;
        
        public double SpotDuration => LastSpotTime - FirstSpotTime;
        
        public bool Equals(SharedTarget other)
        {
            return Target.Equals(other.Target);
        }

        public override bool Equals(object obj)
        {
            return obj is SharedTarget other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Target.GetHashCode();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Target);
            serializer.SerializeValue(ref Visibility);
            serializer.SerializeValue(ref SpotPosition);
            serializer.SerializeValue(ref LastSpotTime);
            serializer.SerializeValue(ref FirstSpotTime);
        }
    }
    public class LegionNetwork : NetworkSingleton<LegionNetwork>
    {
        public NetworkList<SharedTarget> NetworkTargets = new NetworkList<SharedTarget>();
        public float morale = 100f;
        public NetworkVariable<AIMorale> moraleState = new(AIMorale.Normal);
        public NetworkVariable<int> killCount = new(0);
        public NetworkVariable<int> casualties = new(0);
        
        [Header("Morale weights")]
        public float killCountWeight = 0.5f;
        public float casualtiesWeight = 0.5f;
        public float allyAliveWeight = 0.5f;
        public float enemyAliveWeight = 0.5f;
        
        public event Action<AIMorale> MoraleStateChanged;
        
        [Header("Cleaning")]
        public float cleaningInterval = 30f;
        public float cleaningOldestTime = 30f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
                return;
            EventManager.AddListener(Constants.TypedEvents.UnitDeath, OnUnitDeath);
            StartCoroutine(CleaningRoutine());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (!IsOwner)
                return;
            EventManager.RemoveListener(Constants.TypedEvents.UnitDeath, OnUnitDeath);
        }

        private void OnUnitDeath(object arg0)
        {
            if (arg0 is not Unit unit)
                return;
            if (unit.Faction == Faction.Legion)
                casualties.Value++;
            else
                killCount.Value++;
            UpdateMorale();
        }

        public void UpdateMorale()
        {
            float newMorale = 100;
            newMorale += killCount.Value  * killCountWeight;
            newMorale += Factions.GetMembers(Faction.Legion).Count * allyAliveWeight;
            newMorale -= casualties.Value * casualtiesWeight;
            newMorale -= Factions.GetMembers(Faction.Republic).Count * enemyAliveWeight;
            
            newMorale = Mathf.Clamp(newMorale, 0, 200);
            if (Mathf.Approximately(newMorale, 200))
                moraleState.Value = AIMorale.Fearless;
            else if (newMorale > 130)
                moraleState.Value = AIMorale.Confident;
            else if (newMorale > 85)
                moraleState.Value = AIMorale.Normal;
            else if (newMorale > 50)
                moraleState.Value = AIMorale.Cautious;
            else if (newMorale > 0)
                moraleState.Value = AIMorale.Wavering;
            else
                moraleState.Value = AIMorale.Panicked;

            MoraleStateChanged?.Invoke(moraleState.Value);
        }

        private IEnumerator CleaningRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(cleaningInterval);
                ClearOldTargets();
            }
        }

        private void ClearOldTargets()
        {
            var time = NetworkManager.Singleton.ServerTime;
            int countPreCleaning = NetworkTargets.Count;
            for (int i = NetworkTargets.Count - 1; i >= 0; i--)
            {
                SharedTarget target = NetworkTargets[i];
                // if the target is alive and was still detected recently, keep it
                if (target.Target.TryGet(out var targetObj) && targetObj)
                {
                    bool spotExpired = time.Time - target.LastSpotTime > cleaningOldestTime;
                    if (!spotExpired)
                        continue;
                }
                // else, remove it
                NetworkTargets.RemoveAt(i);
            }
            Debug.Log($"[LegionNetwork]: Cleaned {countPreCleaning - NetworkTargets.Count} targets");
        }

        [Rpc(SendTo.Owner)]
        public void ReportTargetRpc(NetworkObjectReference target, TargetInfo.VisibilityStatus visibility, Vector3 spotPosition)
        {
            SharedTarget sharedTarget = new SharedTarget()
            {
                Target = target, Visibility = visibility,
                SpotPosition = spotPosition,
                LastSpotTime = NetworkManager.Singleton.ServerTime.Time,
            };
            
            int index = NetworkTargets.IndexOf(sharedTarget);
            if (index == -1)
            {
                sharedTarget.FirstSpotTime = NetworkManager.Singleton.ServerTime.Time;
                NetworkTargets.Add(sharedTarget);
            }
            else
            {
                sharedTarget.FirstSpotTime = NetworkTargets[index].FirstSpotTime;
                NetworkTargets[index] = sharedTarget;
            }
#if UNITY_EDITOR
            UpdateMirror();
#endif
        }
        
#if UNITY_EDITOR
        public List<GameObject> TargetsMirror = new List<GameObject>();
        private void UpdateMirror()
        {
            TargetsMirror.Clear();
            foreach (var networkTarget in NetworkTargets)
            {
                NetworkObjectReference obj = networkTarget.Target;
                TargetsMirror.Add(obj.TryGet(out var target) ? target.gameObject : null);;
            }
        }
#endif
    }
}