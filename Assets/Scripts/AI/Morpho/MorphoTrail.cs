using System;
using System.Collections;

namespace AI
{
    using UnityEngine;
    using UnityEngine.VFX;

    public class MorphoTrail : MonoBehaviour
    {
        public VisualEffect morphoVFX;
        public MorphoSplineFollower movementScript;

        [Header("Settings")] public float dustMultiplier = 15f;
        public float smoothness = 0.1f;
        public float durationPerSmoke = 8;

        private float _currentEmission;
        private float _velocity;

        private void Start()
        {
            StartCoroutine(VfxUpdate());
        }

        IEnumerator VfxUpdate()
        {
            while (true)
            {
                if (morphoVFX == null || movementScript == null) yield break;

                float targetEmission = movementScript.CurrentSpeed * dustMultiplier;
                _currentEmission = Mathf.SmoothDamp(_currentEmission, targetEmission, ref _velocity, smoothness);
                morphoVFX.SetFloat("DustIntensity", _currentEmission);
                morphoVFX.SetFloat("DustDuration", Mathf.Lerp(3, durationPerSmoke, movementScript.CurrentSpeed / movementScript.MaxSpeed));
                yield return new WaitForSeconds(1f);
            }
        }
    }
}