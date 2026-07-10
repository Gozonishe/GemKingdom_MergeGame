using UnityEngine;

public sealed class SceneLoadButton : MonoBehaviour
{
    private const string MergeGameSceneName = "MergeGameScreen";
    private const string MergeLevelSceneName = "MergeLevelScene";

    [SerializeField] private string sceneName;

    public void LoadTargetScene()
    {
        if (RequiresLifeToLoad() && !PlayerLives.HasLives)
        {
            OpenLivesAddWindow();
            return;
        }

        if (SceneTransition.Instance == null)
        {
            Debug.LogError($"{nameof(SceneLoadButton)} on '{name}' cannot load scene '{sceneName}' because {nameof(SceneTransition)}.Instance does not exist.", this);
            return;
        }

        SceneTransition.Instance.LoadScene(sceneName);
    }

    private bool RequiresLifeToLoad()
    {
        return string.Equals(sceneName, MergeGameSceneName, System.StringComparison.Ordinal)
            || string.Equals(sceneName, MergeLevelSceneName, System.StringComparison.Ordinal);
    }

    private void OpenLivesAddWindow()
    {
        var screenSwitchController = FindFirstObjectByType<ScreenSwitchController>(FindObjectsInactive.Include);
        if (screenSwitchController == null)
        {
            Debug.LogError($"{nameof(SceneLoadButton)} on '{name}' cannot open LivesAddWindow because {nameof(ScreenSwitchController)} was not found.", this);
            return;
        }

        screenSwitchController.OpenLivesAddWindow();
    }
}
