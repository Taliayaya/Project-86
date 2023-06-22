using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public enum Faction
    {
        Republic,
        Legion,
    }
    public static class Factions
    {
        private static readonly Dictionary<Faction, List<GameObject>> _factionMembers = new Dictionary<Faction, List<GameObject>>();
        
        public static void AddMember(Faction faction, GameObject member)
        {
            if (!_factionMembers.ContainsKey(faction))
            {
                _factionMembers[faction] = new List<GameObject>();
            }
            _factionMembers[faction].Add(member);
        }
        
        public static void RemoveMember(Faction faction, GameObject member)
        {
            if (!_factionMembers.ContainsKey(faction))
            {
                return;
            }
            _factionMembers[faction].Remove(member);
        }
        
        public static List<GameObject> GetMembers(Faction faction)
        {
            if (!_factionMembers.ContainsKey(faction))
            {
                return new List<GameObject>();
            }
            return _factionMembers[faction];
        }

    }
}