using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class EventWindowController : MonoBehaviour
{
    [SerializeField] private GameObject pearlChallengeWindowRoot;
    [SerializeField] private GameObject dimBackgroundRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject[] objectsToHideWhileWindowOpen;
    [SerializeField] private bool configureResponsiveLayout = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [SerializeField] private Vector2 windowScreenPadding = new Vector2(24f, 80f);
    [SerializeField] private float minimumWindowScale = 0.9f;
    [SerializeField] private float openAnimationDuration = 0.2f;
    [SerializeField] private float openStartScale = 0.96f;
    [SerializeField] private float openBounceScale = 1.02f;

    private Coroutine openAnimationRoutine;
    private Vector3 windowDefaultScale = Vector3.one;
    private Vector3 windowContainerDefaultScale = Vector3.one;
    private RectTransform windowRectTransform;
    private RectTransform windowContainerRectTransform;
    private RectTransform dimBackgroundRectTransform;
    private RectTransform parentCanvasRectTransform;

    private void Awake()
    {
        ValidateReferences();

        if (pearlChallengeWindowRoot != null)
        {
            windowRectTransform = pearlChallengeWindowRoot.GetComponent<RectTransform>();
            windowContainerRectTransform = pearlChallengeWindowRoot.transform.Find("Container") as RectTransform;
            windowDefaultScale = pearlChallengeWindowRoot.transform.localScale;

            if (windowContainerRectTransform != null)
            {
                windowContainerDefaultScale = windowContainerRectTransform.localScale;
            }

            ConfigureResponsiveLayout();
            pearlChallengeWindowRoot.SetActive(false);
        }

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRectTransform = dimBackgroundRoot.GetComponent<RectTransform>();
            ConfigureDimBackgroundLayout();
            dimBackgroundRoot.SetActive(false);
        }

    }

    private void OnEnable()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    private void OnDisable()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }
    }

    private void OnDestroy()
    {
        StopOpenAnimation();
    }

    public void OpenWindow()
    {
        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(EventWindowController)} on '{name}' cannot open the window because {nameof(pearlChallengeWindowRoot)} is not assigned.", this);
            return;
        }

        if (dimBackgroundRoot != null)
        {
            ConfigureDimBackgroundLayout();
            dimBackgroundRoot.SetActive(true);
        }

        SetBackgroundObjectsVisible(false);
        CenterWindow();
        FitWindowToScreen();
        pearlChallengeWindowRoot.SetActive(true);
        PlayOpenAnimation();
    }

    public void CloseWindow()
    {
        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(EventWindowController)} on '{name}' cannot close the window because {nameof(pearlChallengeWindowRoot)} is not assigned.", this);
            return;
        }

        StopOpenAnimation();
        pearlChallengeWindowRoot.transform.localScale = windowDefaultScale;
        pearlChallengeWindowRoot.SetActive(false);

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRoot.SetActive(false);
        }

        SetBackgroundObjectsVisible(true);
    }

    private bool ValidateReferences()
    {
        var isValid = true;

        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(EventWindowController)} on '{name}' is missing a reference to {nameof(pearlChallengeWindowRoot)}.", this);
            isValid = false;
        }

        if (openButton == null)
        {
            Debug.LogError($"{nameof(EventWindowController)} on '{name}' is missing a reference to {nameof(openButton)}.", this);
            isValid = false;
        }

        if (dimBackgroundRoot == null)
        {
            Debug.LogError($"{nameof(EventWindowController)} on '{name}' is missing a reference to {nameof(dimBackgroundRoot)}.", this);
            isValid = false;
        }

        if (closeButton == null)
        {
            Debug.LogError($"{nameof(EventWindowController)} on '{name}' is missing a reference to {nameof(closeButton)}.", this);
            isValid = false;
        }

        return isValid;
    }

    private void SetBackgroundObjectsVisible(bool isVisible)
    {
        if (objectsToHideWhileWindowOpen == null)
        {
            return;
        }

        for (var i = 0; i < objectsToHideWhileWindowOpen.Length; i++)
        {
            if (objectsToHideWhileWindowOpen[i] == null)
            {
                continue;
            }

            objectsToHideWhileWindowOpen[i].SetActive(isVisible);
        }
    }

    private void PlayOpenAnimation()
    {
        StopOpenAnimation();
        openAnimationRoutine = StartCoroutine(AnimateOpen());
    }

    private void StopOpenAnimation()
    {
        if (openAnimationRoutine == null)
        {
            return;
        }

        StopCoroutine(openAnimationRoutine);
        openAnimationRoutine = null;
    }

    private IEnumerator AnimateOpen()
    {
        var windowTransform = pearlChallengeWindowRoot.transform;
        var startScale = windowDefaultScale * openStartScale;
        var bounceScale = windowDefaultScale * openBounceScale;

        windowTransform.localScale = startScale;

        var bounceDuration = openAnimationDuration * 0.65f;
        var settleDuration = openAnimationDuration - bounceDuration;

        yield return ScaleWindow(windowTransform, startScale, bounceScale, bounceDuration);
        yield return ScaleWindow(windowTransform, bounceScale, windowDefaultScale, settleDuration);

        windowTransform.localScale = windowDefaultScale;
        openAnimationRoutine = null;
    }

    private static IEnumerator ScaleWindow(Transform windowTransform, Vector3 fromScale, Vector3 toScale, float duration)
    {
        if (duration <= 0f)
        {
            windowTransform.localScale = toScale;
            yield break;
        }

        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            windowTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, easedProgress);
            yield return null;
        }

        windowTransform.localScale = toScale;
    }

    private void ConfigureResponsiveLayout()
    {
        if (!configureResponsiveLayout)
        {
            return;
        }

        ConfigureCanvasScaler();
        CenterWindow();
        FitWindowToScreen();
        ConfigureDimBackgroundLayout();
    }

    private void ConfigureCanvasScaler()
    {
        var parentCanvas = pearlChallengeWindowRoot.GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            return;
        }

        parentCanvasRectTransform = parentCanvas.transform as RectTransform;

        var canvasScaler = parentCanvas.GetComponent<CanvasScaler>();

        if (canvasScaler == null)
        {
            return;
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
    }

    private void CenterWindow()
    {
        if (windowRectTransform == null)
        {
            return;
        }

        windowRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        windowRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        windowRectTransform.pivot = new Vector2(0.5f, 0.5f);
        windowRectTransform.anchoredPosition = Vector2.zero;
        windowRectTransform.localPosition = new Vector3(windowRectTransform.localPosition.x, windowRectTransform.localPosition.y, 0f);

        if (windowContainerRectTransform == null)
        {
            return;
        }

        windowContainerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        windowContainerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        windowContainerRectTransform.pivot = new Vector2(0.5f, 0.5f);
        windowContainerRectTransform.anchoredPosition = Vector2.zero;
        windowContainerRectTransform.localPosition = new Vector3(windowContainerRectTransform.localPosition.x, windowContainerRectTransform.localPosition.y, 0f);
    }

    private void FitWindowToScreen()
    {
        if (windowContainerRectTransform == null || parentCanvasRectTransform == null)
        {
            return;
        }

        windowContainerRectTransform.localScale = windowContainerDefaultScale;
        Canvas.ForceUpdateCanvases();

        var containerSize = Vector2.Scale(windowContainerRectTransform.rect.size, windowContainerDefaultScale);
        var canvasSize = parentCanvasRectTransform.rect.size;
        var availableSize = new Vector2(
            Mathf.Max(1f, canvasSize.x - windowScreenPadding.x * 2f),
            Mathf.Max(1f, canvasSize.y - windowScreenPadding.y * 2f));

        if (containerSize.x <= 0f || containerSize.y <= 0f)
        {
            return;
        }

        var fitScale = Mathf.Min(1f, availableSize.x / containerSize.x, availableSize.y / containerSize.y);
        fitScale = Mathf.Max(minimumWindowScale, fitScale);
        windowContainerRectTransform.localScale = windowContainerDefaultScale * fitScale;
    }

    private void ConfigureDimBackgroundLayout()
    {
        if (dimBackgroundRectTransform == null && dimBackgroundRoot != null)
        {
            dimBackgroundRectTransform = dimBackgroundRoot.GetComponent<RectTransform>();
        }

        if (dimBackgroundRectTransform == null)
        {
            return;
        }

        dimBackgroundRectTransform.anchorMin = Vector2.zero;
        dimBackgroundRectTransform.anchorMax = Vector2.one;
        dimBackgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);
        dimBackgroundRectTransform.offsetMin = Vector2.zero;
        dimBackgroundRectTransform.offsetMax = Vector2.zero;
        dimBackgroundRectTransform.anchoredPosition = Vector2.zero;
        dimBackgroundRectTransform.localPosition = new Vector3(dimBackgroundRectTransform.localPosition.x, dimBackgroundRectTransform.localPosition.y, 0f);
    }
}
