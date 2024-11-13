using System;
using System.Collections;
using Gameplay.Units;
using UnityEngine;

namespace Gameplay.Mecha
{
    public class AutoWeaponModule : WeaponModule
    {
        [Header("Auto Weapon Module")]
        [SerializeField] private float minAngleToShoot = 15f;
        [SerializeField] private Unit target;
        [SerializeField] private float holdFireTime = 2f;
        private void Start()
        {
            StartCoroutine(ShootIfEnemyInSight());
        }

        public bool EnemyInRange(Transform turret)
        {
            if (target == null)
                return false;
            var direction = target.transform.position - turret.position;
            var angle = Vector3.Angle(direction, turret.forward);

            return angle < minAngleToShoot * 0.5f;
        }

        private IEnumerator ShootIfEnemyInSight()
        {
            while (true)
            {
                if (target != null)
                {
                    if (HoldFire)
                        StartCoroutine(ShootHoldDuringTime(holdFireTime, EnemyInRange));
                    else
                        StartCoroutine(ShootDuringTime(holdFireTime, EnemyInRange));
                    yield return new WaitForSeconds(holdFireTime);
                }
                else
                {
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }
        
        public void SetTarget(Unit newTarget)
        {
            target = newTarget;
        }
    }
}