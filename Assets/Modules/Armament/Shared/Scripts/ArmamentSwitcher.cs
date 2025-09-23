using System;
using Armament.MainMenu;
using UnityEngine;
namespace Armament.Shared
{
	public class ArmamentSwitcher : MonoBehaviour
	{
		[SerializeField] private ArmamentSwitcherData[] Armaments;

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
			foreach (ArmamentSwitcherData data in Armaments)
			{
				bool isCurrentEnabled = data.Type == currentArmament;
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