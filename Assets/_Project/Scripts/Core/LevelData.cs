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

    [Tooltip("Rows are written from top to bottom. Use 1 or . for a cell, 0 or _ for a hole, S for a full stone, C for a cracked stone and P for Item_Spider.")]
    [TextArea(3, 12)]
    [SerializeField] private string boardMask;

    [Header("Initial Board Fill")]
    [Tooltip("Items that will be placed on random free board cells when a level starts.")]
    [SerializeField] private List<InitialBoardItemDefinition> initialBoardItems = new List<InitialBoardItemDefinition>();

    [Header("Bottom Generator Balance")]
    [Tooltip("Items that can appear in the bottom generated-item slot after tapping the generator button.")]
    [FormerlySerializedAs("weightedSpawnableItems")]
    [SerializeField] private List<SpawnableItemDefinition> generatorSpawnableItems = new List<SpawnableItemDefinition>();
    [HideInInspector]
    [SerializeField] private List<MergeItemData> spawnableItems = new List<MergeItemData>();

    [Header("Reward")]
    [Min(0)]
    [SerializeField] private int coinReward;

    [Header("Orders")]
    [SerializeField] private List<OrderDefinition> orders = new List<OrderDefinition>();

    public int LevelNumber => levelNumber;
    public int MovesCount => movesCount;
    public int BoardColumns => Mathf.Max(1, boardColumns);
    public int BoardRows => Mathf.Max(1, boardRows);
    public string BoardMask => boardMask;
    public IReadOnlyList<InitialBoardItemDefinition> InitialBoardItems => initialBoardItems;
    public IReadOnlyList<SpawnableItemDefinition> GeneratorSpawnableItems => generatorSpawnableItems;
    public IReadOnlyList<SpawnableItemDefinition> WeightedSpawnableItems => generatorSpawnableItems;
    public IReadOnlyList<MergeItemData> SpawnableItems => spawnableItems;
    public int CoinReward => Mathf.Max(0, coinReward);
    public IReadOnlyList<OrderDefinition> Orders => orders;
    public bool HasGeneratorSpawnableItems => generatorSpawnableItems != null && generatorSpawnableItems.Count > 0;
    public bool HasWeightedSpawnableItems => HasGeneratorSpawnableItems;

    public bool TryGetBoardMaskRows(out string[] maskRows, out string validationError)
    {
        validationError = null;

        if (string.IsNullOrWhiteSpace(boardMask))
        {
            maskRows = Array.Empty<string>();
            return false;
        }

        var rawRows = boardMask.Replace("\r", string.Empty).Split('\n');
        var normalizedRows = new List<string>(rawRows.Length);

        for (var i = 0; i < rawRows.Length; i++)
        {
            var row = rawRows[i].Trim();
            if (!string.IsNullOrEmpty(row))
            {
                normalizedRows.Add(row);
            }
        }

        if (normalizedRows.Count != BoardRows)
        {
            maskRows = normalizedRows.ToArray();
            validationError = $"expected {BoardRows} rows, but found {normalizedRows.Count}";
            return false;
        }

        for (var y = 0; y < normalizedRows.Count; y++)
        {
            var row = normalizedRows[y];
            if (row.Length != BoardColumns)
            {
                maskRows = normalizedRows.ToArray();
                validationError = $"row {y + 1} must contain {BoardColumns} symbols, but contains {row.Length}";
                return false;
            }

            for (var x = 0; x < row.Length; x++)
            {
                if (!IsSupportedBoardMaskSymbol(row[x]))
                {
                    maskRows = normalizedRows.ToArray();
                    validationError = $"unsupported symbol '{row[x]}' at row {y + 1}, column {x + 1}";
                    return false;
                }
            }
        }

        maskRows = normalizedRows.ToArray();
        return true;
    }

    private void OnValidate()
    {
        boardColumns = Mathf.Max(1, boardColumns);
        boardRows = Mathf.Max(1, boardRows);

        if (!string.IsNullOrWhiteSpace(boardMask)
            && !TryGetBoardMaskRows(out _, out var validationError))
        {
            Debug.LogWarning($"{name} has an invalid Board Mask: {validationError}. A full rectangular board will be used at runtime.", this);
        }

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

    [ContextMenu("Generate Full Board Mask")]
    private void GenerateFullBoardMask()
    {
        var rows = new string[BoardRows];
        var fullRow = new string('1', BoardColumns);

        for (var y = 0; y < rows.Length; y++)
        {
            rows[y] = fullRow;
        }

        boardMask = string.Join("\n", rows);
        MarkDirtyInEditor();
    }

    [ContextMenu("Clear Board Mask")]
    private void ClearBoardMask()
    {
        boardMask = string.Empty;
        MarkDirtyInEditor();
    }

    private static bool IsSupportedBoardMaskSymbol(char symbol)
    {
        var normalizedSymbol = char.ToUpperInvariant(symbol);
        return normalizedSymbol == '1'
            || normalizedSymbol == '.'
            || normalizedSymbol == '0'
            || normalizedSymbol == '_'
            || normalizedSymbol == 'S'
            || normalizedSymbol == 'C'
            || normalizedSymbol == 'P';
    }

    private void MarkDirtyInEditor()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
