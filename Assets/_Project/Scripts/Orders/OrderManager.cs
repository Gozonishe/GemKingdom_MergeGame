using System.Collections.Generic;
using UnityEngine;

public sealed class OrderManager : MonoBehaviour
{
    [Header("Order Definitions")]
    [SerializeField] private List<OrderDefinition> orderDefinitions = new List<OrderDefinition>();

    [Header("Legacy Orders")]
    [SerializeField] private List<OrderData> activeOrders = new List<OrderData>();

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private RewardPopup rewardPopup;
    [SerializeField] private List<OrderView> orderViews = new List<OrderView>();

    private readonly List<OrderRuntimeData> runtimeOrders = new List<OrderRuntimeData>();

    public IReadOnlyList<OrderRuntimeData> ActiveOrders => runtimeOrders;
    public bool AreAllOrdersClaimed => runtimeOrders.Count > 0 && AreEveryRuntimeOrderClaimed();

    private void Start()
    {
        ResolveReferences();
        BuildRuntimeOrdersIfNeeded();
        BindViews();
        RefreshOrders();
    }

    public void SetOrders(IReadOnlyList<OrderDefinition> definitions)
    {
        orderDefinitions.Clear();

        if (definitions != null)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null)
                {
                    orderDefinitions.Add(definitions[i]);
                }
            }
        }

        BuildRuntimeOrdersFromDefinitions(orderDefinitions);
        BindViews();
        RefreshOrders();
    }

    public void RefreshOrders()
    {
        ResolveReferences();
        BuildRuntimeOrdersIfNeeded();

        for (var i = 0; i < runtimeOrders.Count; i++)
        {
            var order = runtimeOrders[i];
            if (order == null || order.IsClaimed)
            {
                continue;
            }

            if (order.ObjectiveType == OrderObjectiveType.CollectOnBoard)
            {
                order.SetCurrentAmount(CountItemsOnBoard(order.RequiredItem));
            }
        }

        RefreshViews();
    }

    public bool ClaimOrder(OrderRuntimeData order)
    {
        if (order == null || !order.CanClaim)
        {
            return false;
        }

        ResolveReferences();

        if (order.ObjectiveType == OrderObjectiveType.CollectOnBoard && boardManager == null)
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

        var shouldRefreshBoard = false;

        // Orders claim: collect orders consume the required amount, destroy orders only pay rewards after gameplay progress.
        if (order.ObjectiveType == OrderObjectiveType.CollectOnBoard)
        {
            var removedCount = RemoveRequiredItems(order.RequiredItem, order.RequiredAmount);
            if (removedCount < order.RequiredAmount)
            {
                RefreshOrders();
                return false;
            }

            shouldRefreshBoard = removedCount > 0;
        }

        currencyManager.AddReward(order.CoinReward, order.StarReward);

        if (rewardPopup != null)
        {
            rewardPopup.Show(order.CoinReward, order.StarReward);
        }

        if (shouldRefreshBoard)
        {
            boardManager.CollapseColumns();
            boardManager.RefillBoard();
        }

        order.MarkClaimed();
        RefreshOrders();
        return true;
    }

    public void RegisterItemDestroyed(MergeItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0)
        {
            return;
        }

        BuildRuntimeOrdersIfNeeded();

        for (var i = 0; i < runtimeOrders.Count; i++)
        {
            var order = runtimeOrders[i];
            if (order == null
                || order.IsClaimed
                || order.ObjectiveType != OrderObjectiveType.DestroyItems
                || !DoesDestroyedItemMatchRequirement(itemData, order.RequiredItem))
            {
                continue;
            }

            order.SetCurrentAmount(order.CurrentAmount + amount);
        }

        RefreshViews();
    }

    private static bool DoesDestroyedItemMatchRequirement(MergeItemData destroyedItem, MergeItemData requiredItem)
    {
        if (destroyedItem == null || requiredItem == null)
        {
            return false;
        }

        return destroyedItem == requiredItem
            || IsLinkedThroughNextLevel(destroyedItem, requiredItem)
            || IsLinkedThroughNextLevel(requiredItem, destroyedItem);
    }

    private static bool IsLinkedThroughNextLevel(MergeItemData startItem, MergeItemData targetItem)
    {
        var currentItem = startItem != null ? startItem.NextLevelItem : null;
        var guard = 0;

        while (currentItem != null && guard < 64)
        {
            if (currentItem == targetItem)
            {
                return true;
            }

            currentItem = currentItem.NextLevelItem;
            guard++;
        }

        return false;
    }

    public bool CompleteRandomOrderDebug()
    {
        BuildRuntimeOrdersIfNeeded();

        var availableOrders = new List<OrderRuntimeData>();

        for (var i = 0; i < runtimeOrders.Count; i++)
        {
            var order = runtimeOrders[i];
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

    private void BuildRuntimeOrdersIfNeeded()
    {
        if (runtimeOrders.Count > 0)
        {
            return;
        }

        if (orderDefinitions.Count > 0)
        {
            BuildRuntimeOrdersFromDefinitions(orderDefinitions);
            return;
        }

        BuildRuntimeOrdersFromLegacyOrders();
    }

    private void BuildRuntimeOrdersFromDefinitions(IReadOnlyList<OrderDefinition> definitions)
    {
        runtimeOrders.Clear();

        if (definitions == null)
        {
            return;
        }

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (definition != null)
            {
                runtimeOrders.Add(new OrderRuntimeData(definition));
            }
        }
    }

    private void BuildRuntimeOrdersFromLegacyOrders()
    {
        runtimeOrders.Clear();

        for (var i = 0; i < activeOrders.Count; i++)
        {
            var legacyOrder = activeOrders[i];
            if (legacyOrder == null)
            {
                continue;
            }

            var definition = new OrderDefinition(
                legacyOrder.RequiredItem,
                legacyOrder.RequiredAmount,
                legacyOrder.CoinReward,
                legacyOrder.StarReward);

            var runtimeOrder = new OrderRuntimeData(definition);
            runtimeOrder.SetCurrentAmount(legacyOrder.CurrentAmount);

            if (legacyOrder.IsClaimed)
            {
                runtimeOrder.MarkClaimed();
            }

            runtimeOrders.Add(runtimeOrder);
        }
    }

    private void BindViews()
    {
        for (var i = 0; i < orderViews.Count; i++)
        {
            var view = orderViews[i];
            var order = i < runtimeOrders.Count ? runtimeOrders[i] : null;

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
            var order = i < runtimeOrders.Count ? runtimeOrders[i] : null;

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

    private bool AreEveryRuntimeOrderClaimed()
    {
        for (var i = 0; i < runtimeOrders.Count; i++)
        {
            var order = runtimeOrders[i];
            if (order == null || !order.IsClaimed)
            {
                return false;
            }
        }

        return true;
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
