using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ForcePlayFromMainMenu
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";

    static ForcePlayFromMainMenu()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EnsureMainMenuStartScene();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EnsureMainMenuStartScene();
        }
    }

    private static void EnsureMainMenuStartScene()
    {
        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenuScene == null)
        {
            UnityEngine.Debug.LogWarning($"ForcePlayFromMainMenu could not find scene at '{MainMenuScenePath}'.");
            return;
        }

        if (EditorSceneManager.playModeStartScene != mainMenuScene)
        {
            EditorSceneManager.playModeStartScene = mainMenuScene;
        }
    }
}
