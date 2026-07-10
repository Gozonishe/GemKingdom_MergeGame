using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoadRequest
{
    public const string DefaultLoadingSceneName = "LoadingScreen";
    public const string DefaultTargetSceneName = "MainMenuScreen";

    private static string targetSceneName;

    public static string TargetSceneName
    {
        get => string.IsNullOrWhiteSpace(targetSceneName) ? DefaultTargetSceneName : targetSceneName;
        private set => targetSceneName = value;
    }

    public static void LoadWithLoadingScreen(string sceneName, string loadingSceneName = DefaultLoadingSceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"{nameof(SceneLoadRequest)} cannot load an empty target scene.");
            return;
        }

        TargetSceneName = sceneName;

        if (string.IsNullOrWhiteSpace(loadingSceneName))
        {
            loadingSceneName = DefaultLoadingSceneName;
        }

        SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
    }

    public static void Clear()
    {
        targetSceneName = string.Empty;
    }
}
