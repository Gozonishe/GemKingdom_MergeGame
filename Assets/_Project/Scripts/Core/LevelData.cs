using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpawnableItemDefinition
{
    [SerializeField] private MergeItemData itemData;
    [Min(0)]
    [SerializeField] private int weight = 1;

    public MergeItemData ItemData => itemData;
    public int Weight => Mathf.Max(0, weight);

    public SpawnableItemDefinition()
    {
    }

    public SpawnableItemDefinition(MergeItemData itemData, int weight)
    {
        this.itemData = itemData;
        this.weight = Mathf.Max(0, weight);
    }
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Merge-2 Puzzle/Levels/Level Data")]
public sealed class LevelData : ScriptableObject
{
    [Header("Level")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private int movesCount = 100;

    [Header("Board Balance")]
    [SerializeField] private List<SpawnableItemDefinition> weightedSpawnableItems = new List<SpawnableItemDefinition>();
    [HideInInspector]
    [SerializeField] private List<MergeItemData> spawnableItems = new List<MergeItemData>();

    [Header("Orders")]
    [SerializeField] private List<OrderDefinition> orders = new List<OrderDefinition>();

    public int LevelNumber => levelNumber;
    public int MovesCount => movesCount;
    public IReadOnlyList<SpawnableItemDefinition> WeightedSpawnableItems => weightedSpawnableItems;
    public IReadOnlyList<MergeItemData> SpawnableItems => spawnableItems;
    public IReadOnlyList<OrderDefinition> Orders => orders;
    public bool HasWeightedSpawnableItems => weightedSpawnableItems != null && weightedSpawnableItems.Count > 0;

    private void OnValidate()
    {
        if (weightedSpawnableItems == null)
        {
            weightedSpawnableItems = new List<SpawnableItemDefinition>();
        }

        if (weightedSpawnableItems.Count > 0 || spawnableItems == null || spawnableItems.Count == 0)
        {
            return;
        }

        for (var i = 0; i < spawnableItems.Count; i++)
        {
            var itemData = spawnableItems[i];
            if (itemData != null)
            {
                weightedSpawnableItems.Add(new SpawnableItemDefinition(itemData, 1));
            }
        }
    }
}
