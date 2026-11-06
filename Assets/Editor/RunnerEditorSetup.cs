using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RunnerEditorSetup
{
    const string ScenePath = "Assets/Scenes/Runner.unity";
    const string FirstOpenKey = "Runner.FirstOpenDone";

    static RunnerEditorSetup()
    {
        EditorApplication.delayCall += FirstOpen;
    }

    static void FirstOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(FirstOpenKey, false))
            return;

        SessionState.SetBool(FirstOpenKey, true);
        Scene activeScene = EditorSceneManager.GetActiveScene();
        bool shouldOpenRunner = (!activeScene.isDirty
                && (string.IsNullOrEmpty(activeScene.path) || activeScene.path == "Assets/Scenes/SampleScene.unity"));

        EnsureScene();
        AddToBuildSettings();
        if (shouldOpenRunner)
            EditorSceneManager.OpenScene(ScenePath);
    }

    static void EnsureScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;

        Scene previousScene = SceneManager.GetActiveScene();
        Scene runnerScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(runnerScene);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.7f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(32, 54, 67, 255);
        cameraObject.AddComponent<AudioListener>();

        new GameObject("RunnerGame").AddComponent<RunnerGame>();
        EditorSceneManager.SaveScene(runnerScene, ScenePath);
        if (previousScene.IsValid() && previousScene.isLoaded)
            SceneManager.SetActiveScene(previousScene);
        EditorSceneManager.CloseScene(runnerScene, true);
    }

    static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path != ScenePath)
                scenes.Add(scene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [MenuItem("Runner/Open Game Scene")]
    static void OpenScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EnsureScene();
        AddToBuildSettings();
        EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Runner/Reset Best Score")]
    static void ResetBestScore()
    {
        PlayerPrefs.DeleteKey("RunnerBest");
        PlayerPrefs.Save();
        Debug.Log("Platform Runner: best score reset.");
    }
}
