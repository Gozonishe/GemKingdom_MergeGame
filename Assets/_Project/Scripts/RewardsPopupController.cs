using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardsPopupController : MonoBehaviour
{
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
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private RewardItemView rewardItemPrefab;
    [SerializeField] private RewardData[] rewards;
    [SerializeField] private float rewardFadeDuration = 0.25f;

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
        overlayRoot.SetActive(true);

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

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private void ClearRewards()
    {
        for (var i = rewardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(rewardsContainer.GetChild(i).gameObject);
        }
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
