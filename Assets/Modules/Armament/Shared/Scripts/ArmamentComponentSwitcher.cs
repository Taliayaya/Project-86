using System;
using Armament.MainMenu;
using UnityEngine;
using UnityEngine.Serialization;
namespace Armament.Shared
{
	public class ArmamentComponentSwitcher : MonoBehaviour
	{
		[SerializeField] private ArmamentComponentSwitcherData[] Armaments;

		private void Start()
		{
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

		private void ChangedArmament()
		{
			ArmamentType currentArmament = ArmamentConfigManager.GetConfig().CurrentArmament;
			foreach (ArmamentComponentSwitcherData data in Armaments)
			{
				bool isCurrentEnabled = data.Type == currentArmament;
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