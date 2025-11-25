using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI.Behaviour.Types;
using Gameplay;
using Gameplay.Units;
using NaughtyAttributes;
using Networking;
using ScriptableObjects.AI;
using Unity.Netcode;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Utility;

namespace AI
{
    [Serializable]
    public struct SquadLeaderInfo
    {
        public SquadState squadState;
        public int[] priorityList;
        public SquadFormation formation;
        public List<LegionSquad> members;
    }
    [RequireComponent(typeof(NetworkNavMeshAgent), typeof(AIAgent), typeof(BehaviorGraphAgent))]
    public class LegionSquad : NetworkBehaviour
    {
        [SerializeField] private bool isLeader;
        public Rigidbody rb;
        public LegionSquad leader;
        public SquadSO squadSO;

        public int Priority => squadSO.priority;
        public int Spacing => squadSO.spacing;
        public int MemberCount => isLeader ? squadInfo.members.Count : leader.squadInfo.members.Count;
        
        private bool _followLeader;
        public bool FollowLeader
        {
            get => _followLeader;
            set
            {
                _followLeader = value;
                Debug.Assert(_agent.SetVariableValue("FollowLeader", value));
            }
        }

        [ReadOnly] public int squadPosition;
        private Vector3 _squadPositionOffset;
        public SquadFormation Formation => isLeader ? squadInfo.formation : leader.squadInfo.formation;
        public Vector3 DesiredSquadPosition
        {
            get
            {
                if (isLeader)
                    return transform.position;
                var trans = leader.transform;
                
                return trans.position 
                       + trans.right * _squadPositionOffset.x
                       + trans.forward * _squadPositionOffset.z;
            }
        }

        public SquadLeaderInfo squadInfo;
        
        private BehaviorGraphAgent _agent;

        public bool IsLeader
        {
            get => isLeader;
            set
            {
                isLeader = value;
                FollowLeader = !value;
                _agent.BlackboardReference.SetVariableValue("isSquadLeader", value);
            }
        }

        private void Awake()
        {
            _agent = GetComponent<BehaviorGraphAgent>();
            agent = GetComponent<NetworkNavMeshAgent>();
            if (!rb)
                rb = GetComponent<Rigidbody>();
            squadInfo.priorityList = new int[squadSO.maxPriority + 1];
        }

        private void OnEnable()
        {
            if (IsLeader && squadInfo.members is null)
            {
                squadInfo.members = new List<LegionSquad>();
            }
        }

        private void Start()
        {
            StartCoroutine(SquadUpdate());
            StartCoroutine(FollowLeaderCoroutine());

            IsLeader = isLeader;
        }

        public void AddMember(LegionSquad member)
        {
            if (IsLeader && squadInfo.members.Count < squadSO.maxSquadSize)
            {
                squadInfo.members.Add(member);
                member.leader = this;
                member.squadPosition = squadInfo.members.Count;
                member.UpdatePosition();
            }
        }
        
        public void RemoveMember(LegionSquad member)
        {
            if (IsLeader)
                squadInfo.members.Remove(member);
        }
        
        public bool IsInSquad => (IsLeader && squadInfo.members.Count > 0) || leader;

        public NetworkNavMeshAgent agent;

        public void UpdateSquadPosition()
        {
            if (!IsLeader)
                return;
            if (agent.Agent.hasPath)
                SetDestination(agent.Agent.destination);
            else
                SetDestination(transform.position);
        }

        public void SetDestination(Vector3 position)
        {
            if (!IsLeader) return;
            int count = squadInfo.members.Count;

            Vector3 offset = SquadFormationHelper.GetFormationOffset(this);
            UpdatePosition(position + offset);
        }

        private IEnumerator SquadUpdate()
        {
            while (true)
            {
                var legions = Factions.GetMembers(Faction.Legion);
                foreach (var legion in legions)
                {
                    float distance = Vector3.Distance(transform.position, legion.transform.position);
                    if (distance > squadSO.squadMergeDistance)
                        continue;
                    
                    // squad are close to each other : merge them
                    var legionSquad = legion.GetComponent<LegionSquad>();
                    var newLeader = MergeSquad(legionSquad);
                    newLeader.UpdateSquadPosition();
                }
                yield return new WaitForSeconds(20);
            }
        }

        private IEnumerator FollowLeaderCoroutine()
        {
            while (true)
            {
                if (IsLeader || !FollowLeader)
                {
                    yield return new WaitForSeconds(10f);
                    continue;
                }
                
                UpdatePosition();
                yield return new WaitForSeconds(squadSO.positionUpdateFrequency);
            }
        }

        public void AssignPositionsAndLeader(LegionSquad newLeader)
        {
            if (!IsLeader) return;
            for (var i = 0; i < squadInfo.priorityList.Length; i++)
                squadInfo.priorityList[i] = 0;
            
            foreach (var member in squadInfo.members)
            {
                squadInfo.priorityList[member.Priority]++;
                member.squadPosition = squadInfo.priorityList[member.Priority];
                member.leader = newLeader;
                member.IsLeader = false;
                member.UpdatePosition();
            }
            UpdateSquadPosition();
        }

