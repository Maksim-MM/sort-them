using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SortThem
{
    public static class CarAssets
    {
        const float RetryDelay = 2f;

        static readonly Dictionary<CarItemData, GameObject> Prefabs = new Dictionary<CarItemData, GameObject>();
        static readonly HashSet<CarItemData> Requested = new HashSet<CarItemData>();
        static Runner _runner;

        public static int Total => Requested.Count;
        public static int Loaded => Prefabs.Count;
        public static bool IsReady => Requested.Count > 0 && Prefabs.Count == Requested.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Prefabs.Clear();
            Requested.Clear();
            _runner = null;
        }

        public static void Preload(CarCatalog catalog)
        {
            if (catalog == null) return;
            foreach (var car in catalog.Cars)
            {
                if (car == null || car.Prefab == null || !car.Prefab.RuntimeKeyIsValid() || !Requested.Add(car)) continue;
                Request(car);
            }
        }

        public static GameObject Prefab(CarItemData car)
        {
            if (car == null) return null;
            if (Prefabs.TryGetValue(car, out var prefab)) return prefab;
#if UNITY_EDITOR
            if (!Application.isPlaying && car.Prefab != null) return car.Prefab.editorAsset as GameObject;
#endif
            return null;
        }

        static void Request(CarItemData car)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(car.Prefab);
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
                {
                    Prefabs[car] = h.Result;
                    return;
                }
                Debug.LogWarning("SortThem: car prefab failed to load, retrying: " + car.DevName);
                Addressables.Release(h);
                EnsureRunner().StartCoroutine(RetryLater(car));
            };
        }

        static IEnumerator RetryLater(CarItemData car)
        {
            yield return new WaitForSecondsRealtime(RetryDelay);
            Request(car);
        }

        static Runner EnsureRunner()
        {
            if (_runner != null) return _runner;
            var go = new GameObject("CarAssets");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
            return _runner;
        }

        class Runner : MonoBehaviour { }
    }
}
