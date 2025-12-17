using Unity.Behavior;

namespace AI.Behaviour.Types
{
    [BlackboardEnum]
    public enum SquadFormation
    {
        Line,
        Column,
        Wedge,
        Circle,
        PriorityWedge,
    }
}