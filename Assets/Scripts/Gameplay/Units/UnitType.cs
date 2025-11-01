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
        Dinosauria = 16,
        Grauwolf = 32,
        Skorpion = 64,
        
        Legion = Ameise | Lowe | Dinosauria | Grauwolf | Skorpion,
        Republic = Juggernaut | Scavenger,
    }
}