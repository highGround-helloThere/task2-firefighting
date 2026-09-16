using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FireRescueRunRequest
{
    private const string ScenePath = "Assets/FireRescue/Scenes/FireRescue_Main.unity";
    private const string RequestFileName = ".fire-rescue-run-request";
    private const string AutoTestRequestFileName = ".fire-rescue-auto-test-request";
    private const double AutoTestDurationSeconds = 6.0;
    private static bool autoTestRunning;
    private static bool autoPlayRequested;
    private static double autoTestStopAt;

    static FireRescueRunRequest()
    {
        // Keep watching so a request created after the editor has opened is also handled.
        EditorApplication.update += ProcessRequest;
        EditorApplication.update += ProcessAutoTestRequest;
    }

    public static void RunFromCommandLine()
    {
        EditorApplication.delayCall += ProcessRequest;
    }

    private static string RequestPath => Path.GetFullPath(
        Path.Combine(Application.dataPath, "..", RequestFileName));

    private static string AutoTestRequestPath => Path.GetFullPath(
        Path.Combine(Application.dataPath, "..", AutoTestRequestFileName));

    private static void ProcessAutoTestRequest()
    {
        if (autoTestRunning)
        {
            if (EditorApplication.timeSinceStartup < autoTestStopAt)
                return;

            RobotSyncManager activeRobot = UnityEngine.Object.FindFirstObjectByType<RobotSyncManager>();
            activeRobot?.SetAutonomousMode(false);
            autoTestRunning = false;
            Debug.Log("[AUTO TEST] Completed 6-second run; autonomous mode disabled.");
            return;
        }

        if (!File.Exists(AutoTestRequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        if (!EditorApplication.isPlaying)
        {
            if (!autoPlayRequested)
            {
                autoPlayRequested = true;
                EditorApplication.isPlaying = true;
            }
            return;
        }

        RobotSyncManager robot = UnityEngine.Object.FindFirstObjectByType<RobotSyncManager>();
        if (robot == null || !robot.IsConnected)
            return;

        File.Delete(AutoTestRequestPath);
        autoPlayRequested = false;
        robot.SetAutonomousMode(true);
        autoTestRunning = true;
        autoTestStopAt = EditorApplication.timeSinceStartup + AutoTestDurationSeconds;
        Debug.Log("[AUTO TEST] Started 6-second autonomous cruise run.");
    }

    private static void ProcessRequest()
    {
        if (!File.Exists(RequestPath))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ProcessRequest;
            return;
        }

        try
        {
            CloseMcpSetupWindow();

            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("[FireRescueRunRequest] The active scene has unsaved changes; automatic run was cancelled.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            File.Delete(RequestPath);
            Debug.Log("[FireRescueRunRequest] FireRescue_Main loaded; entering Play mode.");
            EditorApplication.delayCall += EnterPlayMode;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FireRescueRunRequest] Failed to start acceptance run: {exception}");
        }
    }

    private static void EnterPlayMode()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnterPlayMode;
            return;
        }

        EditorApplication.isPlaying = true;
    }

    private static void CloseMcpSetupWindow()
    {
        foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (string.Equals(window.titleContent?.text, "MCP Setup", StringComparison.OrdinalIgnoreCase))
                window.Close();
        }
    }
}
