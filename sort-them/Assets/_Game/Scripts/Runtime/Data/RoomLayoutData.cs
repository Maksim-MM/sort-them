using System;
using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Room Layout")]
    public class RoomLayoutData : ScriptableObject
    {
        [Serializable]
        public struct RackEntry
        {
            public string CategoryId;
            public Vector3 Position;
            public float Yaw;
        }

        [Serializable]
        public struct RugEntry
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Size;
        }

        public RackEntry[] Racks = Array.Empty<RackEntry>();
        public RugEntry[] Rugs = Array.Empty<RugEntry>();

        public bool TryGetRack(string categoryId, out Vector3 position, out float yaw)
        {
            for (int i = 0; i < Racks.Length; i++)
            {
                if (Racks[i].CategoryId != categoryId) continue;
                position = Racks[i].Position;
                yaw = Racks[i].Yaw;
                return true;
            }
            position = Vector3.zero;
            yaw = 0f;
            return false;
        }
    }
}
