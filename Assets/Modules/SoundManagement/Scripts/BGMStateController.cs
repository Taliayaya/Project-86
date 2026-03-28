using UnityEngine;

namespace SoundManagement
{
	public class BGMStateController : MonoBehaviour
	{
		[SerializeField] private bool IsInMenu = false;

		private void Start()
		{
			BGMPlayer.Instance.Intensity = CombatBGMState.Default;

			bool isInCombat = false;
			if (!IsInMenu)
			{
				isInCombat = QuestTracker.Instance.IsCombatQuest();
			}

			BGMPlayer.Instance.CheckInMenu(IsInMenu, isInCombat);
		}
	}
}