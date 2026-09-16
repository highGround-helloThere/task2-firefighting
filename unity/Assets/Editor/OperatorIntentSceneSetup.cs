using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OperatorIntentSceneSetup
{
    private const string ScenePath = "Assets/FireRescue/Scenes/FireRescue_Main.unity";

    [MenuItem("Fire Rescue/Configure Operator Intent Acceptance")]
    public static void ConfigureMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RobotSyncManager robotSyncManager = Object.FindFirstObjectByType<RobotSyncManager>();
        if (robotSyncManager == null)
        {
            Debug.LogError("[OperatorIntentSceneSetup] RobotSyncManager was not found.");
            return;
        }

        GameObject acceptanceRoot = FindOrCreate("Operator Input Acceptance");
        KeyboardIntentSource keyboardSource = GetOrAdd<KeyboardIntentSource>(
            FindOrCreateChild(acceptanceRoot, "Keyboard Intent Source"));
        OperatorIntentRouter router = GetOrAdd<OperatorIntentRouter>(
            FindOrCreateChild(acceptanceRoot, "Operator Intent Router"));
        OperatorIntentRobotBridge bridge = GetOrAdd<OperatorIntentRobotBridge>(
            FindOrCreateChild(acceptanceRoot, "Operator Intent Robot Bridge"));
        RobotAvatarController avatarController = GetOrAdd<RobotAvatarController>(
            FindOrCreateChild(acceptanceRoot, "Robot Avatar Controller"));

        SerializedObject routerObject = new SerializedObject(router);
        routerObject.FindProperty("sourceBehaviour").objectReferenceValue = keyboardSource;
        routerObject.ApplyModifiedPropertiesWithoutUndo();

        bridge.robotSyncManager = robotSyncManager;
        bridge.avatarController = avatarController;
        bridge.intentRouter = router;

        EditorUtility.SetDirty(keyboardSource);
        EditorUtility.SetDirty(router);
        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(avatarController);
        EnsureMainSceneInBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[OperatorIntentSceneSetup] Main scene configured for W/S/A/D/J/Esc operator acceptance.");
    }

    private static void EnsureMainSceneInBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene buildScene in scenes)
        {
            if (buildScene.path == ScenePath)
                return;
        }

        EditorBuildSettingsScene[] updatedScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(updatedScenes, 0);
        updatedScenes[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updatedScenes;
    }

    private static GameObject FindOrCreate(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
            return existing;

        return new GameObject(objectName);
    }

    private static GameObject FindOrCreateChild(GameObject parent, string objectName)
    {
        Transform child = parent.transform.Find(objectName);
        if (child != null)
            return child.gameObject;

        GameObject created = new GameObject(objectName);
        created.transform.SetParent(parent.transform, false);
        return created;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }
}
