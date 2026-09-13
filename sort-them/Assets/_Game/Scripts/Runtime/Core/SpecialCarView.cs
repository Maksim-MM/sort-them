using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public class SpecialCarView : MonoBehaviour
    {
        public SpecialCarData Data;
        public int Level;

        Transform _root;

        public Transform Root => _root;

        public void Show(SpecialCarData data, int level)
        {
            Data = data;
            Level = level;
            Rebuild(null);
        }

        public void Rebuild(List<GameObject> newParts)
        {
            if (_root != null) Destroy(_root.gameObject);
            if (Data == null) return;
            _root = SpecialCarAssembly.Build(Data, Level, transform, gameObject.layer, newParts);
            var car = GetComponent<CarInstance>();
            if (car != null && car.Rend != null) car.Rend.enabled = false;
        }

        void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            var car = GetComponent<CarInstance>();
            if (car != null && car.Rend != null && !car.Hidden) car.Rend.enabled = true;
        }
    }
}
