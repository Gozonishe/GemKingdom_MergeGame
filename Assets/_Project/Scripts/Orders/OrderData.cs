using System;
using UnityEngine;

[Serializable]
public sealed class OrderData
{
    [SerializeField] private MergeItemData requiredItem;
    [SerializeField] private int requiredAmount = 1;
    [SerializeField] private int currentAmount;
    [SerializeField] private bool isClaimed;

    public MergeItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;
    public int CurrentAmount => currentAmount;
    public bool IsClaimed => isClaimed;
    public bool CanClaim => !isClaimed && currentAmount >= requiredAmount;

    public void SetCurrentAmount(int amount)
    {
        currentAmount = Mathf.Max(0, amount);
    }

    public void MarkClaimed()
    {
        isClaimed = true;
    }
}
