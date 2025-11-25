using System;
using System.Collections;
using Cinemachine;
using Managers;
using ScriptableObjects;
using UI;
using UI.HUD;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] private float maxRaycastDistance = 500;
        public bool aiIgnore = false;


        public UnityEvent onFire;
        [Header("References")]
        [SerializeField] private Transform gunTransform;
        [SerializeField] private AudioSource gunAudioSource;
        [SerializeField] private AudioSource reloadAudioSource;
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
        [SerializeField] private Transform gunCheckCanShoot;
        [SerializeField] private NetworkAnimator _animator;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Vector3 recoilBodyTorque;
        
        
        public bool canFire = true;
        
        private Collider _gunTransformCollider;
        [Header("Live data")]
        [SerializeField] private int _currentAmmoRemaining;

        private int _recoilLayer = 0;
        
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

        protected virtual void Awake()
        {
            if (gunCheckCanShoot == null)
                gunCheckCanShoot = transform;
            _gunTransformCollider = gunTransform.parent.GetComponent<Collider>();
            if (_animator)
                _recoilLayer = _animator.Animator.GetLayerIndex("Cannon");
            //gunAudioSource.loop = holdFire;
        }

        private void Start()
        {
            CurrentAmmoRemaining = ammo.maxAmmo;
        }

        protected virtual void OnEnable()
        {
            EventManager.AddListener("OnPause", OnPause);
            EventManager.AddListener("OnResume", OnResume);
            if (!listenOrTriggersEvents) return; 
            Debug.Log($"OnEnable {transform.name}, {listenOrTriggersEvents}");
            RegisterPlayerEvents();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!HasAuthority)
                OnDisable();
        }


        protected virtual void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPause);
            EventManager.RemoveListener("OnResume", OnResume);
            if (!listenOrTriggersEvents) return;
            UnRegisterPlayerEvents();
            
        }
        
        private void RegisterPlayerEvents()
        {
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
        private void UnRegisterPlayerEvents()
        {
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
            
            while (_isHeld && _currentAmmoRemaining > 0)
            {
                PlayBulletSoundRpc(false);
                Shoot();
                yield return new WaitForSeconds(1/ammo.fireRate);
            }
            if (_currentAmmoRemaining <= 0)
            {
                TriggerNoAmmoPopUp();
            }


            yield return new WaitForSeconds(0.1f);
        }


        public Coroutine StartShootDuringTime(float time, Func<Transform, bool> canShoot)
        {
            _shootCoroutine = StartCoroutine(ShootDuringTime(time, canShoot));
            return _shootCoroutine;
        }
        private Coroutine _shootCoroutine;
        /// <summary>
        /// AI related method
        /// </summary>
        /// <param name="forwardTransform"></param>
        /// <param name="time"></param>
        /// <param name="canShoot"></param>
        /// <returns></returns>
        public IEnumerator ShootDuringTime(float time, Func<Transform, bool> canShoot)
        {
            var startTime = Time.time;
            float fireRate = 1/ammo.fireRate;
            yield return new WaitForSeconds(fireRate - (startTime - _lastShotTime));
            while (Time.time - startTime < time)
            {
                if (!canShoot(gunCheckCanShoot))
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                _lastShotTime = Time.time;
                Shoot(cameraTransform);
                PlayBulletSoundRpc();
                yield return new WaitForSeconds(fireRate);
            }

            yield return new WaitForSeconds(0.1f);
        }
        
        private float _lastShotTime;

        
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayRecoilAnimationRpc()
        {
            if (_animator == null) return;
            _animator.Animator.Play("Recoil", _recoilLayer, 0f);
            _rb.AddTorque(Vector3.up * recoilBodyTorque.y, ForceMode.Impulse);
            _rb.AddForce(Vector3.back * recoilBodyTorque.z, ForceMode.Impulse);
        }

        /// <summary>
        /// AI related method
        /// </summary>
        /// <param name="forwardTransform"></param>
        /// <param name="time"></param>
        /// <param name="canShoot"></param>
        /// <returns></returns>
        public IEnumerator ShootHoldDuringTime(float time, Func<Transform, bool> canShoot)
        {
            var startTime = Time.time;

            float fireRate = 1 / ammo.fireRate;
            yield return new WaitForSeconds(fireRate - (startTime - _lastShotTime));

            while (Time.time - startTime < time)
            {
                if (!canShoot(gunCheckCanShoot))
                {
                    Debug.Log(transform.name + " can't shoot");
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                _lastShotTime = Time.time;
                Shoot(cameraTransform);
                PlayBulletSoundRpc();
                yield return new WaitForSeconds(fireRate);
            }

            yield return new WaitForSeconds(0.1f);
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
                    if (listenOrTriggersEvents)
                    {
                        EventManager.TriggerEvent("OnShoot:" + weaponType, ammo);
                    }

                    if (HasAuthority && IsSpawned)
                        PlayBulletSoundRpc();
                    PlayRecoilAnimationRpc();
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
            if (!HasAuthority || !IsSpawned)
                return;
            var bulletDirection = origin.forward;
            if (Physics.Raycast(origin.position, origin.forward, out var hit, maxRaycastDistance, fireBulletLayerMask))
            {
                //Debug.Log("hit " + hit.transform.name);
                var direction = (hit.point - gunTransform.position).normalized;
                if (Vector3.Angle(bulletDirection, direction) < 45)
                    bulletDirection = direction;
                Debug.DrawRay(gunTransform.position, bulletDirection * 100, Color.red, 1f);
            }
            else
            {
                Debug.DrawRay(gunTransform.position, bulletDirection * 100, Color.green, 1f);
            }

            if (muzzleFlash)
            {
                PlayMuzzleFlashRpc();
            }

            var bullet = PoolManager.Instance.Instantiate(ammo.prefab, gunTransform.position, Quaternion.identity);
            // colliders
            var bulletCollider = bullet.GetComponentInChildren<Collider>();
            if (_gunTransformCollider != null)
                Physics.IgnoreCollision(bulletCollider, _gunTransformCollider, true);
            if (myParentColliderToIgnore != null)
                Physics.IgnoreCollision(bulletCollider, myParentColliderToIgnore, true);
            
            var bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.Init(ammo, faction);
            bulletScript.InitLifeTime(ammo.maxLifetime);

            var bulletRb = bullet.GetComponent<Rigidbody>();
            bulletRb.angularVelocity = Vector3.zero;
            bulletRb.linearVelocity = Vector3.zero;
            bulletRb.AddForce(bulletDirection * ammo.forcePower, ForceMode.Impulse);
            var rot = bulletRb.rotation.eulerAngles;
            bulletRb.rotation = Quaternion.Euler(rot.x, gunTransform.eulerAngles.y, rot.z);
            
            // network spawning related
            var bulletNetworkObject = bullet.GetComponent<NetworkObject>();
            if (NetworkManager.Singleton.IsConnectedClient && !bulletNetworkObject.IsSpawned)
                bulletNetworkObject.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            
            onFire?.Invoke();
            canFire = false;
            if (ammo.reloadSound != null)
                Invoke(nameof(PlayReloadSound), 0.3f);
            Invoke(nameof(ResetOnFire), 1 / ammo.fireRate);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayMuzzleFlashRpc()
        {
            Debug.Log("PlayMuzzleFlashRPC");
            if (_muzzleFlashCoroutine != null)
                StopCoroutine(_muzzleFlashCoroutine);
            _muzzleFlashCoroutine = StartCoroutine(MuzzleFlash());
        }

        private void PlayReloadSound()
        {
            reloadAudioSource.PlayOneShot(ammo.reloadSound);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlayBulletSoundRpc(bool oneShot = true)
        {
            Debug.Log($"{name}: {ammo.GetRandomFireSound().name}");
            if (oneShot)
                gunAudioSource.PlayOneShot(ammo.GetRandomFireSound());
            else
            {
                gunAudioSource.clip = ammo.GetRandomFireSound();
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
        
        public void CanShoot(bool canShoot)
        {
            listenOrTriggersEvents = canShoot;
            UnRegisterPlayerEvents();
            if (listenOrTriggersEvents)
                RegisterPlayerEvents();
        }

        public void SetCamera(CinemachineVirtualCamera cam)
        {
            cameraTransform = cam.transform;
        }
    }
}