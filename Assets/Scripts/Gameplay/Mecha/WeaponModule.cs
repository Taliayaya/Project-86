using System;
using System.Collections;
using ScriptableObjects;
using UI;
using UI.HUD;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace Gameplay.Mecha
{
    public class WeaponModule : Module
    {
        public enum WeaponType
        {
            Primary,
            Secondary
        }
        
        [Header("Settings")]
        [SerializeField] private WeaponType weaponType;
        [FormerlySerializedAs("listenToEvents")] [SerializeField] private bool listenOrTriggersEvents = true;
        [SerializeField] private bool holdFire = false;
        [SerializeField] private AmmoSO ammo;
        [SerializeField] private LayerMask fireBulletLayerMask = 1;
        
        [Header("References")]
        [SerializeField] private Transform gunTransform;
        [SerializeField] private AudioSource gunAudioSource;
        [SerializeField] private Transform cameraTransform;
        
        [Header(("Muzzle"))]
        [SerializeField] private VisualEffect muzzleFlash;
        [SerializeField] private Light muzzleLight;

        [SerializeField] private Collider myParentColliderToIgnore;

        [Header("Overrides")]
        [Tooltip(
             "Specify the side of the weapon, it will be used to update the right ammo counter. Only works with Secondary Weapons"),
         SerializeField]
        private string ammoLeftOrRight = "Left";
        
        public bool canFire = true;
        private Collider _gunTransformCollider;
        private int _currentAmmoRemaining;
        
        private bool _isHeld = false;

        public float FireRate => ammo.fireRate;
        public bool HoldFire => holdFire;
        public int MaxAmmo => ammo.maxAmmo;
        public WeaponType Type => weaponType;
        public float ReloadTime => ammo.reloadTime;
        public string WeaponName => ammo.gunTypeName;


        public int CurrentAmmoRemaining
        {
            get => _currentAmmoRemaining;
            set
            {
                _currentAmmoRemaining = value;
                if (listenOrTriggersEvents)
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
            EventManager.AddListener("OnPause", OnPause);
            EventManager.AddListener("OnResume", OnResume);
            if (!listenOrTriggersEvents) return;
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
            EventManager.RemoveListener("OnPause", OnPause);
            EventManager.RemoveListener("OnResume", OnResume);
            if (!listenOrTriggersEvents) return;
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
        
        private void OnPause()
        {
            _isHeld = false;
            gunAudioSource.Pause();
        }

        private void OnResume()
        {
            gunAudioSource.UnPause();
        }
        
        private void TriggerNoAmmoPopUp()
        {
            Debug.Log("No Ammo");
            EventManager.TriggerEvent("OnNoAmmo", new AmmoPopUpData(ammo.gunTypeName, 2f));
        }
        
        /// <summary>
        /// Player related method
        /// </summary>
        /// <returns></returns>
        private IEnumerator FireOnHeld()
        {
            if (_currentAmmoRemaining <= 0)
            {
                TriggerNoAmmoPopUp();
                yield break;
            }

            PlayBulletSound(false);
            while (_isHeld && _currentAmmoRemaining > 0)
            {
                Shoot();
                yield return new WaitForSeconds(1/ammo.fireRate);
            }

            yield return new WaitForSeconds(0.1f);
            gunAudioSource.Stop();
        }


        /// <summary>
        /// AI related method
        /// </summary>
        /// <param name="forwardTransform"></param>
        /// <param name="time"></param>
        /// <param name="canShoot"></param>
        /// <returns></returns>
        public IEnumerator ShootDuringTime(float time, Func<bool> canShoot)
        {
            var startTime = Time.time;
            while (Time.time - startTime < time)
            {
                if (!canShoot())
                    yield return new WaitForSeconds(0.1f);
                Shoot(cameraTransform);
                yield return new WaitForSeconds(1/ammo.fireRate);
            }
        }

        /// <summary>
        /// AI related method
        /// </summary>
        /// <param name="forwardTransform"></param>
        /// <param name="time"></param>
        /// <param name="canShoot"></param>
        /// <returns></returns>
        public IEnumerator ShootHoldDuringTime(float time, Func<bool> canShoot)
        {
            PlayBulletSound(false);
            var startTime = Time.time;
            
            while (Time.time - startTime < time)
            {
                if (!canShoot())
                    yield return new WaitForSeconds(0.1f);
                Shoot(cameraTransform);
                yield return new WaitForSeconds(1/ammo.fireRate);
            }
            
            yield return new WaitForSeconds(0.1f);
            gunAudioSource.Stop();
            
        }


        private void OnFire()
        {
            _isHeld = !_isHeld;
            if (canFire && _currentAmmoRemaining > 0)
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
            if (_currentAmmoRemaining <= 0)
                TriggerNoAmmoPopUp();
        }
        
        public void Shoot(Transform origin)
        {
            if (_currentAmmoRemaining <= 0)
                return;
            FireBullet(origin);
            UpdateAmmoAmount();
        }

        private void Shoot()
        {
            Shoot(cameraTransform);
        }

        private IEnumerator MuzzleFlash()
        {
            muzzleFlash.Play();
            muzzleLight.enabled = false;
            yield return new WaitForSeconds(0.1f);
            muzzleLight.enabled = true;
            yield return new WaitForSeconds(0.1f);
            muzzleLight.enabled = false;
        }
        
        private Coroutine _muzzleFlashCoroutine;
        
        private void FireBullet(Transform origin)
        {
            var bulletDirection = origin.forward;
            if (Physics.Raycast(origin.position, origin.forward, out var hit, 500f, fireBulletLayerMask))
            {
                bulletDirection = (hit.point - gunTransform.position).normalized;
                //Debug.DrawRay(gunTransform.position, bulletDirection * 100, Color.red, 1f);
            }
            else
            {
                //Debug.DrawRay(gunTransform.position, bulletDirection * 100, Color.green, 1f);
            }

            if (muzzleFlash)
            {
                if (_muzzleFlashCoroutine != null)
                    StopCoroutine(_muzzleFlashCoroutine);
                _muzzleFlashCoroutine = StartCoroutine(MuzzleFlash());
            }


            var bullet = Instantiate(ammo.prefab, gunTransform.position, Quaternion.identity);
            var bulletCollider = bullet.GetComponentInChildren<Collider>();
            Physics.IgnoreCollision(bulletCollider, _gunTransformCollider, true);
            if (myParentColliderToIgnore != null)
                Physics.IgnoreCollision(bulletCollider, myParentColliderToIgnore, true);
            var bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.Init(ammo, faction);
            bulletScript.InitLifeTime(ammo.maxLifetime);

            var bulletRb = bullet.GetComponent<Rigidbody>();

            canFire = false;
            bulletRb.AddForce(bulletDirection * ammo.forcePower, ForceMode.Impulse);
            var rot = bulletRb.rotation.eulerAngles;
            bulletRb.rotation = Quaternion.Euler(rot.x, gunTransform.eulerAngles.y, rot.z);
            if (ammo.reloadSound != null)
                Invoke(nameof(PlayReloadSound), 0.3f);

            Invoke(nameof(ResetOnFire), 1 / ammo.fireRate);
        }

        private void PlayReloadSound()
        {
            gunAudioSource.PlayOneShot(ammo.reloadSound);
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
                //if (listenOrTriggersEvents) TriggerNoAmmoPopUp();
                if (faction == Faction.Legion)
                    Invoke(nameof(ResetAmmo), 1/ammo.reloadTime);
            }
        }

        private void ResetOnFire()
        {
            canFire = true;
        }
        
        public void ResetAmmo()
        {
            CurrentAmmoRemaining = ammo.maxAmmo;
        }
        
    }
}