using System;
using UnityEngine;

[Serializable]
public sealed class OrderDefinition
{
    [Header("Requirement")]
    [SerializeField] private MergeItemData requiredItem;
    [SerializeField] private int requiredAmount = 1;

    [Header("Reward")]
    [SerializeField] private int coinReward;
    [SerializeField] private int starReward;

    public MergeItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;
    public int CoinReward => coinReward;
    public int StarReward => starReward;

    public OrderDefinition()
    {
    }

    public OrderDefinition(MergeItemData requiredItem, int requiredAmount, int coinReward, int starReward)
    {
        this.requiredItem = requiredItem;
        this.requiredAmount = Mathf.Max(1, requiredAmount);
        this.coinReward = Mathf.Max(0, coinReward);
        this.starReward = Mathf.Max(0, starReward);
    }
}
