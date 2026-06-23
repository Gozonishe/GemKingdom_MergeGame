using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Merge-2 Puzzle/Levels/Level Data")]
public sealed class LevelData : ScriptableObject
{
    [Header("Level")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private int movesCount = 100;

    [Header("Board Balance")]
    [SerializeField] private List<MergeItemData> spawnableItems = new List<MergeItemData>();

    [Header("Orders")]
    [SerializeField] private List<OrderDefinition> orders = new List<OrderDefinition>();

    public int LevelNumber => levelNumber;
    public int MovesCount => movesCount;
    public IReadOnlyList<MergeItemData> SpawnableItems => spawnableItems;
    public IReadOnlyList<OrderDefinition> Orders => orders;
}
