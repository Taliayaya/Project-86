using System;
using Armament.MainMenu;
using BladesCombat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Armament.Shared
{
	public class ArmamentSwitcher : NetworkBehaviour
	{
		[SerializeField] private ArmamentSwitcherData[] Armaments;
		[Tooltip("If empty, it uses the config armament")]
		[SerializeField] private bool changeArmamentOnStart = true;
		[SerializeField] private bool useArmamentOverride;
		[SerializeField] private ArmamentType armamentOverride;

		[SerializeField] private BladeArmamentManager bladeArmamentManager;

		NetworkVariable<ArmamentType> m_currentArmament = new NetworkVariable<ArmamentType>();
		public ArmamentType CurrentArmament => m_currentArmament.Value;

		private void Start()
		{
			if (changeArmamentOnStart)
				ChangedArmament();
			SubscribeToEvents();
		}

		public override void OnNetworkSpawn()
		{
			m_currentArmament.OnValueChanged += ValueChanged;
		}

		protected override void OnNetworkPostSpawn()
		{
			base.OnNetworkPostSpawn();
			ChangedArmament(m_currentArmament.Value);
		}

		private void ValueChanged(ArmamentType previousValue, ArmamentType newValue)
		{
			ChangedArmament(newValue);
		}

		private void SubscribeToEvents()
		{
			if (MenuEvents.Instance != null)
				MenuEvents.Instance.OnChangedArmament += ChangedArmament;

			EventManager.AddListener(nameof(MenuEvents.Instance.OnChangedArmament), ChangedArmament);
		}

		public override void OnDestroy()
		{
			if (MenuEvents.Instance != null)
				MenuEvents.Instance.OnChangedArmament -= ChangedArmament;

			EventManager.RemoveListener(nameof(MenuEvents.Instance.OnChangedArmament), ChangedArmament);
		}

		public void ChangedArmament()
		{
			ArmamentType currentArmament =
				useArmamentOverride ? armamentOverride : ArmamentConfigManager.GetConfig().CurrentArmament;
			ChangedArmament(currentArmament);
		}
		public void ChangedArmament(ArmamentType armament)
		{
			if (IsOwner)
				m_currentArmament.Value = armament;
			foreach (ArmamentSwitcherData data in Armaments)
			{
				bool isCurrentEnabled = data.Type == armament;
				foreach (var visual in data.Visuals)
				{
					visual.SetActive(isCurrentEnabled);
				}
			}
		}
	}

	[Serializable]
	public class ArmamentSwitcherData : OneTypeBase
	{
		public override string TypeName => nameof(Type);
		public override string ValueName => nameof(Visuals);
		public ArmamentType Type;
		public GameObject[] Visuals;
	}
}