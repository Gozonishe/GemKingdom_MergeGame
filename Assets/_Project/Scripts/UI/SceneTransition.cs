using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minLoadingTime = 0.8f;

    private bool isTransitioning;

    public CanvasGroup LoadingCanvasGroup => loadingCanvasGroup;
    public float FadeDuration => fadeDuration;
    public float MinLoadingTime => minLoadingTime;

    public void LoadScene(string sceneName)
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        HideLoadingScreen();
    }

    private void HideLoadingScreen()
    {
        if (loadingCanvasGroup == null)
        {
            Debug.LogWarning($"{nameof(SceneTransition)} on '{name}' is missing {nameof(loadingCanvasGroup)}.", this);
            return;
        }

        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.interactable = false;
        loadingCanvasGroup.blocksRaycasts = false;
        loadingCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (loadingCanvasGroup == null)
        {
            yield break;
        }

        var elapsedTime = 0f;
        loadingCanvasGroup.alpha = from;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            var progress = fadeDuration > 0f ? Mathf.Clamp01(elapsedTime / fadeDuration) : 1f;
            loadingCanvasGroup.alpha = Mathf.Lerp(from, to, progress);
            yield return null;
        }

        loadingCanvasGroup.alpha = to;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (loadingCanvasGroup == null)
        {
            Debug.LogWarning($"{nameof(SceneTransition)} on '{name}' is missing {nameof(loadingCanvasGroup)}.", this);
            isTransitioning = false;
            yield break;
        }

        loadingCanvasGroup.gameObject.SetActive(true);
        loadingCanvasGroup.interactable = true;
        loadingCanvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f);

        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        var loadingTime = 0f;

        while (operation.progress < 0.9f || loadingTime < minLoadingTime)
        {
            loadingTime += Time.deltaTime;
            yield return null;
        }

        operation.allowSceneActivation = true;

        yield return null;

        yield return Fade(1f, 0f);

        loadingCanvasGroup.interactable = false;
        loadingCanvasGroup.blocksRaycasts = false;
        loadingCanvasGroup.gameObject.SetActive(false);
        isTransitioning = false;
    }
}
