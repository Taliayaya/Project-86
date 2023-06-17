using System;
using System.Collections;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Mecha
{
    public class WeaponModule : MonoBehaviour
    {
        public enum WeaponType
        {
            Primary,
            Secondary
        }
        
        [Header("Settings")]
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private bool listenToEvents = true;
        [SerializeField] private bool holdFire = false;
        [SerializeField] private AmmoSO ammo;
        
        [Header("References")]
        [SerializeField] private Transform gunTransform;
        [SerializeField] private AudioSource gunAudioSource;
        [SerializeField] private Transform cameraTransform;

        [Header("Overrides")]
        [Tooltip(
             "Specify the side of the weapon, it will be used to update the right ammo counter. Only works with Secondary Weapons"),
         SerializeField]
        private string ammoLeftOrRight = "Left";
        
        private bool _canFire = true;
        private Collider _gunTransformCollider;
        private int _currentAmmoRemaining;
        
        private bool _isHeld = false;


        public int CurrentAmmoRemaining
        {
            get => _currentAmmoRemaining;
            set
            {
                _currentAmmoRemaining = value;
                EventManager.TriggerEvent($"OnUpdate{weaponType}Ammo{(weaponType == WeaponType.Secondary ? ammoLeftOrRight : string.Empty)}Amount", _currentAmmoRemaining);
            }
        }

        #region Unity Callbacks

        private void Awake()
        {
            _gunTransformCollider = gunTransform.parent.GetComponent<Collider>();
            CurrentAmmoRemaining = ammo.maxAmmo;
            gunAudioSource.loop = holdFire;
        }

        private void OnEnable()
        {
            if (!listenToEvents) return;
            switch (weaponType)
            {
                case WeaponType.Primary:
                    EventManager.AddListener("OnPrimaryFire", OnFire);
                    break;
                case WeaponType.Secondary:
                    EventManager.AddListener("OnSecondaryFire", OnFire);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDisable()
        {
            if (!listenToEvents) return;
            switch (weaponType)
            {
                case WeaponType.Primary:
                    EventManager.RemoveListener("OnPrimaryFire", OnFire);
                    break;
                case WeaponType.Secondary:
                    EventManager.RemoveListener("OnSecondaryFire", OnFire);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
        
        private IEnumerator FireOnHeld()
        {
            PlayBulletSound(false);
            while (_isHeld && _currentAmmoRemaining > 0)
            {
                Shoot();
                yield return new WaitForSeconds(1/ammo.fireRate);
            }

            yield return new WaitForSeconds(0.1f);
            gunAudioSource.Stop();
        }

        private void OnFire()
        {
            _isHeld = !_isHeld;
            if (_canFire && _currentAmmoRemaining > 0)
            {
                if (holdFire)
                {
                    if (_isHeld)
                    {
                        StopCoroutine(FireOnHeld());
                        StartCoroutine(FireOnHeld());
                    }
                }
                else
                {
                    Shoot();
                    PlayBulletSound();
                }
            }
        }

        private void Shoot()
        {
            FireBullet();
            UpdateAmmoAmount();
        }

        private void FireBullet()
        {
            var bulletDirection = cameraTransform.forward;
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, 500f))
            {
                bulletDirection = (hit.point - gunTransform.position).normalized;
            }

            var bullet = Instantiate(ammo.prefab, gunTransform.position, Quaternion.identity);
            var bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.InitLifeTime(ammo.maxLifetime);
            bulletScript.explosionPrefab = ammo.explosionPrefab;
            bulletScript.damage = ammo.damage;


            var bulletRb = bullet.GetComponent<Rigidbody>();

            _canFire = false;
            bulletRb.AddForce(bulletDirection * ammo.forcePower, ForceMode.Impulse);
            var rot = bulletRb.rotation.eulerAngles;
            bulletRb.rotation = Quaternion.Euler(rot.x, gunTransform.eulerAngles.y, rot.z);
            Physics.IgnoreCollision(bullet.GetComponentInChildren<Collider>(), _gunTransformCollider);
            Invoke(nameof(ResetOnFire), 1 / ammo.fireRate);
        }

        private void PlayBulletSound(bool oneShot = true)
        {
            if (oneShot)
                gunAudioSource.PlayOneShot(ammo.fireSound);
            else
            {
                gunAudioSource.clip = ammo.fireSound;
                gunAudioSource.Play();
            }
        }
        

        private void UpdateAmmoAmount()
        {
            CurrentAmmoRemaining--;
            
            if (_currentAmmoRemaining <= 0)
            {
                Invoke(nameof(ResetAmmo), 1/ammo.reloadTime);
            }
        }

        private void ResetOnFire()
        {
            _canFire = true;
        }
        
        private void ResetAmmo()
        {
            CurrentAmmoRemaining = ammo.maxAmmo;
        }
        
    }
}