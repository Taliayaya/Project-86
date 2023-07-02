using Gameplay.Mecha;
using UnityEngine;

namespace ScriptableObjects.Gameplay
{
    [CreateAssetMenu(fileName = "AmmoStack", menuName = "Scriptable Objects/Gameplay/AmmoStack")]
    public class AmmoStackSO : ScriptableObject
    {
        public WeaponModule.WeaponType weaponType;
        public int ammoAmount = 100;
    }
}