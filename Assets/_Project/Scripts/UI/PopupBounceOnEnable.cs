using System.Collections;
using UnityEngine;

public sealed class PopupBounceOnEnable : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField, Range(0f, 2f)] private float startScale = 0.92f;
    [SerializeField, Range(0f, 2f)] private float overshootScale = 1.03f;
    [SerializeField, Min(0f)] private float growDuration = 0.14f;
    [SerializeField, Min(0f)] private float settleDuration = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool restoreScaleOnDisable = true;

    private Coroutine playRoutine;
    private Vector3 targetScale = Vector3.one;
    private bool hasTargetScale;

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        StopPlayRoutine();

        if (restoreScaleOnDisable && hasTargetScale && target != null)
        {
            target.localScale = targetScale;
        }
    }

    public void Play()
    {
        ResolveTarget();

        if (target == null)
        {
            return;
        }

        StopPlayRoutine();
        targetScale = target.localScale;
        hasTargetScale = true;
        playRoutine = StartCoroutine(PlayBounce());
    }

    private IEnumerator PlayBounce()
    {
        var start = targetScale * startScale;
        var overshoot = targetScale * overshootScale;

        target.localScale = start;

        yield return AnimateScale(start, overshoot, growDuration);
        yield return AnimateScale(overshoot, targetScale, settleDuration);

        target.localScale = targetScale;
        playRoutine = null;
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            target.localScale = Vector3.LerpUnclamped(from, to, easedProgress);
            yield return null;
        }

        target.localScale = to;
    }

    private void ResolveTarget()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }
    }

    private void StopPlayRoutine()
    {
        if (playRoutine == null)
        {
            return;
        }

        StopCoroutine(playRoutine);
        playRoutine = null;
    }
}
