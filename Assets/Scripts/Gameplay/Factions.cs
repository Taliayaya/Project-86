using System;
using System.Collections.Generic;
using Gameplay.Units;
using UnityEngine;

namespace Gameplay
{
    public enum Faction
    {
        Republic,
        Legion,
        Neutral
    }

    struct FactionData
    {
        public List<Unit> Members;
        public bool IsPaused;
    }
    public static class Factions
    {
        private static FactionData[] _factionMembers = new FactionData[3];

        static Factions()
        {
            // pre allocate the array because otherwise the list ref
            // only become valid on the first unit spawn in the faction
            _factionMembers[(int)Faction.Republic] = new FactionData() { Members = new List<Unit>()};
            _factionMembers[(int)Faction.Neutral]  = new FactionData() { Members = new List<Unit>()};
            _factionMembers[(int)Faction.Legion]   = new FactionData() { Members = new List<Unit>()};
        }
        
        public static void AddMember(Faction faction, Unit member)
        {
            _factionMembers[(int)faction].Members.Add(member);
        }
        
        public static void RemoveMember(Faction faction, Unit member)
        {
            _factionMembers[(int)faction].Members.Remove(member);
        }
        
        public static List<Unit> GetMembers(Faction faction)
        {
            return _factionMembers[(int)faction].Members;
        }

        public static bool IsPaused(Faction faction)
        {
            return _factionMembers[(int)faction].IsPaused;
        }
        public static void Pause(Faction faction, bool isPaused = true)
        {
            _factionMembers[(int)faction].IsPaused = isPaused;
            EventManager.TriggerEvent(Constants.TypedEvents.FactionPause, faction);
        }
    }
}