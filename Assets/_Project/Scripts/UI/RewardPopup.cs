using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardPopup : MonoBehaviour
{
    private const string RewardsOverlayWindowName = "RewardsOverlayWindow";
    private const string RewardsContainerName = "RewardsContainer";
    private const string GeneratedSpriteShineName = "SpriteShineSweep";

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text starsText;
    [SerializeField] private Button claimButton;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float openFadeDuration = 0.25f;
    [SerializeField, Min(0f)] private float closeFadeDuration = 0.2f;

    [Header("Rewards Overlay")]
    [SerializeField] private GameObject rewardsOverlayWindow;
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private RewardItemView rewardItemPrefab;
    [SerializeField] private Sprite coinsIcon;
    [SerializeField] private Sprite starsIcon;

    private bool isVisible;
    private CanvasGroup rootCanvasGroup;
    private Coroutine fadeCoroutine;
    private Button[] overlayCloseButtons = Array.Empty<Button>();

    public event Action Hidden;
    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        rootCanvasGroup = GetOrAddCanvasGroup(root);
        ResolveRewardsOverlayReferences();
        ValidateReferences();

        if (!isVisible && root != null)
        {
            root.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(Hide);
        }

        AddOverlayCloseListeners();
    }

    private void OnDisable()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(Hide);
        }

        RemoveOverlayCloseListeners();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    public void Show(int coins, int stars)
    {
        isVisible = true;

        if (titleText != null)
        {
            titleText.text = "Reward";
        }

        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }

        if (starsText != null)
        {
            starsText.text = stars.ToString();
        }

        if (root != null)
        {
            root.SetActive(true);
        }

        ShowRewardItems(coins, stars);
        AddOverlayCloseListeners();
        FadeIn();
    }

    public void Hide()
    {
        if (!isVisible)
        {
            return;
        }

        isVisible = false;
        FadeOut();
    }

    private void FadeIn()
    {
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetOrAddCanvasGroup(root);
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        if (openFadeDuration <= 0f)
        {
            rootCanvasGroup.alpha = 1f;
            fadeCoroutine = null;
            return;
        }

        rootCanvasGroup.alpha = 0f;
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f, 1f, openFadeDuration, false));
    }

    private void FadeOut()
    {
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetOrAddCanvasGroup(root);
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        if (closeFadeDuration <= 0f)
        {
            CompleteHide();
            return;
        }

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(rootCanvasGroup.alpha, 0f, closeFadeDuration, true));
    }

    private IEnumerator FadeCanvasGroup(float fromAlpha, float toAlpha, float duration, bool hideWhenFinished)
    {
        var elapsed = 0f;
        var resolvedDuration = Mathf.Max(0.01f, duration);

        while (elapsed < resolvedDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / resolvedDuration);
            rootCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
            yield return null;
        }

        rootCanvasGroup.alpha = toAlpha;
        fadeCoroutine = null;

        if (hideWhenFinished)
        {
            CompleteHide();
        }
    }

    private void CompleteHide()
    {
        RemoveOverlayCloseListeners();

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }

        if (root != null)
        {
            root.SetActive(false);
        }

        Hidden?.Invoke();
    }

    private void ShowRewardItems(int coins, int stars)
    {
        ResolveRewardsOverlayReferences();

        if (rewardsOverlayWindow == null)
        {
            return;
        }

        rewardsOverlayWindow.SetActive(true);
        EnsureSpriteRenderersVisibleInUI();
        ClearRewardItems();

        if (rewardItemPrefab == null || rewardsContainer == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' cannot show reward items because overlay references are missing.", this);
            return;
        }

        if (coins > 0)
        {
            CreateRewardItem(coinsIcon, coins);
        }

        if (stars > 0)
        {
            CreateRewardItem(starsIcon, stars);
        }
    }

    private void CreateRewardItem(Sprite icon, int amount)
    {
        if (amount <= 0 || rewardItemPrefab == null || rewardsContainer == null)
        {
            return;
        }

        var rewardItem = Instantiate(rewardItemPrefab, rewardsContainer);
        rewardItem.Set(icon, amount);
        rewardItem.PlayAppearEffect();
    }

    private void ClearRewardItems()
    {
        if (rewardsContainer == null)
        {
            return;
        }

        for (var i = rewardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(rewardsContainer.GetChild(i).gameObject);
        }
    }

    private void EnsureSpriteRenderersVisibleInUI()
    {
        if (rewardsOverlayWindow == null)
        {
            return;
        }

        var spriteRenderers = rewardsOverlayWindow.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            var spriteRenderer = spriteRenderers[i];
            var spriteRectTransform = spriteRenderer != null ? spriteRenderer.GetComponent<RectTransform>() : null;
            if (spriteRenderer == null || spriteRenderer.sprite == null || spriteRectTransform == null || IsGeneratedSpriteShine(spriteRenderer.gameObject))
            {
                continue;
            }

            spriteRectTransform.localScale = Vector3.one;
            var localPosition = spriteRectTransform.localPosition;
            localPosition.z = 0f;
            spriteRectTransform.localPosition = localPosition;

            var image = spriteRenderer.GetComponent<Image>();
            if (image == null)
            {
                image = spriteRenderer.gameObject.AddComponent<Image>();
            }

            image.sprite = spriteRenderer.sprite;
            image.color = spriteRenderer.color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = true;

            EnsureUiShineSweep(image);
        }
    }

    private static void EnsureUiShineSweep(Image image)
    {
        if (image == null || image.sprite == null)
        {
            return;
        }

        var shineSweep = image.GetComponent<IconEventShineSweep>();
        if (shineSweep == null)
        {
            shineSweep = image.gameObject.AddComponent<IconEventShineSweep>();
        }

        shineSweep.Configure(new Vector2(0.2f, 0.45f), new Vector2(2.5f, 3.5f), 72f, 0.38f);
        shineSweep.RestartSweep(UnityEngine.Random.Range(0.2f, 0.45f));
    }

    private static bool IsGeneratedSpriteShine(GameObject target)
    {
        return target != null && target.name.IndexOf(GeneratedSpriteShineName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ResolveRewardsOverlayReferences()
    {
        if (rewardsOverlayWindow == null)
        {
            var overlayTransform = FindDeepChild(transform, RewardsOverlayWindowName);
            rewardsOverlayWindow = overlayTransform != null ? overlayTransform.gameObject : null;
        }

        if (rewardsContainer == null && rewardsOverlayWindow != null)
        {
            var containerTransform = FindDeepChild(rewardsOverlayWindow.transform, RewardsContainerName);
            rewardsContainer = containerTransform;
        }
    }

    private void AddOverlayCloseListeners()
    {
        ResolveRewardsOverlayReferences();

        if (rewardsOverlayWindow == null)
        {
            return;
        }

        RemoveOverlayCloseListeners();
        overlayCloseButtons = rewardsOverlayWindow.GetComponentsInChildren<Button>(true);

        for (var i = 0; i < overlayCloseButtons.Length; i++)
        {
            if (overlayCloseButtons[i] != null)
            {
                overlayCloseButtons[i].onClick.AddListener(Hide);
            }
        }
    }

    private void RemoveOverlayCloseListeners()
    {
        for (var i = 0; i < overlayCloseButtons.Length; i++)
        {
            if (overlayCloseButtons[i] != null)
            {
                overlayCloseButtons[i].onClick.RemoveListener(Hide);
            }
        }

        overlayCloseButtons = Array.Empty<Button>();
    }

    private void ValidateReferences()
    {
        if (titleText == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(titleText)}.", this);
        }

        if (coinsText == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(coinsText)}.", this);
        }

        if (starsText == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(starsText)}.", this);
        }

        if (claimButton == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(claimButton)}.", this);
        }
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            var nestedChild = FindDeepChild(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        var canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }
}
