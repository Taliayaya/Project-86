using System;
using System.Collections.Generic;
using Gameplay.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Mecha
{
    public struct ReloadModuleData
    {
        public int WeaponIndex;
        public int WeaponCount;
        public Scavenger Scavenger;
        public string WeaponName;
        public float ReloadTime;
        
        public ReloadModuleData(int weaponIndex, int weaponCount, Scavenger scavenger, string weaponName, float reloadTime)
        {
            WeaponIndex = weaponIndex;
            WeaponCount = weaponCount;
            Scavenger = scavenger;
            WeaponName = weaponName;
            ReloadTime = reloadTime;
        }
    }
    [RequireComponent(typeof(SphereCollider))]
    public class ReloadModule : Module
    {
        private List<Scavenger> _scavengerInRange = new List<Scavenger>();
        [Header("Settings")]
        [SerializeField] private bool listenToEvents;
        [Header("References")]
        [SerializeField] private WeaponModule[] weaponModules;

        public Unit unit;

        [CanBeNull]
        public Scavenger GetClosestScavenger()
        {
            if (_scavengerInRange.Count == 0)
                return null;
            var closest = _scavengerInRange[0];
            var closestDistance = Vector3.Distance(transform.position, closest.transform.position);
            foreach (var scavenger in _scavengerInRange)
            {
                var distance = Vector3.Distance(transform.position, scavenger.transform.position);
                if (distance < closestDistance)
                {
                    closest = scavenger;
                    closestDistance = distance;
                }
            }

            return closest;
        }
        
        [CanBeNull] private Scavenger _scavenger;
        
        #region Unity Callbacks

        private void Awake()
        {
            if (weaponModules.Length == 0)
            {
                weaponModules = GetComponentsInChildren<WeaponModule>();
            }
        }

        private void OnEnable()
        {
            if (listenToEvents)
            {
                EventManager.AddListener("OnReload", Reload);
            }
        }
        
        private void OnDisable()
        {
            if (listenToEvents)
            {
                EventManager.RemoveListener("OnReload", Reload);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var scavenger = other.GetComponentInParent<Scavenger>();
            if (scavenger)
            {
                _scavengerInRange.Add(scavenger);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            var scavenger = other.GetComponentInParent<Scavenger>();
            if (scavenger)
            {
                _scavengerInRange.Remove(scavenger);
            }
        }

        #endregion

        public void Reload()
        {
            Debug.Log("Reload");
            _scavenger = GetClosestScavenger();
            if (_scavenger)
            {
                _scavenger.Reload(weaponModules, unit, true);
            }

        }
    }
}