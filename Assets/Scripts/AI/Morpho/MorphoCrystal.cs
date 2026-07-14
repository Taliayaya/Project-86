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
            public Renderer renderer;
            public Collider collider;
            public HealthComponent healthComponent;
            
            public float HealthRatio => healthComponent.Health / healthComponent.MaxHealth;
            public Color GetColor(Gradient gradient) => gradient.Evaluate(1f - HealthRatio);
            
            public bool IsDestroyed => healthComponent.Health <= 0;
            
            public UnityEvent onDestroyed;
            public UnityEvent onRegenerated;
            public UnityEvent onActivated;
            public UnityEvent onDeactivated;
            public UnityEvent<float, float> onHealthChanged;
        }

        
        [SerializeField] [GradientUsage(true)] private Gradient activeColorGradient;
        [SerializeField] private float activationDuration = 1.5f;
        [SerializeField] private Color inActiveColor;
        [SerializeField] private float deactivationDuration = 3f;
        [SerializeField] private float blinkDuration = 0.5f;
        [SerializeField] private float blinkStrength = 5f;
        public List<Crystal> sideCrystals;
        public Crystal mainCrystal;
        
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

        private void Activate(Crystal crystal)
        {
            crystal.status = Status.Active;
            crystal.renderer.material.DOColor(crystal.GetColor(activeColorGradient), activationDuration);
            crystal.renderer.material.DOColor(crystal.GetColor(activeColorGradient), _emissionProperty,
                activationDuration);
            crystal.collider.enabled = true;
            
            crystal.onActivated?.Invoke();
        }

        [ContextMenu("Activate")]
        public void Activate()
        {
            sideCrystals.ForEach(Activate);
        }

        public void ActivateMainCrystal()
        {
            Activate(mainCrystal);
        }

        private void Deactivate(Crystal crystal)
        {
            crystal.status = Status.Invincible;
            crystal.renderer.material.DOColor(inActiveColor, deactivationDuration);
            crystal.renderer.material.DOColor(inActiveColor, _emissionProperty, deactivationDuration);
            crystal.collider.enabled = false;
            
            crystal.onDeactivated?.Invoke();
        }
        
        [ContextMenu("Deactivate")]
        public void Deactivate()
        {
            bool bothCrystalsDestroyed = sideCrystals[0].IsDestroyed && sideCrystals[1].IsDestroyed;
            sideCrystals.ForEach(crystal =>
            {
                Deactivate(crystal);
                
                // regenerate crystals if destroyed
                if (bothCrystalsDestroyed)
                {
                    crystal.healthComponent.Health = crystal.healthComponent.MaxHealth;
                    crystal.onRegenerated?.Invoke();
                }
            });
            
            if (mainCrystal.status == Status.Active)
                Deactivate(mainCrystal);
        }

        public void CheckDestroyed()
        {
            if (sideCrystals[0].IsDestroyed && sideCrystals[1].IsDestroyed)
            {
                onBothCrystalsDestroyed?.Invoke();
                agent.SetVariableValue("State", MorphoState.Stunned);
            }

            if (mainCrystal.IsDestroyed)
            {
                agent.SetVariableValue("Phase", Phase.Phase3);
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
            crystal.renderer.material.DOKill();

            Sequence s = DOTween.Sequence();
            Sequence sEmission = DOTween.Sequence();

            // white flash
            s.Append(crystal.renderer.material.DOColor(Color.white * blinkStrength, blinkDuration * 0.5f));
            sEmission.Append(crystal.renderer.material.DOColor(Color.white * blinkStrength, _emissionProperty,
                blinkDuration * 0.5f));

            // orange color becoming stronger
            s.Append(crystal.renderer.material.DOColor(crystal.GetColor(activeColorGradient), blinkDuration * 0.5f));
            sEmission.Append(crystal.renderer.material.DOColor(crystal.GetColor(activeColorGradient), _emissionProperty,
                blinkDuration * 0.5f));
            
            crystal.onHealthChanged?.Invoke(crystal.healthComponent.Health, crystal.healthComponent.MaxHealth);
        }

        public void OnMainCrystalTookDamage(HealthComponent _, DamagePackage damagePackage)
        {
            CrystalTakeDamage(mainCrystal, damagePackage);
        }

        public void OnLeftCrystalTookDamage(HealthComponent _, DamagePackage damagePackage)
        {
            CrystalTakeDamage(sideCrystals[0], damagePackage);
        }
        
        public void OnRightCrystalTookDamage(HealthComponent _, DamagePackage damagePackage)
        {
            CrystalTakeDamage(sideCrystals[1], damagePackage);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Damage")]
        public void TestDamage()
        {
            OnLeftCrystalTookDamage(null, new DamagePackage());
            OnRightCrystalTookDamage(null, new DamagePackage());
        }
#endif
    }
}