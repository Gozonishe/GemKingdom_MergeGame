using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressText;

    [Header("Loading")]
    [SerializeField, Min(0f)] private float minimumShowTime = 0.35f;
    [SerializeField] private string fallbackSceneName = SceneLoadRequest.DefaultTargetSceneName;

    private bool isLoading;

    private void Start()
    {
        ResolveReferences();
        StartLoading();
    }

    public void StartLoading()
    {
        if (isLoading)
        {
            return;
        }

        var targetSceneName = SceneLoadRequest.TargetSceneName;
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            targetSceneName = fallbackSceneName;
        }

        StartCoroutine(LoadTargetScene(targetSceneName));
    }

    private IEnumerator LoadTargetScene(string targetSceneName)
    {
        isLoading = true;
        var startTime = Time.unscaledTime;
        SetProgress(0f);

        var operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"{nameof(LoadingScreenController)} cannot load scene '{targetSceneName}'.", this);
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
            yield return null;
        }

        SetProgress(1f);

        var remainingTime = minimumShowTime - (Time.unscaledTime - startTime);
        if (remainingTime > 0f)
        {
            yield return new WaitForSecondsRealtime(remainingTime);
        }

        SceneLoadRequest.Clear();
        operation.allowSceneActivation = true;
    }

    private void SetProgress(float normalizedProgress)
    {
        normalizedProgress = Mathf.Clamp01(normalizedProgress);

        if (progressSlider != null)
        {
            progressSlider.value = normalizedProgress;
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = normalizedProgress;
        }

        if (progressText != null)
        {
            progressText.text = normalizedProgress >= 1f
                ? "Loading..."
                : $"Loading... {Mathf.RoundToInt(normalizedProgress * 100f)}%";
        }
    }

    private void ResolveReferences()
    {
        if (progressSlider == null)
        {
            progressSlider = GetComponentInChildren<Slider>(true);
        }

        if (progressFillImage == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image != null && image.type == Image.Type.Filled)
                {
                    progressFillImage = image;
                    break;
                }
            }
        }

        if (progressText == null)
        {
            progressText = FindText("LoadingText");
            progressText = progressText != null ? progressText : FindText("BottonText");
        }
    }

    private TMP_Text FindText(string textName)
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text != null && text.name == textName)
            {
                return text;
            }
        }

        return null;
    }
}
