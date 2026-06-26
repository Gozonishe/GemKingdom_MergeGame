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

    [Header("Reward")]
    [SerializeField] private int coinReward;
    [SerializeField] private int starReward;

    public OrderObjectiveType ObjectiveType => objectiveType;
    public MergeItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;
    public int CoinReward => coinReward;
    public int StarReward => starReward;

    public OrderDefinition()
    {
    }

    public OrderDefinition(MergeItemData requiredItem, int requiredAmount, int coinReward, int starReward)
        : this(OrderObjectiveType.CollectOnBoard, requiredItem, requiredAmount, coinReward, starReward)
    {
    }

    public OrderDefinition(OrderObjectiveType objectiveType, MergeItemData requiredItem, int requiredAmount, int coinReward, int starReward)
    {
        this.objectiveType = objectiveType;
        this.requiredItem = requiredItem;
        this.requiredAmount = Mathf.Max(1, requiredAmount);
        this.coinReward = Mathf.Max(0, coinReward);
        this.starReward = Mathf.Max(0, starReward);
    }
}
