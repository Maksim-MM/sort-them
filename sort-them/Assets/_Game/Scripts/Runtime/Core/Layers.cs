using UnityEngine;

namespace SortThem
{
    public static class Layers
    {
        public static readonly int LooseItems = LayerMask.NameToLayer("LooseItems");
        public static readonly int StaticPlaced = LayerMask.NameToLayer("StaticPlaced");
        public static readonly int Player = LayerMask.NameToLayer("Player");
        public static readonly int InteractMask = ~(1 << LayerMask.NameToLayer("Player"));
    }
}
