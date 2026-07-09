using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OrderView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image requiredItemIcon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button claimButton;

    [Header("Layout")]
    [SerializeField] private RectTransform topContent;
    [SerializeField] private RectTransform rewardInfo;
    [SerializeField] private RectTransform claimContent;

    private OrderManager orderManager;
    private OrderRuntimeData order;

    private void Awake()
    {
        ResolveReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(Claim);
        }
    }

    private void OnDisable()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(Claim);
        }
    }

    public void Bind(OrderManager manager, OrderRuntimeData orderData)
    {
        orderManager = manager;
        order = orderData;
        Refresh(order);
    }

    public void Refresh(OrderRuntimeData orderData)
    {
        order = orderData;

        if (order == null || order.IsClaimed)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (requiredItemIcon != null)
        {
            requiredItemIcon.sprite = order.RequiredItem != null ? order.RequiredItem.Icon : null;
            requiredItemIcon.enabled = requiredItemIcon.sprite != null;
        }

        if (amountText != null)
        {
            var displayedAmount = Mathf.Min(order.CurrentAmount, order.RequiredAmount);
            amountText.text = $"{displayedAmount}/{order.RequiredAmount}";
        }

        if (rewardText != null)
        {
            rewardText.text = string.Empty;
        }

        if (rewardInfo != null)
        {
            rewardInfo.gameObject.SetActive(false);
        }

        if (claimButton != null)
        {
            var canClaim = order.CanClaim;
            claimButton.gameObject.SetActive(canClaim);
            claimButton.interactable = canClaim;
        }
    }

    private void Claim()
    {
        if (orderManager != null && order != null)
        {
            orderManager.ClaimOrder(order);
        }
    }

    private void ValidateReferences()
    {
        ResolveReferences();

        if (requiredItemIcon == null)
        {
            Debug.LogError($"{nameof(OrderView)} on '{name}' is missing {nameof(requiredItemIcon)}.", this);
        }

        if (amountText == null)
        {
            Debug.LogError($"{nameof(OrderView)} on '{name}' is missing {nameof(amountText)}.", this);
        }

        if (claimButton == null)
        {
            Debug.LogError($"{nameof(OrderView)} on '{name}' is missing {nameof(claimButton)}.", this);
        }
    }

    private void ResolveReferences()
    {
        topContent = topContent != null ? topContent : FindRectTransform("TopContent");
        rewardInfo = rewardInfo != null ? rewardInfo : FindRectTransform("RewardInfo");
        claimContent = claimContent != null ? claimContent : FindRectTransform("ClaimContent");

        requiredItemIcon = requiredItemIcon != null
            ? requiredItemIcon
            : FindComponentInChild<Image>("TopContent/RequiredItemIcon");

        amountText = amountText != null
            ? amountText
            : FindComponentInChild<TMP_Text>("TopContent/AmountText");

        rewardText = rewardText != null
            ? rewardText
            : FindComponentInChild<TMP_Text>("RewardInfo/RewardText");

        claimButton = claimButton != null
            ? claimButton
            : FindComponentInChild<Button>("ClaimButton");
    }

    [ContextMenu("Apply Default Layout")]
    private void ApplyDefaultLayout()
    {
        topContent = EnsureChildRect(topContent, "TopContent", 0);
        rewardInfo = EnsureChildRect(rewardInfo, "RewardInfo", 1);
        claimContent = EnsureChildRect(claimContent, "ClaimContent", 2);

        SetStretch(topContent, new Vector2(0f, 0.5f), Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -8f));
        SetStretch(rewardInfo, new Vector2(0f, 0.34f), new Vector2(1f, 0.5f), new Vector2(8f, 2f), new Vector2(-8f, -2f));
        SetStretch(claimContent, Vector2.zero, new Vector2(1f, 0.44f), new Vector2(8f, 8f), new Vector2(-8f, -4f));

        if (requiredItemIcon != null)
        {
            var iconRect = requiredItemIcon.rectTransform;
            iconRect.SetParent(topContent, false);
            SetStretch(iconRect, Vector2.zero, new Vector2(0.36f, 1f), new Vector2(8f, 8f), new Vector2(-6f, -8f));
            requiredItemIcon.preserveAspect = true;
        }

        if (amountText != null)
        {
            var amountRect = amountText.rectTransform;
            amountRect.SetParent(topContent, false);
            SetStretch(amountRect, new Vector2(0.36f, 0.5f), Vector2.one, new Vector2(6f, 0f), new Vector2(-8f, -4f));
            amountText.alignment = TextAlignmentOptions.Center;
        }

        if (rewardText != null)
        {
            var rewardRect = rewardText.rectTransform;
            rewardRect.SetParent(rewardInfo, false);
            SetStretch(rewardRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rewardText.alignment = TextAlignmentOptions.Center;
        }

        if (claimButton != null)
        {
            var buttonRect = claimButton.transform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.SetParent(claimContent, false);
                SetStretch(buttonRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }
    }

    private RectTransform EnsureChildRect(RectTransform current, string childName, int siblingIndex)
    {
        if (current != null)
        {
            current.SetParent(transform, false);
            current.SetSiblingIndex(siblingIndex);
            return current;
        }

        var existing = transform.Find(childName);
        if (existing != null && existing is RectTransform existingRect)
        {
            existingRect.SetSiblingIndex(siblingIndex);
            return existingRect;
        }

        var child = new GameObject(childName, typeof(RectTransform));
        var rect = child.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.SetSiblingIndex(siblingIndex);
        return rect;
    }

    private static void SetStretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private RectTransform FindRectTransform(string childPath)
    {
        var child = transform.Find(childPath);
        return child as RectTransform;
    }

    private T FindComponentInChild<T>(string childPath) where T : Component
    {
        var child = transform.Find(childPath);
        if (child != null && child.TryGetComponent<T>(out var component))
        {
            return component;
        }

        return GetComponentInChildren<T>(true);
    }
}
