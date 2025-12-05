using Unity.Behavior;

namespace AI.Behaviour.Types
{
    [BlackboardEnum]
    public enum SquadState
    {
        Idle,
        Patrolling,
        Attacking
    }

    public enum SquadMemberState
    {
        Attached,
        Flanking
    }
}