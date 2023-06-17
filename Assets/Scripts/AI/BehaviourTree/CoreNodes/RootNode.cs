namespace AI.BehaviourTree.CoreNodes
{
    public class RootNode : Node
    {
        public Node child;
    
        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate() => child.Update();
        public override Node Clone()
        {
            RootNode node = Instantiate(this);
            node.child = child.Clone();
            return node;
        }
    }
}
