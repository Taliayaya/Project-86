namespace AI.BehaviourTree.CoreNodes.Decorator
{
    public class InvertNode : DecoratorNode
    {
        protected override void OnStart()
        {
            
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate()
        {
            var childState = child.Update();
            return childState switch
            {
                State.Running => State.Running,
                State.Failure => State.Success,
                State.Success => State.Failure,
                _ => State.Failure
            };
        }
    }
}