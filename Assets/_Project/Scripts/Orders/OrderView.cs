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

    private OrderManager orderManager;
    private OrderData order;

    private void Awake()
    {
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
            amountText.text = $"{order.CurrentAmount}/{order.RequiredAmount}";
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
}
