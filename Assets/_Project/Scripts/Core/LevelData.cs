using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

[Serializable]
public sealed class InitialBoardItemDefinition
{
    [SerializeField] private MergeItemData itemData;
    [Min(0)]
    [SerializeField] private int count = 1;

    public MergeItemData ItemData => itemData;
    public int Count => Mathf.Max(0, count);

    public InitialBoardItemDefinition()
    {
    }

    public InitialBoardItemDefinition(MergeItemData itemData, int count)
    {
        this.itemData = itemData;
        this.count = Mathf.Max(0, count);
    }
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Merge-2 Puzzle/Levels/Level Data")]
public sealed class LevelData : ScriptableObject
{
    [Header("Level")]
    [SerializeField] private int levelNumber = 1;
    [Tooltip("For the new flow this value means how many times the player can tap the item generator button.")]
    [SerializeField] private int movesCount = 100;

    [Header("Board Size")]
    [Min(1)]
    [SerializeField] private int boardColumns = 6;
    [Min(1)]
    [SerializeField] private int boardRows = 6;

    [Header("Initial Board Fill")]
    [Tooltip("Items that will be placed on random free board cells when a level starts.")]
    [SerializeField] private List<InitialBoardItemDefinition> initialBoardItems = new List<InitialBoardItemDefinition>();

    [Header("Bottom Generator Balance")]
    [Tooltip("Items that can appear in the bottom generated-item slot after tapping the generator button.")]
    [FormerlySerializedAs("weightedSpawnableItems")]
    [SerializeField] private List<SpawnableItemDefinition> generatorSpawnableItems = new List<SpawnableItemDefinition>();
    [HideInInspector]
    [SerializeField] private List<MergeItemData> spawnableItems = new List<MergeItemData>();

    [Header("Orders")]
    [SerializeField] private List<OrderDefinition> orders = new List<OrderDefinition>();

    public int LevelNumber => levelNumber;
    public int MovesCount => movesCount;
    public int BoardColumns => Mathf.Max(1, boardColumns);
    public int BoardRows => Mathf.Max(1, boardRows);
    public IReadOnlyList<InitialBoardItemDefinition> InitialBoardItems => initialBoardItems;
    public IReadOnlyList<SpawnableItemDefinition> GeneratorSpawnableItems => generatorSpawnableItems;
    public IReadOnlyList<SpawnableItemDefinition> WeightedSpawnableItems => generatorSpawnableItems;
    public IReadOnlyList<MergeItemData> SpawnableItems => spawnableItems;
    public IReadOnlyList<OrderDefinition> Orders => orders;
    public bool HasGeneratorSpawnableItems => generatorSpawnableItems != null && generatorSpawnableItems.Count > 0;
    public bool HasWeightedSpawnableItems => HasGeneratorSpawnableItems;

    private void OnValidate()
    {
        boardColumns = Mathf.Max(1, boardColumns);
        boardRows = Mathf.Max(1, boardRows);

        if (initialBoardItems == null)
        {
            initialBoardItems = new List<InitialBoardItemDefinition>();
        }

        if (generatorSpawnableItems == null)
        {
            generatorSpawnableItems = new List<SpawnableItemDefinition>();
        }

        if (generatorSpawnableItems.Count > 0 || spawnableItems == null || spawnableItems.Count == 0)
        {
            return;
        }

        // Legacy migration: old levels used a plain spawnable list. Keep them playable by converting each entry to weight 1.
        for (var i = 0; i < spawnableItems.Count; i++)
        {
            var itemData = spawnableItems[i];
            if (itemData != null)
            {
                generatorSpawnableItems.Add(new SpawnableItemDefinition(itemData, 1));
            }
        }
    }
}
