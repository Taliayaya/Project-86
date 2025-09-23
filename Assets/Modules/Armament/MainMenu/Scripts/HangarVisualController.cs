using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
namespace Armament.MainMenu
{
	public class HangarVisualController : MonoBehaviour
	{
		[SerializeField] private GameObject ToEnable;

		private IEnumerator Start()
		{
			yield return null;
			MenuEvents.Instance.OnOpenedHangar += ShowHangar;
			MenuEvents.Instance.OnClosedHangar += HideHangar;
		}
		private void ShowHangar()
		{
			ToEnable.SetActive(true);
		}

		private void HideHangar()
		{
			ToEnable.SetActive(false);
		}
	}
}