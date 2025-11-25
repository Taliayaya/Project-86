using System;
using Cinemachine;
using Managers;
using ScriptableObjects;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay
{
    public class Bullet : NetworkBehaviour
    {
        private Vector3 _origin;
        private Faction _factionOrigin; // To avoid killing each other
        public AmmoSO Ammo => _ammoSo;
        private AmmoSO _ammoSo;
        private NetworkVariable<int> _ammoName = new NetworkVariable<int>();

        private float Damage => Ammo.damageCurve.Evaluate((_origin - transform.position).magnitude);

        public Rigidbody rb;

        private DamagePackage _damagePackage;
        private float _lifeTime;
        private CinemachineImpulseSource _impulseSource;

        #region Unity Callbacks

        private void Start()
        {
            if (!rb)
                rb = GetComponent<Rigidbody>();
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            var layer = LayerMask.NameToLayer("Projectiles");
            Physics.IgnoreLayerCollision(layer, layer, true);
        }

        private void OnEnable()
        {
            _origin = transform.position;
        }

        public void Init(AmmoSO ammoSo, Faction factionOrigin)
        {
            _ammoSo = ammoSo;
            _ammoName.Value = AmmoReferences.Instance.GetAmmoIndex(ammoSo.name);
            _factionOrigin = factionOrigin;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log("Bullet spawned " + _ammoName.Value);
            // probably not opti at all, perhaps have a class that have all loaded ammo and just fetch reference instead
            _ammoSo = AmmoReferences.Instance.GetAmmo(_ammoName.Value);
            Debug.Log("Bullet spawned " + _ammoName.Value + " " + _ammoSo);
        }

        public void InitLifeTime(float lifeTime)
        {
            Invoke(nameof(Expire), lifeTime);
        }

        private void ApplyCameraShake()
        {
            if (_impulseSource == null) return;
            _impulseSource.GenerateImpulse(Ammo.explosionForce);
        }

        private void ApplyExplosionQuake(Vector3 origin)
        {
            Collider[] results = new Collider[20];
            var size = Physics.OverlapSphereNonAlloc(origin, Ammo.explosionRadiusQuake, results);
            for (int i = 0; i < size; i++)
            {
                var col = results[i];
                if (col.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddExplosionForce(Ammo.explosionForce, origin, Ammo.explosionRadiusQuake);
                }
            }
        }


        private void OnCollisionEnter(Collision other)
        {
            if (!IsSpawned || !HasAuthority)
                return;
            _damagePackage = new DamagePackage()
            {
                Type = DamageType.Bullet,
                Faction = _factionOrigin,
                
                Bullet = new BulletData()
                {
                    Damage = Damage,
                    Size = Ammo.bulletSize,
                    HitPoint = other.contacts[0].point,
                },
                Audio = Ammo.onHitSound,
            };
            ApplyCameraShake();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Debug.Log("bullet hit " + other.gameObject.name + other.collider.name);
            IHealth health;
            bool hasHealth = other.collider.TryGetComponent(out health);
            bool parentHasHealth = false;
            if (!hasHealth)
                parentHasHealth = other.rigidbody && other.rigidbody.TryGetComponent(out health);
            if (hasHealth || parentHasHealth)
            {
                // If the bullet hit a non-hitbox, don't damage it and play a special effect
                // like the bullet ricocheting off the armor
                if (other.collider.CompareTag("NonHitbox"))
                {
                    DeflectBulletRpc(other.contacts[0].point, other.contacts[0].normal);
                    return;
                }

                PoolManager.Instance.BackToPool(gameObject);
                Debug.Log("Bullet hit " + other.collider.name + " on " + other.gameObject.name + " for " +
                          _damagePackage.Bullet.Damage + " damage");
                DamageResponse response = health.TakeDamage(_damagePackage);
                if (response.Status == DamageResponse.DamageStatus.Deflected)
                {
                    DeflectBulletRpc(other.contacts[0].point, other.contacts[0].normal);
                    return;
                }

                if (_ammoSo.hitEffect)
                {
                    // implement on hit effect
                    var rotation = Quaternion.LookRotation(other.contacts[0].point - _origin);
                    var effect = PoolManager.Instance.Instantiate(_ammoSo.hitEffect, _damagePackage.Bullet.HitPoint, rotation);
                    effect.transform.SetParent(other.collider.transform, true);
                }

                return;
            }
            else
            {
                if (Ammo.missEffect != null)
                {
                    // used to be transform.position
                    MissBulletEffectRpc(other.contacts[0].point);
                }
            }

            if (Ammo.explosionRadius == 0)
            {
                PoolManager.Instance.BackToPool(gameObject);
                return;
            }

            var colliders = new Collider[5];
            var position = other.contacts[0].point;
            var size = Physics.OverlapSphereNonAlloc(position, Ammo.explosionRadius, colliders,
                Ammo.explosionLayerMask);
            Debug.DrawLine(position, position + Vector3.up * 10, Color.red, 10f);
            for (int i = 0; i < size; i++)
            {
                var col = colliders[i];
                if (col.TryGetComponent(out IHealth health2))
                {
                    health2.TakeDamage(_damagePackage);
                }
            }

            Instantiate(Ammo.explosionPrefab, position, Quaternion.LookRotation(position - _origin));
            PoolManager.Instance.BackToPool(gameObject);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void DeflectBulletRpc(Vector3 point, Vector3 normal)
        {
            if (Ammo.armorMissEffect != null)
            {
                Vector3 reflectedAngle = Vector3.Reflect(transform.forward, normal);
                var effect = Instantiate(Ammo.armorMissEffect, point,
                    Quaternion.LookRotation(reflectedAngle));
                effect.transform.localScale *= Ammo.armorMissEffectSizeMult;
                Debug.Log("Armor Miss Effect");
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void MissBulletEffectRpc(Vector3 point)
        {
            GameObject missEffect;
            if (Ammo.missEffectLookTop)
                missEffect = PoolManager.Instance.Instantiate(Ammo.missEffect, point, Quaternion.Euler(-90, 0, 0));
            else
                missEffect = PoolManager.Instance.Instantiate(Ammo.missEffect, point,
                    Quaternion.LookRotation(point - _origin));

            if (missEffect.TryGetComponent<ParticleSystem>(out var particles))
            {
                particles.Clear();
                particles.Play();
            }
            missEffect.transform.localScale *= Ammo.missEffectSizeMult;
        }

        #endregion

        private void FixedUpdate()
        {
            _lifeTime += Time.fixedDeltaTime;
        }

        private void Expire()
        {
            PoolManager.Instance.BackToPool(gameObject);
        }
    }
}