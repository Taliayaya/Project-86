using System;
using System.Collections.Generic;
using Gameplay.Units;
using UnityEngine;

namespace SoundManagement
{
	public class DinosauriaTurretTracker : MonoBehaviour
	{
		[SerializeField] private Unit DinosauriaUnit;
		[SerializeField] private CannonController Controller;
		[SerializeField] private bool CheckOnlyMainTurret = true;
		
		[SerializeField] private float minAngleToShoot = 15f;

		private List<CannonController.TurretData> _turretsForCheck = new();

		private bool _dead = false;
		private bool _alreadyAimed = false;
		
		private void Start()
		{
			foreach (CannonController.TurretData turret in Controller.turrets)
			{
				if (CheckOnlyMainTurret)
				{
					if (!turret.IsMainTurret) continue;
				}

				_turretsForCheck.Add(turret);
			}

			DinosauriaUnit.onUnitDeath.AddListener(Dead);
		}

		private void Dead(Unit unit)
		{
			try
			{
				_dead = true;
				enabled = false;
				EventManager.TriggerEvent(SoundEventName.OnDinosauriaDead);
			}
			catch (Exception e)
			{
				Debug.LogError($"Something went wrong on handling dinosauria death. Error: {e.Message}");
				Debug.LogError(e);
			}
		}


		private void Update()
		{
			if (_dead) return;
			if (_alreadyAimed) return;
			
			foreach (CannonController.TurretData turretData in _turretsForCheck)
			{
				if (turretData.target == null) return;

				if (EnemyInRange(turretData.turret, turretData.target))
				{
					_alreadyAimed = true;

					Debug.LogError($"Aimed at target");
					EventManager.TriggerEvent(SoundEventName.OnDinosauriaAimed);
				}
			}
		}
		
		
		public bool EnemyInRange(Transform turret, Transform target)
		{
			if (target == null)
				return false;
			var direction = target.transform.position - turret.position;
			var angle = Vector3.Angle(direction, turret.forward);

			Debug.LogError($"Can aim at target: {angle < minAngleToShoot * 0.5f}");
			return angle < minAngleToShoot * 0.5f;
		}
	}
}