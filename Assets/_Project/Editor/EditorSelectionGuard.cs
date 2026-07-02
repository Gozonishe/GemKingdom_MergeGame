using UnityEditor;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class EditorSelectionGuard
{
    static EditorSelectionGuard()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode
            || state == PlayModeStateChange.EnteredPlayMode
            || state == PlayModeStateChange.ExitingPlayMode)
        {
            ClearSelection();
        }
    }

    private static void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ClearSelection();
        }
    }

    private static void HandleSceneUnloaded(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ClearSelection();
        }
    }

    private static void ClearSelection()
    {
        if (Selection.activeObject == null)
        {
            return;
        }

        Selection.activeObject = null;
        EditorApplication.delayCall += () => Selection.activeObject = null;
    }
}
