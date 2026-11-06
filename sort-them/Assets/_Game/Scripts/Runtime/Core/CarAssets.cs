using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
        static bool _formatLogged;

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
                    LogFormats(h.Result);
                    return;
                }
                Debug.LogWarning("SortThem: car prefab failed to load, retrying: " + car.DevName);
                Addressables.Release(h);
                EnsureRunner().StartCoroutine(RetryLater(car));
            };
        }

        static void LogFormats(GameObject prefab)
        {
            if (_formatLogged) return;
            _formatLogged = true;
            var mf = prefab.GetComponentInChildren<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            string meshInfo = mesh == null ? "no mesh" : mesh.name + " pos=" + mesh.GetVertexAttributeFormat(VertexAttribute.Position) + " nrm=" + mesh.GetVertexAttributeFormat(VertexAttribute.Normal) + " uv=" + mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0) + " stride=" + mesh.GetVertexBufferStride(0);
            Debug.Log("SortThem: gfx " + SystemInfo.graphicsDeviceType + " " + SystemInfo.graphicsDeviceName
                + " | supports Float16x4=" + SystemInfo.SupportsVertexAttributeFormat(VertexAttributeFormat.Float16, 4)
                + " SNorm8x4=" + SystemInfo.SupportsVertexAttributeFormat(VertexAttributeFormat.SNorm8, 4)
                + " UNorm16x2=" + SystemInfo.SupportsVertexAttributeFormat(VertexAttributeFormat.UNorm16, 2)
                + " Float16x2=" + SystemInfo.SupportsVertexAttributeFormat(VertexAttributeFormat.Float16, 2)
                + " | first car " + meshInfo);
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
