using AI.Behaviour.Types;
using UnityEngine;

namespace AI
{
    public static class SquadFormationHelper
    {
        public static Vector3 GetFormationOffset(LegionSquad unit)
        {
            float spacing = unit.squadSO.spacing;
            return unit.Formation switch
            {
                SquadFormation.Line => LineFormation(unit),
                SquadFormation.Column => ColumnFormation(spacing, unit.squadPosition, unit.Priority),
                SquadFormation.Wedge => WedgeFormation(unit),
                SquadFormation.Circle => CircleFormation(spacing, unit.squadPosition, unit.MemberCount, unit.Priority),
                _ => Vector3.zero
            };
        }
        
        private static Vector3 WedgeFormation(LegionSquad unit)
        {
            if (unit.IsLeader) return Vector3.zero;
            int wedgeNumber = unit.leader.Priority - unit.Priority;
            int index = unit.squadPosition - 1;
            
            int side = (index % 2 == 0) ? 1 : -1;
            int rank = index / 2 + 1;
            
            int x = side * rank * unit.Spacing ;
            int z = -rank * unit.Spacing;
            z += wedgeNumber * unit.leader.Spacing;
            return new Vector3(x, 0, z);
        }

        private static Vector3 ColumnFormation(float spacing, int index, int priority)
        {
            return new Vector3(0, 0, -(index) * spacing);
        }

        private static Vector3 LineFormation(LegionSquad unit)
        {
            if (unit.IsLeader)
                return Vector3.zero;
            int lineNumber = unit.leader.Priority - unit.Priority;
            int priorityCount = unit.leader.squadInfo.priorityList[unit.Priority];
            
            int z = lineNumber * unit.Spacing;
            int half = priorityCount / 2;
            // Left group = first half
            if (unit.squadPosition <= half)
            {
                // left side = negative X
                int leftIndex = half - unit.squadPosition + 1;
                return new Vector3(-leftIndex * unit.Spacing, 0, z);
            }
            else
            {
                // right side = positive X
                int rightIndex = unit.squadPosition - half;
                return new Vector3(+rightIndex * unit.Spacing, 0, z);
            }
        }

        private static Vector3 CircleFormation(float spacing, int index, int count, int priority)
        {
            // Leader stays at center
            if (index == 0)
                return Vector3.zero;

            // Re-index so the circle starts at follower #1
            int soldierIndex = index - 1;
            int circleCount = count;

            float radius = 2f * spacing;
            float angle = (360f / circleCount) * soldierIndex;

            return new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );
        }
    }
}