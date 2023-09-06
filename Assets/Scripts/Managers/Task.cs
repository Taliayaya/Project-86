using System.Collections;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// A Task object represents a coroutine.  Tasks can be started, paused, and stopped.
    /// It is an error to attempt to start a task that has been stopped or which has
    /// naturally terminated.
    /// </summary>
    public class Task
    {
        /// <summary>
        /// Returns true if and only if the coroutine is running. Paused tasks
        /// are considered to be active.
        /// </summary>
        public bool Active => task.Running;
        
        /// <summary>
        /// Returns true if and only if the coroutine is running. Paused tasks
        /// are not considered to be running.
        /// </summary>
        public bool Running => task.Running && !task.Paused;

        /// <summary>
        /// Returns true if and only if the coroutine is currently paused.
        /// </summary>
        public bool Paused => task.Paused;

        /// <summary>
        /// Delegate for termination subscribers.  manual is true if and only if
        /// the coroutine was stopped with an explicit call to Stop().
        /// </summary>
        public delegate void FinishedHandler(bool manual);
    
        /// <summary>
        /// Termination event.  Triggered when the coroutine completes execution.
        /// </summary>
        public event FinishedHandler Finished;

        /// <summary>
        /// Creates a new Task object for the given coroutine.
        ///
        /// If autoStart is true (default) the task is automatically started
        /// upon construction.
        /// </summary>
        public Task(IEnumerator c, bool autoStart = true)
        {
            Debug.Log("TASKS: Creating new task");
            task = TaskManager.CreateTask(c);
            task.Finished += TaskFinished;
            if (autoStart)
            {
                Start();
            }
        }
    
        /// <summary>
        /// Begins execution of the coroutine
        /// </summary>
        public void Start()
        {
            //Debug.Log($"TASKS: Starting task {this.task.coroutine}");
            task.Start();
        }

        /// <summary>
        /// Discontinues execution of the coroutine at its next yield.
        /// </summary>
        public void Stop()
        {
            //Debug.Log($"TASKS: Stopping task {this.task.coroutine}");
            task.Stop();
        }
    
        /// <summary>
        /// Pauses execution of the coroutine at its next yield.
        /// </summary>
        public void Pause()
        {
            //Debug.Log($"TASKS: Pausing task {this.task.coroutine}");
            task.Pause();
        }
    
        /// <summary>
        /// Resumes execution of the coroutine.
        /// </summary>
        public void Unpause()
        {
            //Debug.Log($"TASKS: Resuming task {this.task.coroutine}");
            task.Unpause();
        }

        private void TaskFinished(bool manual)
        {
            FinishedHandler handler = Finished;
            handler?.Invoke(manual);
        }

        private TaskManager.TaskState task;
    }
}
