using System;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace BladesCombat
{
    [Serializable]
    public class BladeSharedData
    {
        [SerializeField] private float _FullDamage;
        
        [SerializeField] private Animator _LeftBlade;
        [SerializeField] private Animator _RightBlade;
        
        [SerializeField] private BladeCollisionTrigger _LeftTrigger;
        [SerializeField] private BladeCollisionTrigger _RightTrigger;

        [SerializeField] private Collider[] _Colliders;
        
        public Animator LeftBlade => _LeftBlade;
        public Animator RightBlade => _RightBlade;
        
        public BladeCollisionTrigger LeftTrigger => _LeftTrigger;
        public BladeCollisionTrigger RightTrigger => _RightTrigger;
        public Collider[] Colliders => _Colliders;
        public float FullDamage => _FullDamage;
    }
}