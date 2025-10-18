using System;
using Armament.MainMenu;
using UnityEngine;
namespace Armament.Shared
{
	public class ArmamentSwitcher : MonoBehaviour
	{
		[SerializeField] private ArmamentSwitcherData[] Armaments;
		[Tooltip("If empty, it uses the config armament")]
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