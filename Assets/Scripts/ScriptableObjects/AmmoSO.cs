using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "Ammo", menuName = "Scriptable Objects/Ammo")]
    public class AmmoSO : ScriptableObject
    {
        [Header("Ammo Properties")]
        public float damage = 10;
        public float fireRate = 1f;
        public int maxAmmo = 20;
        public float reloadTime = 1f;
        public float maxLifetime = 5;
        public float forcePower = 500f;

        [Header("Ammo Prefabs")]
        public GameObject prefab;
        public AudioClip fireSound;
        public GameObject explosionPrefab;
        public GameObject muzzleFlashPrefab;
    }
}