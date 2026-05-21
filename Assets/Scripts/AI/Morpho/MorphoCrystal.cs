using System;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay;
using Gameplay.Units;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;

namespace AI
{
    public class MorphoCrystal : HealthComponent
    {
        public enum Status
        {
            Invincible,
            Active,
            Destroyed
        }

        [Serializable]
        public class Crystal
        {
            public Status status;
            public MeshRenderer meshRenderer;
            public Collider collider;
            public HealthComponent healthComponent;
            
            public float HealthRatio => healthComponent.Health / healthComponent.MaxHealth;
            public Color GetColor(Gradient gradient) => gradient.Evaluate(1f - HealthRatio);
            
            public bool IsDestroyed => healthComponent.Health <= 0;
            
            public UnityEvent onDestroyed;
            public UnityEvent onRegenerated;
            public UnityEvent onActivated;
            public UnityEvent onDeactivated;
        }

        
        [SerializeField] [GradientUsage(true)] private Gradient activeColorGradient;
        [SerializeField] private float activationDuration = 1.5f;
        [SerializeField] private Color inActiveColor;
        [SerializeField] private float deactivationDuration = 3f;
        [SerializeField] private float blinkDuration = 0.5f;
        [SerializeField] private float blinkStrength = 5f;
        public List<Crystal> sideCrystals;
        
        public BehaviorGraphAgent agent;

        public UnityEvent onBothCrystalsDestroyed;

        protected override void Start()
        {
            base.Start();
            Deactivate();
        }
        
        public Status GetStatus
        {
            get
            {
                if (sideCrystals[0].status == Status.Invincible)
                    return Status.Invincible;
                if (sideCrystals[0].IsDestroyed && sideCrystals[1].IsDestroyed)
                    return Status.Destroyed;
                return Status.Active;
            }
        }

        private readonly string _emissionProperty = "_EmissionColor";
        
        [ContextMenu("Activate")]
        public void Activate()
        {
            sideCrystals.ForEach(crystal =>
            {
                crystal.status = Status.Active;
                crystal.meshRenderer.material.DOColor(crystal.GetColor(activeColorGradient), activationDuration);
                crystal.meshRenderer.material.DOColor(crystal.GetColor(activeColorGradient), _emissionProperty, activationDuration);
                crystal.collider.enabled = true;
                crystal.onActivated?.Invoke();
            });
        }
        
        [ContextMenu("Deactivate")]
        public void Deactivate()
        {
            bool bothCrystalsDestroyed = sideCrystals[0].IsDestroyed && sideCrystals[1].IsDestroyed;
            sideCrystals.ForEach(crystal =>
            {
                crystal.status = Status.Invincible;
                crystal.meshRenderer.material.DOColor(inActiveColor, deactivationDuration);
                crystal.meshRenderer.material.DOColor(inActiveColor, _emissionProperty, deactivationDuration);
                crystal.collider.enabled = false;
                
                // regenerate crystals if destroyed
                if (bothCrystalsDestroyed)
                {
                    crystal.healthComponent.Health = crystal.healthComponent.MaxHealth;
                    crystal.onRegenerated?.Invoke();
                }
                crystal.onDeactivated?.Invoke();

            });
        }

        public void CheckDestroyed()
        {
            if (sideCrystals[0].IsDestroyed && sideCrystals[1].IsDestroyed)
            {
                onBothCrystalsDestroyed?.Invoke();
                agent.SetVariableValue("State", MorphoState.Stunned);
            }
        }

        // I take damage
        public override void OnTakeDamage(DamagePackage damagePackage)
        {
            base.OnTakeDamage(damagePackage);
        }

        private void CrystalTakeDamage(Crystal crystal, DamagePackage damagePackage)
        {
            if (crystal.IsDestroyed)
            {
                crystal.status = Status.Destroyed;
                crystal.collider.enabled = false;
                CheckDestroyed();
                crystal.onDestroyed?.Invoke();
            }
            crystal.meshRenderer.material.DOKill();

            Sequence s = DOTween.Sequence();
            Sequence sEmission = DOTween.Sequence();

            // white flash
            s.Append(crystal.meshRenderer.material.DOColor(Color.white * blinkStrength, blinkDuration * 0.5f));
            sEmission.Append(crystal.meshRenderer.material.DOColor(Color.white * blinkStrength, _emissionProperty,
                blinkDuration * 0.5f));

            // orange color becoming stronger
            s.Append(crystal.meshRenderer.material.DOColor(crystal.GetColor(activeColorGradient), blinkDuration * 0.5f));
            sEmission.Append(crystal.meshRenderer.material.DOColor(crystal.GetColor(activeColorGradient), _emissionProperty,
                blinkDuration * 0.5f));
        }

        public void OnLeftCrystalTookDamage(HealthComponent _, DamagePackage damagePackage)
        {
            CrystalTakeDamage(sideCrystals[0], damagePackage);
        }
        
        public void OnRightCrystalTookDamage(HealthComponent _, DamagePackage damagePackage)
        {
            CrystalTakeDamage(sideCrystals[1], damagePackage);
        }

        [ContextMenu("Test Damage")]
        public void TestDamage()
        {
            OnLeftCrystalTookDamage(null, new DamagePackage());
            OnRightCrystalTookDamage(null, new DamagePackage());
        }
    }
}