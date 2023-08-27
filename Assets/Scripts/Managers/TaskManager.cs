using System.Collections;
using UnityEngine;

namespace Managers
{
	internal class TaskManager : Singleton<TaskManager>
	{
		public class TaskState
		{
			public bool Running => running;

			public bool Paused => paused;

			public delegate void FinishedHandler(bool manual);
			public event FinishedHandler Finished;

			internal IEnumerator coroutine;
			private bool running;
			private bool paused;
			private bool stopped;
		
			public TaskState(IEnumerator c)
			{
				coroutine = c;
			}
		
			public void Pause()
			{
				paused = true;
			}
		
			public void Unpause()
			{
				paused = false;
			}
		
			public void Start()
			{
				running = true;
				_singleton.StartCoroutine(CallWrapper());
			}
		
			public void Stop()
			{
				stopped = true;
				running = false;
			}

			private IEnumerator CallWrapper()
			{
				yield return null;
				var e = coroutine;
				while(running) {
					if(paused)
						yield return null;
					else {
						if(e != null && e.MoveNext()) {
							yield return e.Current;
						}
						else {
							running = false;
						}
					}
				}
			
				var handler = Finished;
				handler?.Invoke(stopped);
			}
		}

		private static TaskManager _singleton;

		private void Awake()
		{
			_singleton = this;
		}

		public static TaskState CreateTask(IEnumerator coroutine)
		{
			if (!_singleton)
			{
				var taskManager = FindObjectOfType<TaskManager>();
				if (taskManager is not null)
				{
					_singleton = taskManager;
				}
				if(_singleton == null) {
					var go = new GameObject("TaskManager");
					_singleton = go.AddComponent<TaskManager>();
				}
			}
			return new TaskState(coroutine);
		}
	}
}