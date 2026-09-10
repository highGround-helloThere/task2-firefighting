using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FireRescueAndroidBuild
{
    private const string OutputPath = "Builds/Android/UnityFireRescue.apk";

    [MenuItem("Tools/Fire Rescue/Build Android APK")]
    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");

        string outputDirectory = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[Android Build] {summary.result}: {summary.outputPath} ({summary.totalSize} bytes)");

        if (summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Android build failed: {summary.result}, {summary.totalErrors} errors.");
    }
}
