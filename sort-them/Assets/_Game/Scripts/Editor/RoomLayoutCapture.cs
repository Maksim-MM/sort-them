using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class RoomLayoutCapture
    {
        public const string Path = Paths.Data + "/RoomLayout.asset";

        [MenuItem("SortThem/4d. Capture Layout From Scene")]
        public static void Capture()
        {
            var racks = Object.FindObjectsByType<RackController>(FindObjectsSortMode.None);
            if (racks.Length == 0)
            {
                Debug.LogError("SortThem: no racks in scene, nothing to capture");
                return;
            }
            var asset = EditorAssets.LoadOrCreate<RoomLayoutData>(Path);

            var rackEntries = new List<RoomLayoutData.RackEntry>();
            foreach (var rack in racks)
            {
                if (rack.Category == null) continue;
                rackEntries.Add(new RoomLayoutData.RackEntry
                {
                    CategoryId = rack.Category.CategoryID,
                    Position = rack.transform.position,
                    Yaw = Mathf.Repeat(rack.transform.eulerAngles.y, 360f)
                });
            }
            rackEntries.Sort((a, b) => string.CompareOrdinal(a.CategoryId, b.CategoryId));
            asset.Racks = rackEntries.ToArray();

            var rugEntries = new List<RoomLayoutData.RugEntry>();
            var rugsRoot = GameObject.Find("Room/Rugs");
            if (rugsRoot != null)
            {
                foreach (Transform t in rugsRoot.transform)
                {
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr == null) continue;
                    rugEntries.Add(new RoomLayoutData.RugEntry
                    {
                        Name = t.name,
                        Position = t.position,
                        Size = mr.bounds.size
                    });
                }
            }
            asset.Rugs = rugEntries.ToArray();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("SortThem: captured " + asset.Racks.Length + " racks and " + asset.Rugs.Length + " rugs into " + Path);
        }

        public static RoomLayoutData Load() => AssetDatabase.LoadAssetAtPath<RoomLayoutData>(Path);
    }
}
