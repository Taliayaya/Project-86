using Unity.Behavior;

namespace AI.Behaviour.Types
{
    [BlackboardEnum]
    public enum SquadState
    {
        Idle,
        Patrol,
        MoveToPoint,
        AttackTarget,
        Retreat,
        SpreadOut,
        Regroup
    }
}