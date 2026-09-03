#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceSim.Editor
{
    public static class BuildProject
    {
        private const string MainScene = "Assets/Scenes/space.unity";
        private const string WindowsBuildPath = "Builds/Windows/SpaceSim.exe";

        [MenuItem("SpaceSim/Build/Windows x86_64")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WindowsBuildPath) ?? "Builds/Windows");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"SpaceSim build failed: {summary.result}");
            }

            Debug.Log($"SpaceSim build completed: {WindowsBuildPath} ({summary.totalSize} bytes)");
        }
    }
}
#endif
