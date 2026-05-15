using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoyalRaceWindowController : MonoBehaviour
{
    [SerializeField] private GameObject pearlChallengeWindowRoot;
    [SerializeField] private GameObject dimBackgroundRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private float openAnimationDuration = 0.2f;
    [SerializeField] private float openStartScale = 0.96f;
    [SerializeField] private float openBounceScale = 1.02f;

    private Coroutine openAnimationRoutine;
    private Vector3 windowDefaultScale = Vector3.one;

    private void Awake()
    {
        ValidateReferences();

        if (pearlChallengeWindowRoot != null)
        {
            windowDefaultScale = pearlChallengeWindowRoot.transform.localScale;
            pearlChallengeWindowRoot.SetActive(false);
        }

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRoot.SetActive(false);
        }

        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    private void OnDestroy()
    {
        StopOpenAnimation();

        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }
    }

    public void OpenWindow()
    {
        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(RoyalRaceWindowController)} on '{name}' cannot open the window because {nameof(pearlChallengeWindowRoot)} is not assigned.", this);
            return;
        }

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRoot.SetActive(true);
        }

        pearlChallengeWindowRoot.SetActive(true);
        PlayOpenAnimation();
    }

    public void CloseWindow()
    {
        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(RoyalRaceWindowController)} on '{name}' cannot close the window because {nameof(pearlChallengeWindowRoot)} is not assigned.", this);
            return;
        }

        StopOpenAnimation();
        pearlChallengeWindowRoot.transform.localScale = windowDefaultScale;
        pearlChallengeWindowRoot.SetActive(false);

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRoot.SetActive(false);
        }
    }

    private bool ValidateReferences()
    {
        var isValid = true;

        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(RoyalRaceWindowController)} on '{name}' is missing a reference to {nameof(pearlChallengeWindowRoot)}.", this);
            isValid = false;
        }

        if (openButton == null)
        {
            Debug.LogError($"{nameof(RoyalRaceWindowController)} on '{name}' is missing a reference to {nameof(openButton)}.", this);
            isValid = false;
        }

        if (dimBackgroundRoot == null)
        {
            Debug.LogError($"{nameof(RoyalRaceWindowController)} on '{name}' is missing a reference to {nameof(dimBackgroundRoot)}.", this);
            isValid = false;
        }

        if (closeButton == null)
        {
            Debug.LogError($"{nameof(RoyalRaceWindowController)} on '{name}' is missing a reference to {nameof(closeButton)}.", this);
            isValid = false;
        }

        return isValid;
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
}
