using Unity.Netcode;
using UnityEngine;

namespace Gameplay
{
    public abstract class Module : NetworkBehaviour
    {
        public Faction faction;

    }
}