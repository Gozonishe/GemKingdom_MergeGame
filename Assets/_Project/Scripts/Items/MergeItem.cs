using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class MergeItem : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private MergeItemData data;

    [Header("Visuals")]
    [SerializeField] private Image iconImage;

    [Header("Merge Pop Effect")]
    [SerializeField] private bool playPopOnMerge = true;
    [SerializeField] private float popDuration = 0.18f;
    [SerializeField] private float popScale = 1.18f;

    [Header("Fall Effect")]
    [SerializeField] private float defaultFallDuration = 0.22f;
    [SerializeField] private float defaultFallBouncePixels = 7f;

    [Header("Runtime State")]
    [field: SerializeField] public BoardCell CurrentCell { get; private set; }

    private Coroutine popCoroutine;
    private Coroutine fallCoroutine;
    private CanvasGroup canvasGroup;

    public MergeItemData Data => data;
    public int Level => data != null ? data.Level : 0;
    public bool IsAnimatingFall { get; private set; }

    private void Awake()
    {
        iconImage = iconImage != null ? iconImage : GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        RefreshVisuals();
    }

    private void Reset()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnValidate()
    {
        iconImage = iconImage != null ? iconImage : GetComponent<Image>();
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
        RefreshVisuals();
    }

    public void SetData(MergeItemData itemData)
    {
        data = itemData;
        RefreshVisuals();
    }

    public int GetLevel()
    {
        return Level;
    }

    public bool CanMergeWith(MergeItem otherItem)
    {
        if (otherItem == null || data == null || otherItem.Data == null)
        {
            return false;
        }

        if (data.DestroyBothOnAnyNeighborMerge || otherItem.Data.DestroyBothOnAnyNeighborMerge)
        {
            return !data.IsSpider && !otherItem.Data.IsSpider;
        }

        return !data.IsSpider
            && !otherItem.Data.IsSpider
            && !data.ReactToAdjacentMerge
            && !otherItem.Data.ReactToAdjacentMerge
            && data.CanMergeToNextLevel
            && data == otherItem.Data;
    }

    public void SetCell(BoardCell cell)
    {
        CurrentCell = cell;
    }

    public void PlayMergePopEffect()
    {
        if (!playPopOnMerge || !isActiveAndEnabled)
        {
            return;
        }

        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
        }

        popCoroutine = StartCoroutine(PlayPopCoroutine());
    }

    public void PlayFallToCellEffect(float duration = -1f, float bouncePixels = -1f, bool fadeIn = false)
    {
        if (!isActiveAndEnabled || transform is not RectTransform itemRectTransform)
        {
            return;
        }

        if (fallCoroutine != null)
        {
            StopCoroutine(fallCoroutine);
        }

        var resolvedDuration = duration > 0f ? duration : defaultFallDuration;
        var resolvedBouncePixels = bouncePixels >= 0f ? bouncePixels : defaultFallBouncePixels;
        fallCoroutine = StartCoroutine(PlayFallCoroutine(itemRectTransform, resolvedDuration, resolvedBouncePixels, fadeIn));
    }

    public void RefreshVisuals()
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = data != null ? data.Icon : null;
    }

    private IEnumerator PlayPopCoroutine()
    {
        var itemTransform = transform;
        var baseScale = Vector3.one;
        var peakScale = baseScale * Mathf.Max(1f, popScale);
        var halfDuration = Mathf.Max(0.01f, popDuration * 0.5f);

        itemTransform.localScale = baseScale;

        yield return ScaleTo(itemTransform, baseScale, peakScale, halfDuration);
        yield return ScaleTo(itemTransform, peakScale, baseScale, halfDuration);

        itemTransform.localScale = baseScale;
        popCoroutine = null;
    }

    private IEnumerator PlayFallCoroutine(RectTransform itemRectTransform, float duration, float bouncePixels, bool fadeIn)
    {
        IsAnimatingFall = true;
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();

        if (fadeIn && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        var startPosition = itemRectTransform.anchoredPosition;
        var targetPosition = Vector2.zero;
        var bounceOffset = new Vector2(0f, -Mathf.Max(0f, bouncePixels));
        var resolvedDuration = Mathf.Max(0.01f, duration);
        var fallDuration = bouncePixels > 0f ? resolvedDuration * 0.82f : resolvedDuration;
        var settleDuration = resolvedDuration - fallDuration;
        var fallTargetPosition = bouncePixels > 0f ? bounceOffset : targetPosition;
        var elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / fallDuration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            itemRectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, fallTargetPosition, easedProgress);
            RefreshFadeInAlpha(fadeIn, elapsed / resolvedDuration);
            yield return null;
        }

        if (bouncePixels > 0f)
        {
            yield return MoveAnchoredPosition(itemRectTransform, bounceOffset, targetPosition, settleDuration);
        }

        itemRectTransform.anchoredPosition = targetPosition;
        RefreshFadeInAlpha(fadeIn, 1f);
        IsAnimatingFall = false;
        fallCoroutine = null;
    }

    private static IEnumerator ScaleTo(Transform target, Vector3 fromScale, Vector3 toScale, float duration)
    {
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            target.localScale = Vector3.LerpUnclamped(fromScale, toScale, easedProgress);
            yield return null;
        }

        target.localScale = toScale;
    }

    private static IEnumerator MoveAnchoredPosition(RectTransform target, Vector2 fromPosition, Vector2 toPosition, float duration)
    {
        var elapsed = 0f;
        var resolvedDuration = Mathf.Max(0.01f, duration);

        while (elapsed < resolvedDuration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / resolvedDuration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
            target.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, easedProgress);
            yield return null;
        }

        target.anchoredPosition = toPosition;
    }

    private void RefreshFadeInAlpha(bool fadeIn, float progress)
    {
        if (fadeIn && canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(progress);
        }
    }
}
