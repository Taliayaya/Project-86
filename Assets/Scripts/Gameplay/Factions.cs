using System.Collections.Generic;
using Gameplay.Units;
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
        private static readonly Dictionary<Faction, List<Unit>> _factionMembers = new Dictionary<Faction, List<Unit>>();
        
        public static void AddMember(Faction faction, Unit member)
        {
            if (!_factionMembers.ContainsKey(faction))
            {
                _factionMembers[faction] = new List<Unit>();
            }
            _factionMembers[faction].Add(member);
        }
        
        public static void RemoveMember(Faction faction, Unit member)
        {
            if (!_factionMembers.ContainsKey(faction))
            {
                return;
            }
            _factionMembers[faction].Remove(member);
        }
        
        public static List<Unit> GetMembers(Faction faction)
        {
            if (!_factionMembers.ContainsKey(faction))
            {
                return new List<Unit>();
            }
            return _factionMembers[faction];
        }

    }
}