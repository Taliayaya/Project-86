using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Gameplay.Mecha;
using UnityEngine;

namespace SoundManagement
{
	public class PlayerStateTracker : MonoBehaviour
	{
		public static PlayerStateTracker Instance;

		public bool Destroyed => _destroyed;

		public float HealthPercent => _healthPercent;

		private bool _destroyed = false;

		private float _startTime = 0;
		private float _healthPercent = 1;
		private float _ammoSum = 0;
		
		private const float DelayBeforeStartChecking = 3;

		private bool _inited = false;

		private List<WeaponModule> _weaponModules = new();

		private bool _hasDinosauriaAimed = false;
		
		private void Awake()
		{
			Instance = this;
		}

		private void Start()
		{
			EventManager.AddListener(Constants.TypedEvents.OnPlayerChanged, OnPlayerChanged);
			EventManager.AddListener(Constants.TypedEvents.OnTakeDamage, OnTakeDamage);
			EventManager.AddListener(SoundEventName.OnDinosauriaAimed, OnDinosauriaAimed);
			EventManager.AddListener(SoundEventName.OnDinosauriaDead, OnDinosauriaDead);
		}


		private void OnDestroy()
		{
			EventManager.RemoveListener(Constants.TypedEvents.OnPlayerChanged, OnPlayerChanged);
			EventManager.RemoveListener(SoundEventName.OnDinosauriaAimed, OnDinosauriaAimed);
			EventManager.RemoveListener(SoundEventName.OnDinosauriaDead, OnDinosauriaDead);
		}

		private void Update()
		{
			if (_destroyed) return;
			if (BGMPlayer.Instance.IsPaused) return;
			
			if (_startTime >= DelayBeforeStartChecking)
			{
				GetState();
				CheckState();
			}
			else
			{
				_startTime += Time.deltaTime;
			}
		}

		private void GetState()
		{
			if (PlayerManager.Player == null)
			{
				_destroyed = true;
			}
			else
			{
				_healthPercent = PlayerManager.Player.Health / PlayerManager.Player.MaxHealth;

				Init();
				_ammoSum = 0;
				foreach (WeaponModule module in _weaponModules)
				{
					_ammoSum += (float)module.CurrentAmmoRemaining / module.MaxAmmo;
				}

				int modulesWithAmmo = _weaponModules.Count(m => m.MaxAmmo > 0);
				_ammoSum /= modulesWithAmmo > 0 ? modulesWithAmmo : 1;
			}
		}

		private void Init()
		{
			if (!_inited)
			{
				_inited = true;
				IReadOnlyCollection<Module> modules = PlayerManager.Player.GetModules();
				foreach (Module module in modules)
				{
					if (module is WeaponModule weaponModule)
					{
						_weaponModules.Add(weaponModule);
					}
				}
			}
		}

		private void CheckState()
		{
			CombatBGMState combatState = GetCombatState();
			if (combatState != BGMPlayer.Instance.CombatState)
			{
				BGMPlayer.Instance.SetCombatState(combatState);
			}
		}

		private CombatBGMState GetCombatState()
		{
			if (_destroyed) return CombatBGMState.Death;


			bool isIntense = IsIntenseCombatState();

			if (isIntense)
			{
				return CombatBGMState.Intense;
			}

			bool isDinosauriaAiming = IsDinosauriaAimed();

			if (isDinosauriaAiming)
			{
				return CombatBGMState.Dinosauria;
			}

			return CombatBGMState.Default;
		}

		private bool IsDinosauriaAimed()
		{
			return _hasDinosauriaAimed;
		}

		private bool IsIntenseCombatState()
		{
			const float lowHpThreshold = 0.3f;
			const float lowAmmoThreshold = 0.3f;
			return _healthPercent <= lowHpThreshold || _ammoSum <= lowAmmoThreshold;
		}

		private void OnDinosauriaAimed()
		{
			_hasDinosauriaAimed = true;
		}

		private void OnDinosauriaDead()
		{
			_hasDinosauriaAimed = false;
		}
		
		private void OnPlayerChanged(object arg0)
		{
			_healthPercent = 1f;
		}
		
		private void OnTakeDamage(object arg0)
		{
			// if we take damage during exploration / death music playing
			// => action is continuing
			_destroyed = false;
			// it has a guard condition
			BGMPlayer.Instance.PlayCombat();
		}
	}
}