using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "Ammo", menuName = "Scriptable Objects/Ammo")]
    public class AmmoSO : ScriptableObject
    {
        [Header("Information")] public string gunTypeName = "Main Gun : 57mm APFSDS";
        [Header("Ammo Properties")]
        public AnimationCurve damageCurve;
        public float fireRate = 1f;
        public int maxAmmo = 20;
        public float reloadTime = 1f;
        public float maxLifetime = 5;
        public float forcePower = 500f;
        
        [Header("Explosion Properties")]
        [Range(0, 200)] public float explosionRadius = 1f;
        public LayerMask explosionLayerMask = 1;

        [Header("Ammo Prefabs")]
        public GameObject prefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public GameObject explosionPrefab;
        public GameObject muzzleFlashPrefab;
        
        
        public AudioClip onHitSound;
        public GameObject missEffect;

        public float Damage(int time) => damageCurve.Evaluate(time);

    }
}