using Gameplay.Units;
using ScriptableObjects.AI;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BehaviourTree.BasicNodes
{
    public class LockEnemy : ActionNode
    {
        private bool _isSet;
        private bool _isDone;
        
        private float _lastExecutionTime;
        private float _remainingTime;
        
        [SerializeField] private float lockTime = 5f;
        
        private AIAgent _agent;
        private AgentSO _agentSO;
        private NavMeshAgent _navMeshAgent;
        private Transform _sensor;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _isSet = true;
                _agent = blackBoard.GetValue<AIAgent>("aiAgent");
                _agentSO = blackBoard.GetValue<AgentSO>("agentSO");
                _sensor = blackBoard.GetValue<Transform>("sensor");
                _navMeshAgent = blackBoard.GetValue<NavMeshAgent>("navMeshAgent");
            }
            _lastExecutionTime = Time.time;
            _remainingTime = lockTime;
            _navMeshAgent.isStopped = true;
            Debug.Log("Sensor: " + _sensor.name);
        }

        protected override void OnStop()
        {
            _navMeshAgent.isStopped = false;
        }

        protected override State OnUpdate()
        {
            UpdateVisibility();
            if (_agent.Target is null || _agent.Target.Visibility == TargetInfo.VisibilityStatus.NotVisible)
            {
                _isDone = true;
                return State.Failure;
            }
            
            _remainingTime -= Time.time - _lastExecutionTime;
            _lastExecutionTime = Time.time;
            
            _isDone = _remainingTime <= 0f;

            if (_isDone)
            {
                Debug.Log("Locked enemy");
                return State.Success;
            }

            return State.Running;
        }
        
        private void UpdateVisibility()
        {
            if (_agent.Target is null)
            {
                Debug.Log("Target is null");
                return;
            }
            Debug.Log("Updating visibility");

            var direction = _agent.Target.Position - _sensor.position;
            Debug.DrawRay(_sensor.transform.position, direction * 2, Color.red, 5);
            if (CanSeeObject.PerformRaycast(_sensor.transform, _agentSO, direction * 2,
                    _agent.Target.Unit.gameObject))
            {
                Debug.Log("Target is visible");
                _agent.Target.Visibility = TargetInfo.VisibilityStatus.Visible;
            }
            else
            {
                Debug.Log("Target is not visible");
                _agent.Target.Visibility = TargetInfo.VisibilityStatus.NotVisible;
            }
        }
    }
    
    
}