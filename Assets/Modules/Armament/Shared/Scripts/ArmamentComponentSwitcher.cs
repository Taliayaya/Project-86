using System;
using Armament.MainMenu;
using UnityEngine;
using UnityEngine.Serialization;
namespace Armament.Shared
{
	public class ArmamentComponentSwitcher : MonoBehaviour
	{
		[SerializeField] private ArmamentComponentSwitcherData[] Armaments;
		
		[SerializeField] private bool changeArmamentOnStart = true;
		[SerializeField] private bool useArmamentOverride;
		[SerializeField] private ArmamentType armamentOverride;

		private void Start()
		{
			if (changeArmamentOnStart)
				ChangedArmament();
			SubscribeToEvents();
		}

		private void SubscribeToEvents()
		{
			if (MenuEvents.Instance != null)
				MenuEvents.Instance.OnChangedArmament += ChangedArmament;

			EventManager.AddListener(nameof(MenuEvents.Instance.OnChangedArmament), ChangedArmament);
		}

		private void OnDestroy()
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
			foreach (ArmamentComponentSwitcherData data in Armaments)
			{
				bool isCurrentEnabled = data.Type == armament;
				foreach (var visual in data.Components)
				{
					visual.enabled = isCurrentEnabled;
				}
			}
		}
	}

	[Serializable]
	public class ArmamentComponentSwitcherData : OneTypeBase
	{
		public override string TypeName => nameof(Type);
		public override string ValueName => nameof(Components);
		public ArmamentType Type;
		public Behaviour[] Components;
	}
}