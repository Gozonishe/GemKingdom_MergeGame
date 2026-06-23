using UnityEngine;

public sealed class ItemSpawner : MonoBehaviour
{
    [Header("Item Pool Source")]
    [SerializeField] private MergeItemData[] spawnableItems;
    [SerializeField] private MergeItem itemPrefab;

    [Header("Spawn Parents")]
    [SerializeField] private Transform itemsRoot;

    public MergeItemData[] SpawnableItems => spawnableItems;
    public MergeItem ItemPrefab => itemPrefab;
    public Transform ItemsRoot => itemsRoot;

    public MergeItemData GetRandomItemData()
    {
        if (spawnableItems == null || spawnableItems.Length == 0)
        {
            return null;
        }

        return spawnableItems[Random.Range(0, spawnableItems.Length)];
    }

    public MergeItem SpawnItem(BoardCell targetCell, MergeItemData itemData = null)
    {
        var dataToSpawn = itemData != null ? itemData : GetRandomItemData();
        if (targetCell == null)
        {
            Debug.LogError($"{nameof(ItemSpawner)} on '{name}' cannot spawn because target cell is null.", this);
            return null;
        }

        if (dataToSpawn == null)
        {
            Debug.LogError($"{nameof(ItemSpawner)} on '{name}' cannot spawn because no item data is available.", this);
            return null;
        }

        if (itemPrefab == null)
        {
            Debug.LogError($"{nameof(ItemSpawner)} on '{name}' cannot spawn because {nameof(itemPrefab)} is not assigned.", this);
            return null;
        }

        var parent = targetCell.ItemRoot != null ? targetCell.ItemRoot : itemsRoot;
        var mergeItem = Instantiate(itemPrefab, parent);
        mergeItem.SetData(dataToSpawn);
        targetCell.SetItem(mergeItem);

        return mergeItem;
    }
}
