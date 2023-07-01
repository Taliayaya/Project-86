using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Mecha;
using ScriptableObjects.Gameplay;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Gameplay.Units
{
    [RequireComponent(typeof(SphereCollider))]
    public class Scavenger : Unit
    {
        [SerializeField] private List<AmmoStackSO> ammoStacks;
        
        private List<AmmoStackSO> _copyAmmoStacks;

        public bool IsReloading { get; private set; } = false;
        [SerializeField] private bool emitEvents;

        private int GetAmmoIndex(WeaponModule.WeaponType weaponType)
        {
            return ammoStacks.FindIndex((w) => w.weaponType == weaponType && w.ammoAmount > 0);
        }
        
        #region Unity Callbacks

        public override void Awake()
        {
            base.Awake();
            _copyAmmoStacks = new List<AmmoStackSO>(ammoStacks.Count);
            foreach (var ammoStack in ammoStacks)
                _copyAmmoStacks.Add(Instantiate(ammoStack));
        }

        #endregion

        #region Reload

        private void Reload(WeaponModule weaponModule)
        {
            UpdateAmmoAmount(weaponModule);
            
        }

        public void Reload(WeaponModule[] weaponModules, Unit unit)
        {
            if (IsReloading)
                return;
            // Preventing firing while reloading
            unit.State = UnitState.Reloading;
            foreach (var weaponModule in weaponModules)
                weaponModule.canFire = false;
            StartCoroutine(ReloadCoroutine(weaponModules, unit));
        }

        private IEnumerator ReloadCoroutine(WeaponModule[] weaponModules, Unit unit)
        {
            IsReloading = true;
            for (int i = 0; i < weaponModules.Length; i++)
            {
                var weaponModule = weaponModules[i];
                if (emitEvents)
                    EventManager.TriggerEvent("OnReloadWeaponModule", new ReloadModuleData(i, weaponModules.Length, this, weaponModule.WeaponName, weaponModule.ReloadTime));
                yield return new WaitForSeconds(weaponModule.ReloadTime);
                Reload(weaponModule);
            }
            
            // Allowing firing again
            IsReloading = false;
            foreach (var weaponModule in weaponModules)
                weaponModule.canFire = true;
            unit.State = UnitState.Default;
        }
        private void UpdateAmmoAmount(WeaponModule weaponModule)
        {
            int index = GetAmmoIndex(weaponModule.Type);
            int ammoToAdd = Mathf.Clamp(_copyAmmoStacks[index].ammoAmount, 0, weaponModule.MaxAmmo - weaponModule.CurrentAmmoRemaining);
            _copyAmmoStacks[index].ammoAmount -= ammoToAdd;
            weaponModule.CurrentAmmoRemaining += ammoToAdd;
        }
        
        
        #endregion
    }
}