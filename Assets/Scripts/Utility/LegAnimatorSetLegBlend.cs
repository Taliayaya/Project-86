using System;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

namespace Utility
{
    [RequireComponent(typeof(LegsAnimator))]
    public class LegAnimatorSetLegBlend : MonoBehaviour
    {
        [SerializeField] private int legIndexLeft;
        [SerializeField] private int legIndexRight;
        [SerializeField] private float fadeDuration;
        private LegsAnimator _animator;

        private void Awake()
        {
            if (!_animator) _animator = GetComponent<LegsAnimator>();
        }

        public void SetLeftLegBlend(float blend)
        {
            _animator.User_FadeLeg(legIndexLeft, blend, fadeDuration);
        }

        public void SetRightLegBlend(float blend)
        {
            _animator.User_FadeLeg(legIndexRight, blend, fadeDuration);
        }
    }
}