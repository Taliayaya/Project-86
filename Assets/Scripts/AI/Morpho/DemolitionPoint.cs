using System.Collections;
using Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace AI
{
    public enum DemolitionPhase : byte { Idle, Planting, Armed, Detonated }

    /// <summary>
    /// Marked demolition spot at the base of a big-tier structure. The player
    /// orders Fido here with the existing OrderScavenger flow (no new UI).
    /// While the Scavenger stands inside the trigger volume, planting progresses;
    /// leaving or dying resets it. When planted, the charge arms and counts down
    /// (world-space beep/light via onPhaseChanged hooks), then triggers the
    /// CollapsePoint. Phase logic runs on the session owner (same authority as
    /// the Morpho graph); phase syncs to everyone via NetworkVariable.
    /// Scavenger code is untouched: this component observes Fido, Fido doesn't
    /// know demolition exists.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DemolitionPoint : NetworkBehaviour
    {
        [SerializeField] private CollapsePoint collapsePoint;
        [SerializeField] private float plantDuration = 12f;
        [SerializeField] private float countdownDuration = 5f;
        [Tooltip("Scavenger must be slower than this to make planting progress")]
        [SerializeField] private float maxPlantSpeed = 1f;
        [Tooltip("Invoked on all clients when the phase changes (hook beeping light / SFX here)")]
        public UnityEngine.Events.UnityEvent<DemolitionPhase> onPhaseChanged;

        // distributed-authority project: owner (= session owner for scene objects) writes
        private readonly NetworkVariable<DemolitionPhase> _phase = new(DemolitionPhase.Idle,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> _plantProgress = new(0f,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private ScavengerController _scavengerInside;

        public DemolitionPhase Phase => _phase.Value;

        public override void OnNetworkSpawn()
        {
            _phase.OnValueChanged += (_, next) => onPhaseChanged?.Invoke(next);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody &&
                other.attachedRigidbody.TryGetComponent(out ScavengerController scav))
                _scavengerInside = scav;
        }

        private void OnTriggerExit(Collider other)
        {
            if (_scavengerInside && other.attachedRigidbody &&
                other.attachedRigidbody.GetComponent<ScavengerController>() == _scavengerInside)
                _scavengerInside = null;
        }

        private void Update()
        {
            if (!HasAuthority) // distributed authority, not IsServer (see CLAUDE.md)
                return;
            if (_phase.Value != DemolitionPhase.Idle && _phase.Value != DemolitionPhase.Planting)
                return;

            bool planting = _scavengerInside &&
                            _scavengerInside.gameObject.activeInHierarchy &&
                            ScavengerIsStationary();

            if (!planting)
            {
                if (_phase.Value == DemolitionPhase.Planting)
                {
                    _phase.Value = DemolitionPhase.Idle;
                    _plantProgress.Value = 0f;
                }
                return;
            }

            if (_phase.Value == DemolitionPhase.Idle)
                _phase.Value = DemolitionPhase.Planting;

            _plantProgress.Value += Time.deltaTime;
            if (_plantProgress.Value >= plantDuration)
                StartCoroutine(ArmAndDetonate());
        }

        private bool ScavengerIsStationary()
        {
            var agent = _scavengerInside.GetComponent<UnityEngine.AI.NavMeshAgent>();
            return agent && agent.velocity.magnitude <= maxPlantSpeed;
        }

        private IEnumerator ArmAndDetonate()
        {
            _phase.Value = DemolitionPhase.Armed;
            yield return new WaitForSeconds(countdownDuration);
            _phase.Value = DemolitionPhase.Detonated;
            DetonateRpc();
        }

        // topple physics runs locally on every client (same accepted
        // simplification as FallingRock); blockade placement is authored
        [Rpc(SendTo.Everyone)]
        private void DetonateRpc() => collapsePoint.TriggerCollapse();

        public void DebugForcePlant()
        {
            if (HasAuthority && _phase.Value == DemolitionPhase.Idle)
                StartCoroutine(ArmAndDetonate());
        }
    }
}
