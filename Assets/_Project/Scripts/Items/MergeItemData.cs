using UnityEngine;

[CreateAssetMenu(fileName = "MergeItemData", menuName = "Merge-2 Puzzle/Items/Merge Item Data")]
public sealed class MergeItemData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private int level = 1;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;

    [Header("Merge Chain")]
    [SerializeField] private MergeItemData nextLevelItem;

    [Header("Adjacent Merge Reaction")]
    [SerializeField] private bool reactToAdjacentMerge;

    [Header("Special Merge")]
    [SerializeField] private bool destroyBothOnAnyNeighborMerge;

    [Header("Board Object")]
    [SerializeField] private bool isSpider;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public int Level => level;
    public Sprite Icon => icon;
    public MergeItemData NextLevelItem => nextLevelItem;
    public bool ReactToAdjacentMerge => reactToAdjacentMerge;
    public bool DestroyBothOnAnyNeighborMerge => destroyBothOnAnyNeighborMerge;
    public bool IsSpider => isSpider;
    public bool IsMaxLevel => nextLevelItem == null;
    public bool CanMergeToNextLevel => nextLevelItem != null;
}
