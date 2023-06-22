namespace AI.BehaviourTree.CoreNodes.Composite
{
    public class FallbackNode : CompositeNode
    {
        private int _current;
        protected override void OnStart()
        {
            _current = 0;
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            var child = children[_current];
            switch (child.Update())
            {
                case State.Running:
                    return State.Running;
                case State.Failure:
                    _current++;
                    break;
                case State.Success:
                    return State.Success;
            }
            return (_current >= children.Count) ? State.Failure : State.Running;
        
        }
    }
}
