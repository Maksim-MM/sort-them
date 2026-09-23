using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;

namespace SortThem.Editor
{
    public static class BuildTool
    {
        [MenuItem("SortThem/Build WebGL")]
        public static void BuildWebGL() => Build("Builds/WebGL", BuildOptions.None);

        [MenuItem("SortThem/Build WebGL (Profiler)")]
        public static void BuildWebGLProfiler()
        {
            var prev = PlayerSettings.insecureHttpOption;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            try { Build("Builds/WebGL_Profile", BuildOptions.Development | BuildOptions.ConnectWithProfiler); }
            finally { PlayerSettings.insecureHttpOption = prev; }
        }

        [MenuItem("SortThem/Build WebGL (Diag)")]
        public static void BuildWebGLDiag()
        {
            var prevHttp = PlayerSettings.insecureHttpOption;
            var prevExc = PlayerSettings.WebGL.exceptionSupport;
            var prevDiag = PlayerSettings.WebGL.showDiagnostics;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithoutStacktrace;
            PlayerSettings.WebGL.showDiagnostics = true;
            try { Build("Builds/WebGL/WebGL_Diag", BuildOptions.None); }
            finally
            {
                PlayerSettings.insecureHttpOption = prevHttp;
                PlayerSettings.WebGL.exceptionSupport = prevExc;
                PlayerSettings.WebGL.showDiagnostics = prevDiag;
            }
        }

        static void EnsureWebGLTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            AddressablesRuntimeProperties.ClearCachedPropertyValues();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            string target = settings.profileSettings.EvaluateString(settings.activeProfileId, "[BuildTarget]");
            if (target != "WebGL")
                throw new BuildFailedException($"Addressables [BuildTarget] = {target}, ожидается WebGL");
        }

        static void Build(string folder, BuildOptions buildOptions)
        {
            EnsureWebGLTarget();
            string output = Path.Combine(Directory.GetCurrentDirectory(), folder);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { Paths.MainScene },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = buildOptions
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"SortThem: WebGL build {s.result}, size {s.totalSize / (1024f * 1024f):0.0} MB, time {s.totalTime.TotalSeconds:0} s, errors {s.totalErrors}, warnings {s.totalWarnings}, output {s.outputPath}");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
