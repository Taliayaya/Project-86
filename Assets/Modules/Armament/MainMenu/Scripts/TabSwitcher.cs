using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace Armament.MainMenu
{
	public class TabSwitcher : MonoBehaviour
	{
		[SerializeField] private GameObject[] RelatedObjects;
		[SerializeField] private Image Visual;
		[SerializeField] private Color EnabledColor;
		[SerializeField] private Color DisabledColor;
		[SerializeField] private bool IsEnabledByDefault;

		[SerializeField] private TabType TabType;
		
		private bool _isEnabled = false;

		public TabType Type => TabType;
		
		private void Start()
		{
			_isEnabled = IsEnabledByDefault;
			UpdateVisuals(true);
			MenuEvents.Instance.OnTabChanged += UpdateState;
		}
		private void UpdateState(TabSwitcher tab)
		{
			_isEnabled = tab == this;
			UpdateVisuals(false);
		}

		private void UpdateVisuals(bool immediateColor)
		{
			var targetColor = _isEnabled ? EnabledColor : DisabledColor;
			if (immediateColor)
			{
				Visual.color = targetColor;
			}
			else
			{
				Visual.DOColor(targetColor, 0.2f);
			}
			foreach (var relatedObject in RelatedObjects)
			{
				relatedObject.SetActive(_isEnabled);
			}
		}

		public void Click()
		{
			MenuEvents.Instance.FireOnTabChanged(this);
		}

	}
}