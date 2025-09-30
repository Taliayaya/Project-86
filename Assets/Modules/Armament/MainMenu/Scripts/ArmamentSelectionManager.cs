using System;
using System.Linq;
using Armament.Shared;
using UI.MainMenu;
using UnityEngine;
namespace Armament.MainMenu
{
	public class ArmamentSelectionManager : MonoBehaviour
	{
		public static ArmamentSelectionManager Instance;

		[SerializeField] private Transform Container;
		[SerializeField] private ArmamentCell CellPrefab;
		
		
		private ArmamentConfig Config => ArmamentConfigManager.GetConfig();

		private bool _inited = false;
		
		private void Awake()
		{
			Instance = this;
			EventManager.AddListener(nameof(PersonalMarksMenu) + nameof(PersonalMarksMenu.Open), OnOpened);
			EventManager.AddListener(nameof(PersonalMarksMenu) + nameof(PersonalMarksMenu.Close), OnClosed);
		}
		
		private void OnDestroy()
		{
			EventManager.RemoveListener(nameof(PersonalMarksMenu) + nameof(PersonalMarksMenu.Open), OnOpened);
			EventManager.RemoveListener(nameof(PersonalMarksMenu) + nameof(PersonalMarksMenu.Close), OnClosed);
			Instance = null;
		}

		private void Start()
		{
			OnOpened();
		}

		private void OnOpened()
		{
			if (!_inited)
			{
				InitCells();
			}
			MenuEvents.Instance.FireOnOpenedHangar();
		}
		
		private void InitCells()
		{
			_inited = true;	
			var armaments = Resources.LoadAll<ArmamentSO>("ScriptableObjects/Armament/");

			if (armaments is { Length: > 0 })
			{
				var ordered = armaments.OrderBy(a => a.ForceIndex);
				foreach (ArmamentSO armamentSo in ordered)
				{
					CreateCell(armamentSo);
				}
			}
		}
		
		private void CreateCell(ArmamentSO armament)
		{
			Instantiate(CellPrefab, Container).Init(armament);
		}
		
		public void OnClosed()
		{
			MenuEvents.Instance.FireOnClosedHangar();
		}
		
	}
}