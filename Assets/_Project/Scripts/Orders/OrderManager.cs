using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class OrderManager : MonoBehaviour
{
    [Header("Order Definitions")]
    [SerializeField] private List<OrderDefinition> orderDefinitions = new List<OrderDefinition>();

    [Header("Legacy Orders")]
    [SerializeField] private List<OrderData> activeOrders = new List<OrderData>();

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Transform orderViewsRoot;
    [SerializeField] private OrderView orderViewPrefab;
    [SerializeField] private List<OrderView> orderViews = new List<OrderView>();
    [SerializeField] private AdaptiveHorizontalScrollLayout adaptiveHorizontalLayout;

    [Header("Auto Claim")]
    [FormerlySerializedAs("autoRewardPopupDelay")]
    [SerializeField, Min(0f)] private float autoClaimDelay = 1f;

    private readonly List<OrderRuntimeData> runtimeOrders = new List<OrderRuntimeData>();
    private bool isAutoClaimingCompletedOrders;

    public IReadOnlyList<OrderRuntimeData> ActiveOrders => runtimeOrders;
    public bool AreAllOrdersClaimed => runtimeOrders.Count > 0 && AreEveryRuntimeOrderClaimed();
    public bool AreAllOrdersReadyOrClaimed => runtimeOrders.Count > 0 && AreEveryRuntimeOrderReadyOrClaimed();
    public bool IsAutoClaimingCompletedOrders => isAutoClaimingCompletedOrders;

    public bool CanCreateMergeResult(MergeItemData sourceItem, MergeItemData resultItem)
    {
        if (sourceItem == null || resultItem == null || !AreItemsInSameChain(sourceItem, resultItem))
        {
            return false;
        }

        var maxRequiredLevel = GetMaxRequiredLevelForItemFamily(sourceItem);
        return maxRequiredLevel < 0 || resultItem.Level <= maxRequiredLevel;
    }

    public int GetMaxRequiredLevelForItemFamily(MergeItemData itemData)
    {
        if (itemData == null)
        {
            return -1;
        }

        BuildRuntimeOrdersIfNeeded();

        var maxLevel = -1;
        for (var i = 0; i < runtimeOrders.Count; i++)
        {
            var order = runtimeOrders[i];
            var requiredItem = order != null && !order.IsClaimed ? order.RequiredItem : null;

            if (requiredItem != null && AreItemsInSameChain(itemData, requiredItem))
            {
                maxLevel = Mathf.Max(maxLevel, requiredItem.Level);
            }
        }

        return maxLevel;
    }

    private void Start()
    {
        ResolveReferences();
        BuildRuntimeOrdersIfNeeded();
        EnsureOrderViews();
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
        EnsureOrderViews();
        BindViews();
        RefreshOrders(false);
    }

    public void RefreshOrders()
    {
        RefreshOrders(true);
    }

    private void RefreshOrders(bool allowAutoClaim)
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

        if (allowAutoClaim)
        {
            TryAutoClaimAllCompletedOrders();
        }

        RefreshAdaptiveOrderLayout();
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

        // Collect orders consume the required amount, destroy orders are completed by gameplay progress.
        // In the new board flow removed collect items leave empty cells; refill is reserved for debug tools.
        if (order.ObjectiveType == OrderObjectiveType.CollectOnBoard)
        {
            var removedCount = RemoveRequiredItems(order.RequiredItem, order.RequiredAmount);
            if (removedCount < order.RequiredAmount)
            {
                RefreshOrders();
                return false;
            }
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
                || !AreItemsInSameChain(itemData, order.RequiredItem))
            {
                continue;
            }

            order.SetCurrentAmount(order.CurrentAmount + amount);
        }

        RefreshViews();
        TryAutoClaimAllCompletedOrders();
    }

    private static bool AreItemsInSameChain(MergeItemData firstItem, MergeItemData secondItem)
    {
        if (firstItem == null || secondItem == null)
        {
            return false;
        }

        return firstItem == secondItem
            || IsLinkedThroughNextLevel(firstItem, secondItem)
            || IsLinkedThroughNextLevel(secondItem, firstItem);
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

        var selectedOrder = availableOrders[Random.Range(0, availableOrders.Count)];
        selectedOrder.SetCurrentAmount(selectedOrder.RequiredAmount);
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
                legacyOrder.RequiredAmount);

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
        EnsureOrderViews();

        for (var i = 0; i < orderViews.Count; i++)
        {
            var view = orderViews[i];
            var order = i < runtimeOrders.Count ? runtimeOrders[i] : null;

            if (view != null)
            {
                view.Bind(this, order);
            }
        }

        RefreshAdaptiveOrderLayout();
    }

    private void RefreshViews()
    {
        EnsureOrderViews();

        for (var i = 0; i < orderViews.Count; i++)
        {
            var view = orderViews[i];
            var order = i < runtimeOrders.Count ? runtimeOrders[i] : null;

            if (view != null)
            {
                view.Refresh(order);
            }
        }

        RefreshAdaptiveOrderLayout();
    }

    private void EnsureOrderViews()
    {
        ResolveOrderViewsRoot();
        RemoveNullOrderViews();

        var requiredCount = runtimeOrders.Count;
        for (var i = orderViews.Count; i < requiredCount; i++)
        {
            var view = CreateOrderView(i);
            if (view == null)
            {
                break;
            }

            orderViews.Add(view);
        }

        for (var i = 0; i < orderViews.Count; i++)
        {
            var view = orderViews[i];
            if (view == null)
            {
                continue;
            }

            if (orderViewsRoot != null && view.transform.parent != orderViewsRoot)
            {
                view.transform.SetParent(orderViewsRoot, false);
            }

            view.transform.SetSiblingIndex(i);
            view.name = $"OrderView_{i + 1}";

            if (i >= requiredCount)
            {
                view.Bind(this, null);
            }
        }
    }

    private OrderView CreateOrderView(int index)
    {
        var prefab = orderViewPrefab != null ? orderViewPrefab : GetFallbackOrderViewTemplate();
        if (prefab == null)
        {
            Debug.LogError($"{nameof(OrderManager)} on '{name}' cannot create OrderView_{index + 1} because no {nameof(orderViewPrefab)} is assigned.", this);
            return null;
        }

        var parent = orderViewsRoot != null ? orderViewsRoot : transform;
        var view = Instantiate(prefab, parent);
        view.name = $"OrderView_{index + 1}";
        return view;
    }

    private OrderView GetFallbackOrderViewTemplate()
    {
        for (var i = 0; i < orderViews.Count; i++)
        {
            if (orderViews[i] != null)
            {
                return orderViews[i];
            }
        }

        return GetComponentInChildren<OrderView>(true);
    }

    private void RemoveNullOrderViews()
    {
        for (var i = orderViews.Count - 1; i >= 0; i--)
        {
            if (orderViews[i] == null)
            {
                orderViews.RemoveAt(i);
            }
        }
    }

    private void ResolveOrderViewsRoot()
    {
        if (orderViewsRoot == null)
        {
            orderViewsRoot = transform;
        }
    }

    private void RefreshAdaptiveOrderLayout()
    {
        ResolveAdaptiveHorizontalLayout();

        if (adaptiveHorizontalLayout != null)
        {
            adaptiveHorizontalLayout.RefreshLayout();
            adaptiveHorizontalLayout.RefreshLayoutDelayed();
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

    private void TryAutoClaimAllCompletedOrders()
    {
        if (isAutoClaimingCompletedOrders || runtimeOrders.Count == 0 || !AreEveryRuntimeOrderReadyOrClaimed())
        {
            return;
        }

        isAutoClaimingCompletedOrders = true;
        StartCoroutine(AutoClaimAllCompletedOrdersAfterDelay());
    }

    private IEnumerator AutoClaimAllCompletedOrdersAfterDelay()
    {
        if (autoClaimDelay > 0f)
        {
            yield return new WaitForSeconds(autoClaimDelay);
        }

        if (!AreEveryRuntimeOrderReadyOrClaimed())
        {
            isAutoClaimingCompletedOrders = false;
            yield break;
        }

        for (var i = 0; i < runtimeOrders.Count; i++)
        {
            var order = runtimeOrders[i];
            if (order == null || order.IsClaimed)
            {
                continue;
            }

            order.MarkClaimed();
        }

        isAutoClaimingCompletedOrders = false;
        RefreshViews();
    }

    private bool AreEveryRuntimeOrderReadyOrClaimed()
    {
        for (var i = 0; i < runtimeOrders.Count; i++)
        {
            var order = runtimeOrders[i];
            if (order == null || (!order.IsClaimed && !order.CanClaim))
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

        ResolveOrderViewsRoot();
        ResolveAdaptiveHorizontalLayout();
    }

    private void ResolveAdaptiveHorizontalLayout()
    {
        if (adaptiveHorizontalLayout != null)
        {
            return;
        }

        adaptiveHorizontalLayout = GetComponent<AdaptiveHorizontalScrollLayout>();
        if (adaptiveHorizontalLayout == null)
        {
            adaptiveHorizontalLayout = GetComponentInChildren<AdaptiveHorizontalScrollLayout>(true);
        }

        if (adaptiveHorizontalLayout == null && orderViewsRoot != null)
        {
            adaptiveHorizontalLayout = orderViewsRoot.GetComponentInParent<AdaptiveHorizontalScrollLayout>();
        }
    }
}
