using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace AI
{
    /// <summary>
    /// One-shot authored set-piece: a structure that topples across the rails
    /// about a HingeJoint at its base (anchor on the base edge facing the track,
    /// axis along that edge, connected body null, rigidbody kinematic until
    /// triggered). Gravity produces the accelerating base-pivot rotation —
    /// no torque-spin. On settle, destroys the joint, activates the pre-placed
    /// blockade and arms its TrackBlockade.
    /// Triggers: OnGrapplePull (fragile tier), Damage threshold (medium tier,
    /// collider on Damageable layer), TriggerCollapse (DemolitionPoint / debug).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
    public class CollapsePoint : NetworkBehaviour
    {
        [Header("Topple")]
        [Tooltip("Small nudge about the hinge axis to start the fall; gravity does the rest")]
        [SerializeField] private float nudgeTorque = 200f;
        [SerializeField] private float settleAngularVelocity = 0.1f;
        [SerializeField] private VisualEffect dust;

        [Header("Blockade (pre-placed, inactive, carries TrackBlockade)")]
        [SerializeField] private TrackBlockade blockade;

        [Header("Damage trigger (medium tier)")]
        [SerializeField] private float damageThreshold = 500f;

        private Rigidbody _rb;
        private HingeJoint _hinge;
        private float _accumulatedDamage;
        private bool _hasFallen;
        private bool _dustPlayed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _hinge = GetComponent<HingeJoint>();
        }

        // Local executor: topple physics runs on every client (accepted
        // simplification, see DemolitionPoint). _hasFallen makes it idempotent,
        // so overlapping trigger paths are safe. DemolitionPoint already
        // broadcasts, so it calls this directly on each client.
        public void TriggerCollapse()
        {
            if (_hasFallen)
                return;
            _hasFallen = true;
            _rb.isKinematic = false;
            // hinge axis is local space, AddTorque wants world space
            _rb.AddTorque(transform.TransformDirection(_hinge.axis).normalized * nudgeTorque, ForceMode.VelocityChange);
            StartCoroutine(SettleAndArm());
        }

        [Rpc(SendTo.Everyone)]
        private void CollapseRpc() => TriggerCollapse();

        // called by the grappling module on the pulling player's client only —
        // broadcast so the structure falls for everyone
        public void OnGrapplePull()
        {
            if (!_hasFallen)
                CollapseRpc();
        }

        // called via SendMessage("Damage", amount) by weapons hitting Damageable
        // colliders — on the shooting player's client. Accumulate on the authority
        // so hits from different players stack instead of tracking per-client.
        public void Damage(float damage)
        {
            if (_hasFallen)
                return;
            DamageRpc(damage);
        }

        [Rpc(SendTo.Owner)]
        private void DamageRpc(float damage)
        {
            if (_hasFallen)
                return;
            _accumulatedDamage += damage;
            if (_accumulatedDamage >= damageThreshold)
                CollapseRpc();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // first ground impact after the topple starts -> dust
            if (!_hasFallen || _dustPlayed || !dust)
                return;
            _dustPlayed = true;
            dust.transform.position = collision.GetContact(0).point;
            dust.enabled = true;
            dust.Play();
        }

        private IEnumerator SettleAndArm()
        {
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => _rb.angularVelocity.magnitude < settleAngularVelocity);
            Destroy(_hinge);
            blockade.gameObject.SetActive(true);
            blockade.Arm();
        }
    }
}
