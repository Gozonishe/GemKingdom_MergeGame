using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HudButtonPressAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float animationDuration = 0.08f;
    [SerializeField] private bool scaleAroundVisualCenter;
    [SerializeField] private bool scaleChildrenInsteadOfTarget;

    private Button button;
    private Coroutine animationRoutine;
    private Vector3 defaultScale = Vector3.one;
    private Vector2 defaultAnchoredPosition;
    private RectTransform targetRect;
    private ChildScaleState[] childScaleStates = new ChildScaleState[0];

    private struct ChildScaleState
    {
        public RectTransform RectTransform;
        public Vector2 AnchoredPosition;
        public Vector3 LocalScale;
    }

    private void Awake()
    {
        button = GetComponent<Button>();

        if (target == null)
        {
            target = FindAnimatedTarget();
        }

        if (target != null)
        {
            defaultScale = target.localScale;
            targetRect = target;
            defaultAnchoredPosition = targetRect.anchoredPosition;
            CacheChildScaleStates();
        }
    }

    private void OnDisable()
    {
        StopAnimation();

        if (target != null)
        {
            ApplyScale(defaultScale);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate())
        {
            return;
        }

        AnimateTo(defaultScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateToDefault();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateToDefault();
    }

    private void AnimateToDefault()
    {
        if (target == null)
        {
            return;
        }

        AnimateTo(defaultScale);
    }

    private void AnimateTo(Vector3 targetScale)
    {
        if (target == null)
        {
            return;
        }

        StopAnimation();
        animationRoutine = StartCoroutine(AnimateScale(target.localScale, targetScale));
    }

    private IEnumerator AnimateScale(Vector3 fromScale, Vector3 toScale)
    {
        if (animationDuration <= 0f)
        {
            ApplyScale(toScale);
            animationRoutine = null;
            yield break;
        }

        var elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / animationDuration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            ApplyScale(Vector3.LerpUnclamped(fromScale, toScale, easedProgress));
            yield return null;
        }

        ApplyScale(toScale);
        animationRoutine = null;
    }

    private void ApplyScale(Vector3 scale)
    {
        if (scaleChildrenInsteadOfTarget)
        {
            ApplyChildrenScale(scale);
            return;
        }

        target.localScale = scale;

        if (!scaleAroundVisualCenter || targetRect == null)
        {
            return;
        }

        var rect = targetRect.rect;
        var pivot = targetRect.pivot;
        var centerOffset = new Vector2((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height);
        var scaleRatio = new Vector2(
            defaultScale.x != 0f ? scale.x / defaultScale.x : 1f,
            defaultScale.y != 0f ? scale.y / defaultScale.y : 1f);

        targetRect.anchoredPosition = defaultAnchoredPosition - new Vector2(
            centerOffset.x * (scaleRatio.x - 1f),
            centerOffset.y * (scaleRatio.y - 1f));
    }

    private void CacheChildScaleStates()
    {
        if (!scaleChildrenInsteadOfTarget || targetRect == null)
        {
            childScaleStates = new ChildScaleState[0];
            return;
        }

        var childStates = new ChildScaleState[targetRect.childCount];
        var childStateCount = 0;

        for (var i = 0; i < targetRect.childCount; i++)
        {
            if (targetRect.GetChild(i) is not RectTransform childRect)
            {
                continue;
            }

            childStates[childStateCount] = new ChildScaleState
            {
                RectTransform = childRect,
                AnchoredPosition = childRect.anchoredPosition,
                LocalScale = childRect.localScale
            };
            childStateCount++;
        }

        if (childStateCount != childStates.Length)
        {
            System.Array.Resize(ref childStates, childStateCount);
        }

        childScaleStates = childStates;
    }

    private void ApplyChildrenScale(Vector3 scale)
    {
        var scaleRatio = new Vector2(
            defaultScale.x != 0f ? scale.x / defaultScale.x : 1f,
            defaultScale.y != 0f ? scale.y / defaultScale.y : 1f);

        for (var i = 0; i < childScaleStates.Length; i++)
        {
            var childState = childScaleStates[i];
            if (childState.RectTransform == null)
            {
                continue;
            }

            childState.RectTransform.localScale = new Vector3(
                childState.LocalScale.x * scaleRatio.x,
                childState.LocalScale.y * scaleRatio.y,
                childState.LocalScale.z);
            childState.RectTransform.anchoredPosition = new Vector2(
                childState.AnchoredPosition.x * scaleRatio.x,
                childState.AnchoredPosition.y * scaleRatio.y);
        }
    }

    private void StopAnimation()
    {
        if (animationRoutine == null)
        {
            return;
        }

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }

    private bool CanAnimate()
    {
        return target != null && (button == null || button.IsInteractable());
    }

    private RectTransform FindAnimatedTarget()
    {
        var texture = FindChildRecursive(transform, "Texture");
        if (texture != null)
        {
            return texture;
        }

        var background = FindChildRecursive(transform, "BGImage");
        if (background != null)
        {
            return background;
        }

        var content = FindChildRecursive(transform, "Content");
        return content != null ? content : transform as RectTransform;
    }

    private static RectTransform FindChildRecursive(Transform root, string childName)
    {
        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);

            if (child.name == childName)
            {
                return child as RectTransform;
            }

            var result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
