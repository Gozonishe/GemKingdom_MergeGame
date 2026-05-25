using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HudButtonPressAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float animationDuration = 0.08f;

    private Button button;
    private Coroutine animationRoutine;
    private Vector3 defaultScale = Vector3.one;

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
        }
    }

    private void OnDisable()
    {
        StopAnimation();

        if (target != null)
        {
            target.localScale = defaultScale;
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
            target.localScale = toScale;
            animationRoutine = null;
            yield break;
        }

        var elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / animationDuration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            target.localScale = Vector3.LerpUnclamped(fromScale, toScale, easedProgress);
            yield return null;
        }

        target.localScale = toScale;
        animationRoutine = null;
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
