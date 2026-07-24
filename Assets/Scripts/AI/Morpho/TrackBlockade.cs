using UnityEngine;

namespace AI
{
    /// <summary>
    /// A massive object resting on the Morpho's track. When armed, waits for the
    /// Morpho to enter its brake-range trigger, then fires MorphoObstacleChannel
    /// exactly once. The only obstacle component that talks to the channel.
    /// The trigger collider on this object defines the brake range (how early
    /// the Morpho reacts) — size it generously, the Morpho needs braking distance.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrackBlockade : MonoBehaviour
    {
        [SerializeField] private MorphoObstacleChannel morphoObstacleChannel;
        [Tooltip("Armed blockades fire the channel when the Morpho comes in range. CollapsePoint arms at runtime; tick manually for pre-armed scene blockades.")]
        [SerializeField] private bool armed;

        private bool _sent;

        public bool IsArmed => armed;

        public void Arm() => armed = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!armed || _sent)
                return;
            if (other.attachedRigidbody && other.attachedRigidbody.CompareTag("Morpho"))
            {
                _sent = true;
                morphoObstacleChannel.SendEventMessage(transform);
            }
        }
    }
}
