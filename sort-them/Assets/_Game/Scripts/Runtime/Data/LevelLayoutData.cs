using System;
using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Level Layout")]
    public class LevelLayoutData : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int CarIndex;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        [Serializable]
        public struct PoseEntry
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public CarCatalog Catalog;
        public Entry[] Instances = Array.Empty<Entry>();
        public GameObject CollectiblePrefab;
        public PoseEntry[] Collectibles = Array.Empty<PoseEntry>();
    }
}
