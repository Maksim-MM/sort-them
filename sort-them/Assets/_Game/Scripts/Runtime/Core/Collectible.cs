using UnityEngine;

namespace SortThem
{
    public class Collectible : MonoBehaviour
    {
        public int Index;
        [System.NonSerialized] public Rigidbody Body;
        [System.NonSerialized] public float CalmSince = -1f;
    }
}
