using System;
using UnityEngine;
using UnityEngine.Localization;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Special Car")]
    public class SpecialCarData : ScriptableObject
    {
        [Serializable]
        public struct Part
        {
            public GameObject Prefab;
            public Vector3 LocalPosition;
            public Vector3 LocalEuler;
        }

        [Serializable]
        public class Step
        {
            public string DevName;
            public LocalizedString DisplayName;
            public Part[] Add = Array.Empty<Part>();
            public GameObject[] Remove = Array.Empty<GameObject>();
        }

        public CarItemData Car;
        public Part[] BaseParts = Array.Empty<Part>();
        public Step[] Steps = Array.Empty<Step>();
        public Vector3 AssemblyOffset;
        public float AssemblyScale = 0.075f;

        public int MaxLevel => Steps != null ? Steps.Length : 0;
        public string CarID => Car != null ? Car.CarID : null;
    }
}
