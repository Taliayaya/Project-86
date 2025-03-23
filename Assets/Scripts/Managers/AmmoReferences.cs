using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Managers
{
    public class AmmoReferences : Singleton<AmmoReferences>
    {
        private Dictionary<string, int> _ammoDictionary = new Dictionary<string, int>();
        private AmmoSO[] _ammoSos;

        protected override void OnAwake()
        {
            base.OnAwake();
            _ammoSos = Resources.LoadAll<AmmoSO>("ScriptableObjects/Ammo");
            for (int i = 0; i < _ammoSos.Length; i++)
            {
                _ammoDictionary.Add(_ammoSos[i].name, i);
            }
        }
        
        public AmmoSO GetAmmo(string ammoName)
        {
            return GetAmmo(_ammoDictionary[ammoName]);
        }
        
        public AmmoSO GetAmmo(int index)
        {
            return _ammoSos[index];
        }
        
        public int GetAmmoIndex(string ammoName)
        {
            return _ammoDictionary[ammoName];
        }
    }
}