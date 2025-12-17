using System;
using System.Linq;
using BladesCombatTutorial;
using Gameplay;
using UnityEngine;
namespace BladesCombat
{
    public class BladeCollision : BladeComponent
    {
        public override bool UseTriggers => true;

        public event OnBladeCollision OnCollision;
        
        public override void OnTriggerEnter(Collider other, object additionalData = null)
        {
            if (additionalData is not TriggerData data)
            {
                Debug.Log($"Trigger not from blade. Must be for something else");
                return;
            }

            bool isPlayer = SharedData.Colliders.Contains(other);
            if (isPlayer) return;

            OnCollision?.Invoke(data.IsLeftBlade, other);

            if (other.TryGetComponent(out BladeHitReceiver receiver))
            {
                StabType stabType;
                if (data.IsLeftBlade)
                {
                    stabType = Switcher.IsLeftActive ? StabType.Side : StabType.Forward;
                }
                else
                {
                    stabType = Switcher.IsRightActive ? StabType.Side : StabType.Forward;
                }

                receiver.TakeDamage(stabType);
            }
            
            if (!other.TryGetComponent(out IHealth health))
            {
                Debug.Log($"{other.name} doesn't have health component");
                return;
            }

            if (other.TryGetComponent(out BladeNotDamageable _))
            {
                Debug.Log($"Not damageable {other.gameObject}, ignoring", other.gameObject);
                return;
            }
            
            bool isFriendlyUnit = health.Faction == Faction.Republic;

            if (isFriendlyUnit)
            {
                if (data.IsLeftBlade && !Switcher.IsLeftActive)
                {
                    return;
                }
                if (!data.IsLeftBlade && !Switcher.IsRightActive)
                {
                    return;
                }
            }

            Debug.LogError($"{other.name} taking damage: {SharedData.FullDamage}");
            health.TakeDamage(new DamagePackage()
            {
                Type = DamageType.Blade,
                Faction = Faction.Republic,
                SourcePosition = Vector3.zero,
                
                Blade = new BladeData()
                {
                    Damage = SharedData.FullDamage
                },
            });

        }

    }
    
    public delegate void OnBladeCollision(bool isLeftBlade, Collider other);
}