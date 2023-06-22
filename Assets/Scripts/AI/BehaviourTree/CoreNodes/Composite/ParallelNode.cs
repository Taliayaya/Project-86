namespace AI.BehaviourTree.CoreNodes.Composite
{
    public class ParallelNode : CompositeNode
    {
        private int _successCount;
        private int _failureCount;
        private int _current;
        protected override void OnStart()
        {
            _successCount = 0;
            _failureCount = 0;
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate()
        {
            foreach (var child in children)
            {
                switch (child.Update())
                {
                    case State.Running:
                        return State.Running;
                    case State.Failure:
                        _failureCount++;
                        break;
                    case State.Success:
                        _successCount++;
                        break;
                }
            }

            if (_failureCount > 0)
                return State.Failure;
            if (_successCount == children.Count)
                return State.Success;
            return State.Running;
        }
    }
}