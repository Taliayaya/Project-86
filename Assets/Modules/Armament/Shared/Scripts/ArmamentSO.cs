using System;
using System.Collections.Generic;
using NaughtyAttributes;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
// ReSharper disable InconsistentNaming
namespace Armament.Shared
{
	[CreateAssetMenu(menuName = "Scriptable Objects/Armament/Armament Info")]
	public class ArmamentSO : ScriptableObject
	{
		[SerializeField] private int _forceIndex = -100;
		[SerializeField] private ArmamentType _type;

		[Header("Visuals")]
		[SerializeField] private Sprite _icon;
		[SerializeField] private string _name;


		[Header("Data")]
		[SerializeField] private ArmamentDataType DataType;

		[SerializeField, HideIf(nameof(_isDirect))] 
		private AmmoSO AmmoDetails;

		[SerializeField, ShowIf(nameof(_isDirect))]
		private int Damage;
		
		[SerializeField, ShowIf(nameof(_isDirect))]
		private int Range;
		
		[SerializeField, ShowIf(EConditionOperator.And, nameof(_isDirect), nameof(_isArmamentWithAmmo))]
		private int AttackSpeed;
		
		[SerializeField, ShowIf(EConditionOperator.And, nameof(_isDirect), nameof(_isArmamentWithAmmo))]
		private int Ammo;


		private bool _isDirect => DataType == ArmamentDataType.Direct;

		public int ForceIndex => _forceIndex;
		public ArmamentType ArmamentType => _type;
		public Sprite Icon => _icon;
		public string Name => _name;

		private bool _isArmamentWithAmmo => _type == ArmamentType.MachineGun;

		/// <summary>
		/// Each time array is created. Cache whenever possible
		/// </summary>
		public ArmamentDetail[] Details
		{
			get
			{
				int damage1 = 0;
				int damage2 = 0;
				int range = 0;
				int attackSpeed = 0;
				int ammo = 0;
				if (_isDirect)
				{
					damage1 = damage2 = Damage;
					range = Range;
					attackSpeed = AttackSpeed;
					ammo = Ammo;
				}
				else
				{
					damage1 = Mathf.RoundToInt(AmmoDetails.Damage(0));
					damage2 = Mathf.RoundToInt(AmmoDetails.Damage(100000));

					float mass = 1;
					if (AmmoDetails.prefab != null && AmmoDetails.prefab.TryGetComponent(out Rigidbody rigidbody))
					{
						mass = rigidbody.mass;
					}

					range = Mathf.RoundToInt((AmmoDetails.forcePower / mass) * AmmoDetails.maxLifetime);
					attackSpeed = Mathf.RoundToInt(AmmoDetails.fireRate);

					ammo = AmmoDetails.maxAmmo;
				}

				List<ArmamentDetail> details = ListPool<ArmamentDetail>.Get();

				details.Add(new ArmamentDetail("Damage", damage1, damage2));
				details.Add(new ArmamentDetail("Range", range));

				if (_type == ArmamentType.MachineGun)
				{
					details.Add(new ArmamentDetail("Fire Rate", attackSpeed));
					details.Add(new ArmamentDetail("Ammo", ammo));
				}
				else if (_type == ArmamentType.Blades)
				{

				}

				ArmamentDetail[] array = details.ToArray();
				ListPool<ArmamentDetail>.Release(details);
				return array;
			}
		}
	}

	public struct ArmamentDetail
	{
		public string Name;
		public int Value;
		public int Value2;

		public ArmamentDetail(string name, int value)
		{
			Name = name;
			Value = value;
			Value2 = value;
		}

		public ArmamentDetail(string name, int value, int value2)
		{
			Name = name;
			Value = value;
			Value2 = value2;
		}
	}
}