using System.Collections.Generic;
using UnityEngine;

public sealed class OrderManager : MonoBehaviour
{
    [Header("Orders")]
    [SerializeField] private List<OrderData> activeOrders = new List<OrderData>();

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private RewardPopup rewardPopup;
    [SerializeField] private List<OrderView> orderViews = new List<OrderView>();

    public IReadOnlyList<OrderData> ActiveOrders => activeOrders;

    private void Start()
    {
        ResolveReferences();
        BindViews();
        RefreshOrders();
    }

    public void RefreshOrders()
    {
        if (boardManager == null)
        {
            ResolveReferences();
        }

        for (var i = 0; i < activeOrders.Count; i++)
        {
            var order = activeOrders[i];
            if (order == null || order.IsClaimed)
            {
                continue;
            }

            order.SetCurrentAmount(CountItemsOnBoard(order.RequiredItem));
        }

        RefreshViews();
    }

    public bool ClaimOrder(OrderData order)
    {
        if (order == null || !order.CanClaim)
        {
            return false;
        }

        ResolveReferences();

        if (boardManager == null)
        {
            Debug.LogWarning($"{nameof(OrderManager)} cannot claim order because {nameof(boardManager)} is not assigned.", this);
            return false;
        }

        if (currencyManager == null)
        {
            Debug.LogError($"{nameof(OrderManager)} on '{name}' cannot claim order because {nameof(currencyManager)} is not assigned.", this);
            return false;
        }

        if (rewardPopup == null)
        {
            Debug.LogError($"{nameof(OrderManager)} on '{name}' will claim the order without showing rewards because {nameof(rewardPopup)} is not assigned.", this);
        }

        // Orders claim: remove required items from the board first, then pay rewards and refresh board/UI.
        var removedCount = RemoveRequiredItems(order.RequiredItem, order.RequiredAmount);
        if (removedCount < order.RequiredAmount)
        {
            RefreshOrders();
            return false;
        }

        currencyManager.AddReward(order.CoinReward, order.StarReward);

        if (rewardPopup != null)
        {
            rewardPopup.Show(order.CoinReward, order.StarReward);
        }

        boardManager.CollapseColumns();
        boardManager.RefillBoard();
        order.MarkClaimed();
        RefreshOrders();
        return true;
    }

    public bool CompleteRandomOrderDebug()
    {
        var availableOrders = new List<OrderData>();

        for (var i = 0; i < activeOrders.Count; i++)
        {
            var order = activeOrders[i];
            if (order != null && !order.IsClaimed)
            {
                availableOrders.Add(order);
            }
        }

        if (availableOrders.Count == 0)
        {
            Debug.Log($"{nameof(OrderManager)}: no active orders to complete.", this);
            return false;
        }

        ResolveReferences();

        var selectedOrder = availableOrders[Random.Range(0, availableOrders.Count)];
        selectedOrder.SetCurrentAmount(selectedOrder.RequiredAmount);

        if (currencyManager != null)
        {
            currencyManager.AddReward(selectedOrder.CoinReward, selectedOrder.StarReward);
        }

        if (rewardPopup != null)
        {
            rewardPopup.Show(selectedOrder.CoinReward, selectedOrder.StarReward);
        }

        selectedOrder.MarkClaimed();
        RefreshOrders();
        return true;
    }

    private void BindViews()
    {
        for (var i = 0; i < orderViews.Count; i++)
        {
            var view = orderViews[i];
            var order = i < activeOrders.Count ? activeOrders[i] : null;

            if (view != null)
            {
                view.Bind(this, order);
            }
        }
    }

    private void RefreshViews()
    {
        for (var i = 0; i < orderViews.Count; i++)
        {
            var view = orderViews[i];
            var order = i < activeOrders.Count ? activeOrders[i] : null;

            if (view != null)
            {
                view.Refresh(order);
            }
        }
    }

    private int CountItemsOnBoard(MergeItemData requiredItem)
    {
        if (requiredItem == null || boardManager == null || boardManager.Cells == null)
        {
            return 0;
        }

        var count = 0;

        for (var y = 0; y < boardManager.Rows; y++)
        {
            for (var x = 0; x < boardManager.Columns; x++)
            {
                var item = boardManager.GetItemAt(x, y);
                if (item != null && item.Data == requiredItem)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private int RemoveRequiredItems(MergeItemData requiredItem, int requiredAmount)
    {
        if (requiredItem == null || boardManager == null || boardManager.Cells == null || requiredAmount <= 0)
        {
            return 0;
        }

        var itemsToRemove = new List<MergeItem>(requiredAmount);

        for (var y = 0; y < boardManager.Rows && itemsToRemove.Count < requiredAmount; y++)
        {
            for (var x = 0; x < boardManager.Columns && itemsToRemove.Count < requiredAmount; x++)
            {
                var item = boardManager.GetItemAt(x, y);
                if (item == null || item.Data != requiredItem)
                {
                    continue;
                }

                itemsToRemove.Add(item);
            }
        }

        if (itemsToRemove.Count < requiredAmount)
        {
            return 0;
        }

        for (var i = 0; i < itemsToRemove.Count; i++)
        {
            var item = itemsToRemove[i];
            var cell = item != null ? item.CurrentCell : null;

            if (cell != null)
            {
                cell.Clear();
            }

            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        return itemsToRemove.Count;
    }

    private void ResolveReferences()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        if (currencyManager == null)
        {
            currencyManager = FindFirstObjectByType<CurrencyManager>();
        }

        if (rewardPopup == null)
        {
            rewardPopup = FindFirstObjectByType<RewardPopup>(FindObjectsInactive.Include);
        }
    }
}
