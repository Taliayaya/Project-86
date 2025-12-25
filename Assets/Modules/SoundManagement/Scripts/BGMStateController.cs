using UnityEngine;

namespace SoundManagement
{
	public class BGMStateController : MonoBehaviour
	{
		[SerializeField] private bool IsInMenu = false;

		private void Start()
		{
			BGMPlayer.Instance.RemovePauses();
			BGMPlayer.Instance.SetCombatState(CombatBGMState.Normal);

			bool isInCombat = false;
			if (!IsInMenu)
			{
				isInCombat = QuestTracker.Instance.IsCombatQuest();
			}

			BGMPlayer.Instance.CheckInMenu(IsInMenu, isInCombat);
		}
	}
}