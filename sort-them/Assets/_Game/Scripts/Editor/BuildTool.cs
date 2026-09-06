using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SortThem.Editor
{
    public static class BuildTool
    {
        [MenuItem("SortThem/Build WebGL")]
        public static void BuildWebGL()
        {
            string output = Path.Combine(Directory.GetCurrentDirectory(), "Builds/WebGL");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { Paths.MainScene },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"SortThem: WebGL build {s.result}, size {s.totalSize / (1024f * 1024f):0.0} MB, time {s.totalTime.TotalSeconds:0} s, errors {s.totalErrors}, warnings {s.totalWarnings}, output {s.outputPath}");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
