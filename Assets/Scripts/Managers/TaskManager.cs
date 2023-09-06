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
                Instance.StartCoroutine(CallWrapper());
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

        public static TaskState CreateTask(IEnumerator coroutine)
        {
            if (!Instance)
                    Debug.Log("Creating instance...");
            return new TaskState(coroutine);
        }
    }
}