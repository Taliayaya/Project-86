using Gameplay;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// Milestone point on the railway: when the Morpho drives through, it brakes and
    /// railguns the monument. Reuses the obstacle brake-and-shoot flow
    /// (MorphoObstacleChannel), so no behavior graph changes are needed.
    /// Becomes inert once its monument is destroyed.
    /// </summary>
    public class Milestone : MonoBehaviour
    {
        [SerializeField] private Monument monument;
        [SerializeField] private MorphoObstacleChannel morphoObstacleChannel;

        private void OnTriggerEnter(Collider other)
        {
            if (!monument || monument.IsDestroyed)
                return;
            if (other.attachedRigidbody && other.attachedRigidbody.CompareTag("Morpho"))
                morphoObstacleChannel.SendEventMessage(monument.transform);
        }
    }
}
