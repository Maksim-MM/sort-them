using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SortThem.Editor
{
    public static class MarketingModes
    {
        [MenuItem("SortThem/Marketing/Fountain Mode (hide cars)")]
        public static void FountainMode() => Apply(true);

        [MenuItem("SortThem/Marketing/Level Mode (cars on level)")]
        public static void LevelMode() => Apply(false);

        static void Apply(bool fountain)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MarketingBaker.Scene)
            {
                Debug.LogError("SortThem: open " + MarketingBaker.Scene + " first");
                return;
            }
            var f = Object.FindFirstObjectByType<MarketingFountain>();
            if (f == null)
            {
                Debug.LogError("SortThem: no MarketingFountain in scene");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                var gm = GameManager.I;
                if (gm == null || !gm.Ready) return;
                if (fountain)
                {
                    f.Rearm();
                    Debug.Log("SortThem: fountain mode, loose cars hidden, press R1+L1");
                    return;
                }
                int restored = 0;
                var entries = gm.Layout.Instances;
                foreach (var car in gm.Cars)
                {
                    if (car.State != CarState.Loose || car.InstanceId < 0 || car.InstanceId >= entries.Length) continue;
                    var e = entries[car.InstanceId];
                    car.SetLoose(e.Position, e.Rotation, true);
                    restored++;
                }
                foreach (var crate in gm.Collectibles) if (crate != null) crate.gameObject.SetActive(true);
                f.Disarm();
                Debug.Log("SortThem: level mode, restored " + restored + " loose cars to layout");
                return;
            }

            Undo.RecordObject(f, "Marketing mode");
            f.HideLooseOnStart = fountain;
            EditorUtility.SetDirty(f);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SortThem: marketing scene set to " + (fountain ? "fountain" : "level") + " mode");
        }
    }
}
