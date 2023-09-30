using System;

namespace Gameplay.Units
{
    [Flags] // Flags means you should always multiply the previous value by 2
    public enum UnitType
    {
        None = 0,
        Ameise = 1,
        Lowe = 2,
        Juggernaut = 4,
        Scavenger = 8,
        
        
        Legion = Ameise | Lowe,
        Republic = Juggernaut | Scavenger,
    }
}