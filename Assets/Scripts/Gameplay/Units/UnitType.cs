using System;

namespace Gameplay.Units
{
    [Flags] // Flags means you should always multiply the previous value by 2
    public enum UnitType
    {
        None = 128,
        Ameise = 1,
        Lowe = 2,
        Juggernaut = 4,
        Scavenger = 8,
        Dinosauria = 16,
        Grauwolf = 32,
        Skorpion = 64,
        Morpho = 128,
        
        Legion = Ameise | Lowe | Dinosauria | Grauwolf | Skorpion | Morpho,
        Republic = Juggernaut | Scavenger,
    }
}