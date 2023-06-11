using UnityEngine;

namespace AI.BehaviourTree
{
    public abstract class Node : ScriptableObject
    {
        public enum State
        {
            Running,
            Failure,
            Success
        }
        
        [HideInInspector] public State state = State.Running;
        [HideInInspector] public bool started = false;
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;
        [HideInInspector] public BlackBoard blackBoard;
        
        [TextArea] public string description;

        public State Update()
        {
            if (!started)
            {
                OnStart();
                started = true;
            }

            state = OnUpdate();
            
            if (state is State.Failure or State.Success)
            {
                OnStop();
                started = false;
            }

            return state;
        }

        public virtual Node Clone() => Instantiate(this);

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract State OnUpdate();
    }
}