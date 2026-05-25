using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardsPopupController : MonoBehaviour
{
    private struct GraphicAlphaState
    {
        public Graphic Graphic;
        public float Alpha;
    }

    private struct SpriteRendererAlphaState
    {
        public SpriteRenderer SpriteRenderer;
        public float Alpha;
    }

    [Serializable]
    private struct RewardData
    {
        [SerializeField] private Sprite icon;
        [SerializeField] private int amount;

        public Sprite Icon => icon;
        public int Amount => amount;
    }

    [SerializeField] private Button openButton;
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button dimCloseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject[] objectsToHideWhilePopupOpen;
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private RewardItemView rewardItemPrefab;
    [SerializeField] private RewardData[] rewards;
    [SerializeField] private float rewardFadeDuration = 0.25f;
    [SerializeField] private float closeFadeDuration = 0.2f;

    private CanvasGroup overlayCanvasGroup;
    private GraphicAlphaState[] overlayGraphicAlphaStates = Array.Empty<GraphicAlphaState>();
    private SpriteRendererAlphaState[] overlaySpriteRendererAlphaStates = Array.Empty<SpriteRendererAlphaState>();

    private void Awake()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }

        if (openButton != null)
        {
            openButton.onClick.AddListener(Open);
        }

        if (dimCloseButton != null)
        {
            dimCloseButton.onClick.AddListener(Close);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    private void OnDestroy()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(Open);
        }

        if (dimCloseButton != null)
        {
            dimCloseButton.onClick.RemoveListener(Close);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    public void Open()
    {
        if (overlayRoot == null || rewardsContainer == null || rewardItemPrefab == null)
        {
            Debug.LogError($"{nameof(RewardsPopupController)} on '{name}' is missing required references.", this);
            return;
        }

        StopAllCoroutines();
        ClearRewards();
        SetBackgroundObjectsVisible(false);
        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();
        PrepareOverlayForOpen();

        if (rewards == null)
        {
            return;
        }

        for (var i = 0; i < rewards.Length; i++)
        {
            var rewardItem = Instantiate(rewardItemPrefab, rewardsContainer);
            rewardItem.Set(rewards[i].Icon, rewards[i].Amount);

            var canvasGroup = rewardItem.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rewardItem.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            StartCoroutine(FadeInReward(canvasGroup));
            rewardItem.PlayAppearEffect();
        }
    }

    public void Close()
    {
        StopAllCoroutines();

        if (overlayRoot == null)
        {
            SetBackgroundObjectsVisible(true);
            return;
        }

        StartCoroutine(FadeOutAndClose());
    }

    private void ClearRewards()
    {
        for (var i = rewardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(rewardsContainer.GetChild(i).gameObject);
        }
    }

    private void SetBackgroundObjectsVisible(bool isVisible)
    {
        if (objectsToHideWhilePopupOpen == null)
        {
            return;
        }

        for (var i = 0; i < objectsToHideWhilePopupOpen.Length; i++)
        {
            if (objectsToHideWhilePopupOpen[i] == null)
            {
                continue;
            }

            objectsToHideWhilePopupOpen[i].SetActive(isVisible);
        }
    }

    private void PrepareOverlayForOpen()
    {
        if (overlayCanvasGroup == null)
        {
            overlayCanvasGroup = GetOrAddCanvasGroup(overlayRoot);
        }

        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = true;
        overlayCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeOutAndClose()
    {
        if (overlayCanvasGroup == null)
        {
            overlayCanvasGroup = GetOrAddCanvasGroup(overlayRoot);
        }

        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.alpha = 1f;
        CacheOverlayGraphicAlphaStates();
        CacheOverlaySpriteRendererAlphaStates();

        if (closeFadeDuration <= 0f)
        {
            SetOverlayGraphicsAlphaMultiplier(0f);
            SetOverlaySpriteRenderersAlphaMultiplier(0f);
        }
        else
        {
            var elapsed = 0f;

            while (elapsed < closeFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / closeFadeDuration));
                SetOverlayGraphicsAlphaMultiplier(alpha);
                SetOverlaySpriteRenderersAlphaMultiplier(alpha);
                yield return null;
            }

            SetOverlayGraphicsAlphaMultiplier(0f);
            SetOverlaySpriteRenderersAlphaMultiplier(0f);
        }

        overlayRoot.SetActive(false);
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = true;
        overlayCanvasGroup.blocksRaycasts = true;
        SetOverlayGraphicsAlphaMultiplier(1f);
        SetOverlaySpriteRenderersAlphaMultiplier(1f);
        SetBackgroundObjectsVisible(true);
    }

    private void CacheOverlayGraphicAlphaStates()
    {
        var graphics = overlayRoot.GetComponentsInChildren<Graphic>(true);
        overlayGraphicAlphaStates = new GraphicAlphaState[graphics.Length];

        for (var i = 0; i < graphics.Length; i++)
        {
            overlayGraphicAlphaStates[i] = new GraphicAlphaState
            {
                Graphic = graphics[i],
                Alpha = graphics[i].color.a
            };
        }
    }

    private void SetOverlayGraphicsAlphaMultiplier(float alphaMultiplier)
    {
        for (var i = 0; i < overlayGraphicAlphaStates.Length; i++)
        {
            var graphic = overlayGraphicAlphaStates[i].Graphic;

            if (graphic == null)
            {
                continue;
            }

            var color = graphic.color;
            color.a = overlayGraphicAlphaStates[i].Alpha * alphaMultiplier;
            graphic.color = color;
        }
    }

    private void CacheOverlaySpriteRendererAlphaStates()
    {
        var spriteRenderers = overlayRoot.GetComponentsInChildren<SpriteRenderer>(true);
        overlaySpriteRendererAlphaStates = new SpriteRendererAlphaState[spriteRenderers.Length];

        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            overlaySpriteRendererAlphaStates[i] = new SpriteRendererAlphaState
            {
                SpriteRenderer = spriteRenderers[i],
                Alpha = spriteRenderers[i].color.a
            };
        }
    }

    private void SetOverlaySpriteRenderersAlphaMultiplier(float alphaMultiplier)
    {
        for (var i = 0; i < overlaySpriteRendererAlphaStates.Length; i++)
        {
            var spriteRenderer = overlaySpriteRendererAlphaStates[i].SpriteRenderer;

            if (spriteRenderer == null)
            {
                continue;
            }

            var color = spriteRenderer.color;
            color.a = overlaySpriteRendererAlphaStates[i].Alpha * alphaMultiplier;
            spriteRenderer.color = color;
        }
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        var canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private IEnumerator FadeInReward(CanvasGroup canvasGroup)
    {
        if (rewardFadeDuration <= 0f)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }

        var elapsed = 0f;

        while (elapsed < rewardFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / rewardFadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
