using System;
using Unity.Netcode.Components;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace BladesCombat
{
    [Serializable]
    public class BladeSharedData
    {
        [SerializeField] private float _FullDamage;
        
        [SerializeField] private NetworkAnimator _BothBlade;
        [SerializeField] private NetworkAnimator _LeftBlade;
        [SerializeField] private NetworkAnimator _RightBlade;
        
        [SerializeField] private BladeCollisionTrigger _LeftTrigger;
        [SerializeField] private BladeCollisionTrigger _RightTrigger;

        [SerializeField] private Collider[] _Colliders;
        
        public NetworkAnimator LeftBlade => _LeftBlade;
        public NetworkAnimator RightBlade => _RightBlade;
        public NetworkAnimator BothBlade => _BothBlade;
        
        public BladeCollisionTrigger LeftTrigger => _LeftTrigger;
        public BladeCollisionTrigger RightTrigger => _RightTrigger;
        public Collider[] Colliders => _Colliders;
        public float FullDamage => _FullDamage;
    }
}