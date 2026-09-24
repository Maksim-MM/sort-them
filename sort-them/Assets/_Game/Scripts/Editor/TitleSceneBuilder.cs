using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SortThem.Editor
{
    public static class TitleSceneBuilder
    {
        public const string BootstrapScene = "Assets/UpscaleSDK/BootstrapScene/UPSBootstrap.unity";
        public const string TitleScene = Paths.Scenes + "/Title.unity";
        const string BackgroundPath = Paths.Root + "/Art/Textures/Background+logo v2.png";
        const string ConfigPath = Paths.Config + "/GameConfig.asset";

        [MenuItem("SortThem/Build Title Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("SortThem: останови Play."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = EditorSceneManager.GetActiveScene().path;

            bool exists = File.Exists(TitleScene);
            var scene = exists
                ? EditorSceneManager.OpenScene(TitleScene, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var screen = Object.FindFirstObjectByType<TitleScreen>();
            if (screen == null) screen = new GameObject("TitleScreen").AddComponent<TitleScreen>();

            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camGo = new GameObject("Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.cullingMask = 0;
                camGo.AddComponent<AudioListener>();
            }

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            screen.Background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            if (config != null)
            {
                screen.Music = config.MusicClips;
                screen.Click = config.UiClickClip;
            }
            screen.NextScene = Path.GetFileNameWithoutExtension(Paths.MainScene);
            EditorUtility.SetDirty(screen);

            EditorSceneManager.SaveScene(scene, TitleScene);
            EnsureBuildScenes();
            Debug.Log("SortThem: title scene " + (exists ? "updated" : "created") + ", background=" + (screen.Background != null) + ", music=" + screen.Music.Length + ", click=" + (screen.Click != null));

            if (!string.IsNullOrEmpty(previous) && previous != TitleScene) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }

        public static void EnsureBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var path in new[] { BootstrapScene, TitleScene, Paths.MainScene })
                if (File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
