using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIButtonPressScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Scale")]
    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.9f;
    [SerializeField, Min(0f)] private float pressDuration = 0.06f;
    [SerializeField, Min(0f)] private float releaseDuration = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 defaultScale;
    private Coroutine scaleRoutine;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        StopScaleRoutine();

        if (isInitialized && target != null)
        {
            target.localScale = defaultScale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Initialize();
        AnimateTo(defaultScale * pressedScale, pressDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(defaultScale, releaseDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(defaultScale, releaseDuration);
    }

    public void OnCancel(BaseEventData eventData)
    {
        AnimateTo(defaultScale, releaseDuration);
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        target = target != null ? target : transform as RectTransform;
        defaultScale = target != null ? target.localScale : Vector3.one;
        isInitialized = true;
    }

    private void AnimateTo(Vector3 targetScale, float duration)
    {
        if (target == null)
        {
            return;
        }

        StopScaleRoutine();

        if (!isActiveAndEnabled || duration <= 0f)
        {
            target.localScale = targetScale;
            return;
        }

        scaleRoutine = StartCoroutine(ScaleTo(targetScale, duration));
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        var startScale = target.localScale;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            target.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedProgress);
            yield return null;
        }

        target.localScale = targetScale;
        scaleRoutine = null;
    }

    private void StopScaleRoutine()
    {
        if (scaleRoutine == null)
        {
            return;
        }

        StopCoroutine(scaleRoutine);
        scaleRoutine = null;
    }
}
