using System;
using UnityEngine;
namespace Armament.MainMenu
{
	public class MenuEvents : MonoBehaviour
	{
		public static MenuEvents Instance;

		public event Action<TabSwitcher> OnTabChanged;
		public event Action OnChangedArmament;
		public event Action OnOpenedHangar;
		public event Action OnClosedHangar;
		
		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			Instance = null;
		}

		public void FireOnTabChanged(TabSwitcher tab)
		{
			OnTabChanged?.Invoke(tab);
		}

		public void FireOnChangedArmament()
		{
			OnChangedArmament?.Invoke();
		}

		public void FireOnOpenedHangar()
		{
			OnOpenedHangar?.Invoke();
		}

		public void FireOnClosedHangar()
		{
			OnClosedHangar?.Invoke();
		}
	}
}