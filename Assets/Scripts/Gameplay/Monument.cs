using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay
{
    /// <summary>
    /// A lore target (Giad, The Republic, ...) the Morpho railguns at milestone points.
    /// Must be tagged "Destructible" on the GameObject holding the collider: the railgun
    /// explosion (BeamTrigger) destroys it via SendMessage("Damage") — on the authority
    /// only; destruction replicates to everyone through a NetworkVariable.
    /// When the last monument falls, Constants.Events.AllMonumentsDestroyed is raised.
    /// Needs a NetworkObject on the same GameObject.
    /// </summary>
    public class Monument : NetworkBehaviour
    {
        private static readonly List<Monument> All = new();

        public string monumentName;
        public UnityEvent onDestroyed;

        private readonly NetworkVariable<bool> _destroyed = new();

        public bool IsDestroyed => _destroyed.Value;

        public static bool AllDestroyed => All.TrueForAll(m => m.IsDestroyed);

        private void Awake() => All.Add(this);

        public override void OnDestroy()
        {
            All.Remove(this);
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            _destroyed.OnValueChanged += OnDestroyedChanged;
        }

        public override void OnNetworkDespawn()
        {
            _destroyed.OnValueChanged -= OnDestroyedChanged;
        }

        private void OnDestroyedChanged(bool previous, bool current)
        {
            if (!current)
                return;
            onDestroyed?.Invoke();
            if (AllDestroyed)
                EventManager.TriggerEvent(Constants.Events.AllMonumentsDestroyed);
        }

        // called by the railgun explosion (BeamTrigger) via SendMessage on
        // "Destructible" colliders — authority-side only (BeamTrigger gates it)
        public void Damage(int amount)
        {
            if (IsDestroyed || !HasAuthority)
                return;
            _destroyed.Value = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!CompareTag("Destructible"))
                Debug.LogWarning($"{name}: Monument must be tagged 'Destructible' or the railgun won't damage it", this);
        }
#endif
    }
}
