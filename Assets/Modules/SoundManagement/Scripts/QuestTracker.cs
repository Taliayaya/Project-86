using System.Linq;
using Gameplay.Quests;
using Gameplay.Quests.Tasks;
using Gameplay.Quests.Tasks.TasksType;
using UnityEngine;

namespace SoundManagement
{
	public class QuestTracker : MonoBehaviour
	{
		public static QuestTracker Instance;
		
		private Quest _currentQuest;

		private void Awake()
		{
			Instance = this;
		}

		private void OnEnable()
		{
			EventManager.AddListener(SoundEventName.QuestChanged, OnQuestChanged);
		}

		private void OnDisable()
		{
			EventManager.RemoveListener(SoundEventName.QuestChanged, OnQuestChanged);
		}

		private void OnQuestChanged(object questArg)
		{
			if (_currentQuest != null)
			{
				UnsubscribeToTasks();
			}
			
			_currentQuest = (Quest)questArg;
			if (IsCombatQuest())
			{
				SubscribeToTasks();
			}
			
			UpdateBGMByQuest();
		}

		private void SubscribeToTasks()
		{
			foreach (Task task in _currentQuest.Tasks)
			{
				if (task is KillTask killTask)
				{
					killTask.OnTaskProgressChanged += TaskProgressChanged;
				}
			}
		}

		private void UnsubscribeToTasks()
		{
			foreach (Task task in _currentQuest.Tasks)
			{
				if (task is KillTask killTask)
				{
					killTask.OnTaskProgressChanged -= TaskProgressChanged;
				}
			}
		}

		private void TaskProgressChanged(Task task)
		{
			if (task is KillTask killTask && killTask.IsCompleted)
			{
				killTask.OnTaskProgressChanged -= TaskProgressChanged;
			}
			
			UpdateBGMByQuest();
		}

		private void UpdateBGMByQuest()
		{
			if (IsCombatQuest())
			{
				BGMPlayer.Instance.PlayCombat();
			}
			else
			{
				BGMPlayer.Instance.PlayExploration();
			}
		}

		public bool IsCombatQuest()
		{
			if (_currentQuest == null) return false;

			return _currentQuest.Tasks.Any(t => t is KillTask && !t.IsCompleted);
		}
	}
}