        private LegionSquad GetMemberWithHighestPriority()
        {
            LegionSquad highestPriorityMember = null;
            int highestPriority = -1;
            foreach (var member in squadInfo.members)
            {
                if (member.Priority > highestPriority)
                {
                    highestPriorityMember = member;
                    highestPriority = member.Priority;
                }
            }
            return highestPriorityMember;
        }

        private void LeaveSquad()
        {
            if (IsLeader)
            {
                if (squadInfo.members.Count == 0)
                    return;
                // pass leadership to the last member in the squad
                var newLeader = GetMemberWithHighestPriority();
                squadInfo.priorityList[newLeader.Priority]--;
                // TODO: get the highest priority to become the new leader
                squadInfo.members.Remove(newLeader);
                
                AssignPositionsAndLeader(newLeader);
                
                newLeader.IsLeader = true;
                newLeader.squadPosition = 0;
                (newLeader.squadInfo, squadInfo) = (squadInfo, newLeader.squadInfo);
                newLeader.UpdateSquadPosition();
            }
            else
            {
                leader.squadInfo.members.Remove(this);
                leader.AssignPositionsAndLeader(leader);
                leader.UpdateSquadPosition();
                squadPosition = 0;
                leader = null;
                IsLeader = true;
            }
        }

        /// Merges the current squad with the specified squad. The method combines squad
        /// members and leaders, ensuring the integrity of squad hierarchy and preventing
        /// invalid operations such as self-merging or merging with non-leader squads without
        /// valid references.
        /// <param name="squadToMerge">The squad to merge with the current squad.
        /// Must not be null, and must be a leader squad or have a valid leader reference.</param>
        /// <returns>The resultant squad leader after the merge operation, which is usually
        /// the current squad's leader unless the other squad's leader had a greater member count.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the squad to merge is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a non-leader squad attempts to merge
        /// without a valid leader reference.</exception>
        private LegionSquad MergeSquad(LegionSquad squadToMerge)
        {
            if (squadToMerge == null)
                throw new ArgumentNullException(nameof(squadToMerge));

            if (!IsLeader)
            {
                if (!leader)
                    throw new InvalidOperationException("Non-leader squad has no leader reference");
                Debug.Log($"[LegionSquad] {name} ask {leader.name} to merge squad with {squadToMerge.name}");
                return leader.MergeSquad(squadToMerge);
            }

            if (!squadToMerge.IsLeader)
            {
                if (!squadToMerge.leader)
                    throw new InvalidOperationException("Non-leader squad has no leader reference");
                Debug.Log($"[LegionSquad] {name} ask leader {squadToMerge.leader.name} to merge squad because of {squadToMerge.name}");
                return MergeSquad(squadToMerge.leader);
            }

            if (Priority < squadToMerge.Priority)
            {
                return squadToMerge.MergeSquad(this);
            }
            if (Priority == squadToMerge.Priority && squadToMerge.squadInfo.members.Count > squadInfo.members.Count)
            {
                return squadToMerge.MergeSquad(this);
            }
            
            // Prevent merging with self
            if (this == squadToMerge || ReferenceEquals(squadToMerge, this))
                return this;
                
            Debug.Log($"[LegionSquad] Merge {name}[{IsLeader}-{GetInstanceID()}] with {squadToMerge.name}[{squadToMerge.IsLeader}-{squadToMerge.GetInstanceID()}");
            // Add the squad leader as a member
            squadInfo.members.Add(squadToMerge);
            squadToMerge.leader = this;
            squadToMerge.IsLeader = false;
            // Add all its members
            squadInfo.members.AddRange(squadToMerge.squadInfo.members);
            squadToMerge.squadInfo.members.Clear();
            
            AssignPositionsAndLeader(this);
            // breakpoint Unity
            
            return this;
        }
        
        
        private void Update()
        {
            // UpdateSquadPosition();
        }

        private void UpdatePosition(Vector3 position)
        {
            agent.SetDestination(position);

            if (IsLeader)
                return;
            if (Vector3.Distance(transform.position, leader.transform.position) > squadSO.sprintDistance)
                agent.Sprint();
            else
                agent.Sprint(false);
        }

        public void UpdatePosition()
        {
            if (!IsLeader)
            {
                _squadPositionOffset = SquadFormationHelper.GetFormationOffset(this);
                Debug.Log($"[LegionSquad] UpdatePosition {name}[{squadPosition} + {_squadPositionOffset}");
                UpdatePosition(DesiredSquadPosition);
            }
        }
        
        public float DistanceFromLeader => !IsLeader ? Vector3.Distance(transform.position, leader.transform.position) : 0;

        public override void OnDestroy()
        {
            base.OnDestroy();
            LeaveSquad();
        }

        
        [Header("Debug")]
        public float leaderCrownHeight = 3f;
        private void OnDrawGizmos()
        {
            Handles.color = Color.grey;
            Handles.DrawWireArc(transform.position, Vector3.up, Vector3.forward, 360, (float)squadSO.squadMergeDistance / 2);

            if (IsLeader)
            {
                Vector3 top = transform.position + Vector3.up * leaderCrownHeight;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(top, 0.25f);
                Gizmos.DrawLine(transform.position + Vector3.up, top);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(DesiredSquadPosition, 0.5f);
            }
        }
    }
}