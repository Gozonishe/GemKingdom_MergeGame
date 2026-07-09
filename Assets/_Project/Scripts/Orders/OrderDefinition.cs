using System;
using UnityEngine;

public enum OrderObjectiveType
{
    CollectOnBoard,
    DestroyItems
}

[Serializable]
public sealed class OrderDefinition
{
    [Header("Requirement")]
    [SerializeField] private OrderObjectiveType objectiveType = OrderObjectiveType.CollectOnBoard;
    [SerializeField] private MergeItemData requiredItem;
    [SerializeField] private int requiredAmount = 1;

    public OrderObjectiveType ObjectiveType => objectiveType;
    public MergeItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;

    public OrderDefinition()
    {
    }

    public OrderDefinition(MergeItemData requiredItem, int requiredAmount)
        : this(OrderObjectiveType.CollectOnBoard, requiredItem, requiredAmount)
    {
    }

    public OrderDefinition(OrderObjectiveType objectiveType, MergeItemData requiredItem, int requiredAmount)
    {
        this.objectiveType = objectiveType;
        this.requiredItem = requiredItem;
        this.requiredAmount = Mathf.Max(1, requiredAmount);
    }
}
