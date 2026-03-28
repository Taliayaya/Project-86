using System;
using Gameplay.Quests.Tasks.TaskHelper;
using Gameplay.Units;
using Unity.Netcode;
using UnityEngine;
using Utility;

namespace Gameplay.Quests.Tasks.TasksType
{
    public class ReachTask : Task
    {
        public ReachZone zoneArea;
        public string zoneName = "Reach the area";
        public UnitType unitAllowed = UnitType.Juggernaut;
        public bool playerOnly = true;
        
        [NonSerialized]
        private NetworkVariable<bool> _zoneReached = new NetworkVariable<bool>(false);
        public override void Activate()
        {
            base.Activate();
            _zoneReached.OnValueChanged += ValueChanged;
            zoneArea.Task = this;
            zoneArea.onTriggerEnter.AddListener(OnTriggerEnter);
        }

        private void ValueChanged(bool previousValue, bool newValue)
        {
            Complete();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!PlayerManager.Player) return;
            bool isUnitAllowed = unitAllowed.HasFlag(other.GetComponent<Unit>()?.unitType ?? UnitType.None);
            bool isPlayerOrAllowed = !playerOnly || other.gameObject == PlayerManager.Player.gameObject;
            if (isUnitAllowed && isPlayerOrAllowed && !IsCompleted)
            {
                Debug.Log("[ReachTask] OnTriggerEnter()");
                if (IsOwner)
                {
                    Debug.Log("[ReachTask] OnTriggerEnter() IsOwner");
                    _zoneReached.Value = true;
                    Complete();
                }
            }
        }

        public override bool Complete(bool forceComplete = false)
        {
            if (base.Complete(forceComplete))
            {
                zoneArea.gameObject.SetActive(false);
                zoneArea.onTriggerEnter.RemoveListener(OnTriggerEnter);
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            float distance = zoneArea.DistanceToZone(PlayerManager.PlayerPosition);
            // if its more than 1000m, then display it in kilometers
            if (distance < 1000)
                return $"{zoneName}: {distance:F0}m";
            return $"{zoneName}: {distance / 1000:F1}km";
        }

        public override bool CanComplete()
        {
            return _zoneReached.Value;
        }
    }
}