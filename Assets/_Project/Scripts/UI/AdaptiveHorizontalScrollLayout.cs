using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class AdaptiveHorizontalScrollLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroup;
    [SerializeField] private ContentSizeFitter contentSizeFitter;

    [Header("Behavior")]
    [SerializeField, Min(0f)] private float fitTolerance = 0.5f;
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool refreshOnSizeChange = true;
    [SerializeField] private bool overrideMovementType;
    [SerializeField] private ScrollRect.MovementType movementType = ScrollRect.MovementType.Clamped;

    private Coroutine delayedRefreshCoroutine;

    private void Awake()
    {
        ResolveReferences();
        ApplyScrollDefaults();
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
        {
            RefreshLayoutDelayed();
        }
    }

    private void OnDisable()
    {
        if (delayedRefreshCoroutine != null)
        {
            StopCoroutine(delayedRefreshCoroutine);
            delayedRefreshCoroutine = null;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (refreshOnSizeChange && isActiveAndEnabled)
        {
            RefreshLayoutDelayed();
        }
    }

    [ContextMenu("Refresh Layout")]
    public void RefreshLayout()
    {
        ResolveReferences();

        if (scrollRect == null || content == null || viewport == null || horizontalLayoutGroup == null)
        {
            Debug.LogError($"{nameof(AdaptiveHorizontalScrollLayout)} on '{name}' has missing references.", this);
            return;
        }

        ApplyScrollDefaults();
        RebuildNow();

        var viewportWidth = viewport.rect.width;
        var preferredContentWidth = LayoutUtility.GetPreferredWidth(content);
        var contentHeight = Mathf.Max(content.rect.height, viewport.rect.height);

        var shouldScroll = preferredContentWidth > viewportWidth + fitTolerance;
        if (shouldScroll)
        {
            ApplyScrollableLayout(preferredContentWidth, contentHeight);
        }
        else
        {
            ApplyCenteredLayout(preferredContentWidth, contentHeight);
        }

        RebuildNow();
    }

    public void RefreshLayoutDelayed()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (delayedRefreshCoroutine != null)
        {
            StopCoroutine(delayedRefreshCoroutine);
        }

        delayedRefreshCoroutine = StartCoroutine(RefreshLayoutNextFrame());
    }

    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null;
        RefreshLayout();
        delayedRefreshCoroutine = null;
    }

    private void ApplyCenteredLayout(float preferredContentWidth, float contentHeight)
    {
        scrollRect.horizontal = false;
        scrollRect.velocity = Vector2.zero;

        horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        SetContentHorizontalTransform(0.5f, 0.5f, 0.5f, preferredContentWidth, contentHeight);

        scrollRect.horizontalNormalizedPosition = 0.5f;
    }

    private void ApplyScrollableLayout(float preferredContentWidth, float contentHeight)
    {
        scrollRect.horizontal = true;
        scrollRect.velocity = Vector2.zero;

        horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        SetContentHorizontalTransform(0f, 0f, 0f, preferredContentWidth, contentHeight);

        scrollRect.horizontalNormalizedPosition = 0f;
    }

    private void SetContentHorizontalTransform(float anchorX, float pivotX, float anchoredPositionX, float width, float height)
    {
        var anchorMin = content.anchorMin;
        var anchorMax = content.anchorMax;
        var pivot = content.pivot;
        var anchoredPosition = content.anchoredPosition;

        anchorMin.x = anchorX;
        anchorMax.x = anchorX;
        pivot.x = pivotX;
        anchoredPosition.x = anchoredPositionX;

        content.anchorMin = anchorMin;
        content.anchorMax = anchorMax;
        content.pivot = pivot;
        content.anchoredPosition = anchoredPosition;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, width));
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, height));
    }

    private void ApplyScrollDefaults()
    {
        if (scrollRect == null)
        {
            return;
        }

        scrollRect.vertical = false;

        if (overrideMovementType)
        {
            scrollRect.movementType = movementType;
        }
    }

    private void RebuildNow()
    {
        Canvas.ForceUpdateCanvases();

        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        if (viewport != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
        }
    }

    private void ResolveReferences()
    {
        scrollRect = scrollRect != null ? scrollRect : GetComponent<ScrollRect>();

        if (content == null && scrollRect != null)
        {
            content = scrollRect.content;
        }

        if (viewport == null && scrollRect != null)
        {
            viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
        }

        if (horizontalLayoutGroup == null && content != null)
        {
            horizontalLayoutGroup = content.GetComponent<HorizontalLayoutGroup>();
        }

        if (contentSizeFitter == null && content != null)
        {
            contentSizeFitter = content.GetComponent<ContentSizeFitter>();
        }

        if (contentSizeFitter != null)
        {
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }
}
