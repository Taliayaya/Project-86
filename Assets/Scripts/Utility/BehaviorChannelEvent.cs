using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Utility
{
    public class BehaviorChannelEvent : MonoBehaviour
    {
        [SerializeField] MorphoStunned morphoStunnedEventChannel;
        [SerializeField] UnityEvent<GameObject, float> onMorphoStunnedEvent;

        private void OnEnable()
        {
            if (morphoStunnedEventChannel != null && onMorphoStunnedEvent != null)
                    morphoStunnedEventChannel.Event += onMorphoStunnedEvent.Invoke;
        }

        private void OnDisable()
        {
            if (morphoStunnedEventChannel != null && onMorphoStunnedEvent != null)
                morphoStunnedEventChannel.Event -= onMorphoStunnedEvent.Invoke;
        }
    }
}