using System;
using Gameplay.Quests.Tasks.TaskHelper;
using UnityEngine;
using Utility;

namespace Gameplay.Quests.Tasks.TasksType
{
    public class ReachTask : Task
    {
        [NonSerialized]
        public ReachZone zoneArea;
        public GameObject zoneAreaPrefab;
        public string zoneName = "Reach the area";
        
        [NonSerialized]
        private bool _zoneReached = false;
        public override void Activate()
        {
            base.Activate();
            zoneArea = Instantiate(zoneAreaPrefab, zoneAreaPrefab.transform.position, zoneAreaPrefab.transform.rotation)
                .GetComponent<ReachZone>();
            zoneArea.Task = this;
            zoneArea.onTriggerEnter.AddListener(OnTriggerEnter);
        }
        
        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == PlayerManager.Player.gameObject && !IsCompleted)
            {
                _zoneReached = true;
                Complete();
            }
        }

        public override bool Complete(bool forceComplete = false)
        {
            if (base.Complete(forceComplete))
            {
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
            return _zoneReached;
        }
    }
}