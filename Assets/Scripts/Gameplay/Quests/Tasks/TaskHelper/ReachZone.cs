using System;
using System.Collections;
using Gameplay.Quests.Tasks.TasksType;
using UnityEngine;
using Utility;

namespace Gameplay.Quests.Tasks.TaskHelper
{
    public class ReachZone : ColliderEvent
    {
        [NonSerialized]
        private ReachTask _task;
        public GameObject miniMapIcon;
        public Marker marker;
        
        private void OnDisable()
        {
            miniMapIcon.SetActive(false);
            marker.gameObject.SetActive(false);
        }

        public ReachTask Task
        {
            get => _task;
            set
            {
                _task = value;
                StartCoroutine(InformPlayerPosition());
            }
        }
        public Vector3 ZonePosition => transform.position;
        
        public float DistanceToZone(Vector3 position)
        {
            return Vector3.Distance(position, ZonePosition);
        }

        private IEnumerator InformPlayerPosition()
        {
            while (Task && !Task.IsCompleted)
            {
                Task.OnTaskProgressChangedHandler();
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}