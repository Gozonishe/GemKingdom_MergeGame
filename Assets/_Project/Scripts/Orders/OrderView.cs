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
    [SerializeField] private RectTransform claimContent;

    private OrderManager orderManager;
    private OrderData order;

    private void Awake()
    {
        ValidateReferences();
        ApplyDefaultLayout();
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

    public void Bind(OrderManager manager, OrderData orderData)
    {
        orderManager = manager;
        order = orderData;
        Refresh(order);
    }

    public void Refresh(OrderData orderData)
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
            rewardText.text = $"{order.CoinReward} coins  {order.StarReward} stars";
        }

        if (claimButton != null)
        {
            claimButton.interactable = order.CanClaim;
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
        if (requiredItemIcon == null)
        {
            Debug.LogError($"{nameof(OrderView)} on '{name}' is missing {nameof(requiredItemIcon)}.", this);
        }

        if (amountText == null)
        {
            Debug.LogError($"{nameof(OrderView)} on '{name}' is missing {nameof(amountText)}.", this);
        }

        if (rewardText == null)
        {
            Debug.LogError($"{nameof(OrderView)} on '{name}' is missing {nameof(rewardText)}.", this);
        }

        if (claimButton == null)
        {
            Debug.LogError($"{nameof(OrderView)} on '{name}' is missing {nameof(claimButton)}.", this);
        }
    }

    [ContextMenu("Apply Default Layout")]
    private void ApplyDefaultLayout()
    {
        topContent = EnsureChildRect(topContent, "TopContent", 0);
        claimContent = EnsureChildRect(claimContent, "ClaimContent", 1);

        SetStretch(topContent, new Vector2(0f, 0.44f), Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -8f));
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
            rewardRect.SetParent(topContent, false);
            SetStretch(rewardRect, new Vector2(0.36f, 0f), new Vector2(1f, 0.5f), new Vector2(6f, 4f), new Vector2(-8f, 0f));
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
}
