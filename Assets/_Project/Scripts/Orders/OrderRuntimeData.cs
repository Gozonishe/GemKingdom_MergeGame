using System;
using UnityEngine;

[Serializable]
public sealed class OrderRuntimeData
{
    [SerializeField] private OrderDefinition definition;
    [SerializeField] private int currentAmount;
    [SerializeField] private bool isClaimed;

    public OrderDefinition Definition => definition;
    public OrderObjectiveType ObjectiveType => definition != null ? definition.ObjectiveType : OrderObjectiveType.CollectOnBoard;
    public MergeItemData RequiredItem => definition != null ? definition.RequiredItem : null;
    public int RequiredAmount => definition != null ? definition.RequiredAmount : 0;
    public int CurrentAmount => currentAmount;
    public bool IsClaimed => isClaimed;
    public bool CanClaim => !isClaimed && RequiredAmount > 0 && currentAmount >= RequiredAmount;

    public OrderRuntimeData(OrderDefinition definition)
    {
        this.definition = definition;
    }

    public void SetCurrentAmount(int amount)
    {
        currentAmount = Mathf.Max(0, amount);
    }

    public void MarkClaimed()
    {
        isClaimed = true;
    }
}
