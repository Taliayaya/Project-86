using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace AI
{
    /// <summary>
    /// The Morpho's behavior graph only runs on the owner (NetcodeRunOnlyOnOwner)
    /// and NetworkTransform moves the body on the other clients. This replicates
    /// the blackboard CurrentSpeed so those clients can drive the walk animation
    /// and dust trail, which the graph normally drives (TrackFollowerAction /
    /// MorphoTrailAction). Attach next to the BehaviorGraphAgent.
    /// </summary>
    public class MorphoNetworkSync : NetworkBehaviour
    {
        [SerializeField] private BehaviorGraphAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private VisualEffect dustVfx;

        [Header("Mirror the graph's tuning values")]
        [SerializeField] private float maxSpeed = 50f;
        [SerializeField] private float walkMaxAnimationSpeed = 2.5f;
        [SerializeField] private float dustMultiplier = 15f;
        [SerializeField] private float durationPerSmoke = 8f;

        private readonly NetworkVariable<float> _speed = new();
        private BlackboardVariable<float> _currentSpeed;

        private void Update()
        {
            if (HasAuthority)
            {
                if (_currentSpeed == null && !agent.GetVariable("CurrentSpeed", out _currentSpeed))
                    return;
                // ponytail: 0.25 dead-band keeps the NetworkVariable from spamming
                // deltas every frame; animation speed doesn't need more precision
                if (Mathf.Abs(_currentSpeed.Value - _speed.Value) > 0.25f)
                    _speed.Value = _currentSpeed.Value;
                return; // owner's graph already drives animator + vfx
            }

            if (animator != null)
            {
                // isWalking replicates via the NetworkAnimator already on the root;
                // only Animator.speed needs manual sync. Stopped: speed 1 so idle
                // plays normally (TrackFollowerAction does the same when braking;
                // also keeps the stationary FakeMorpho animated).
                animator.speed = _speed.Value > 0.5f
                    ? _speed.Value / maxSpeed * walkMaxAnimationSpeed
                    : 1f;
            }
            if (dustVfx != null)
            {
                dustVfx.SetFloat("DustIntensity", _speed.Value * dustMultiplier);
                dustVfx.SetFloat("DustDuration", Mathf.Lerp(3f, durationPerSmoke, _speed.Value / maxSpeed));
            }
        }
    }
}
