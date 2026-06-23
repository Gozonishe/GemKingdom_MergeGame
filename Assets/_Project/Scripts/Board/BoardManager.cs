using System.Collections.Generic;
using UnityEngine;

public sealed class BoardManager : MonoBehaviour
{
    private const int DefaultColumns = 6;
    private const int DefaultRows = 7;
    private const int MaxRefillItemLevel = 2;

    [Header("Board Size")]
    [SerializeField] private int columns = DefaultColumns;
    [SerializeField] private int rows = DefaultRows;

    [Header("UI References")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private BoardCell cellPrefab;
    [SerializeField] private MergeItem itemPrefab;
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private List<MergeItemData> spawnableItems = new List<MergeItemData>();

    [Header("Layout")]
    [SerializeField] private Vector2 cellSize = new Vector2(120f, 120f);
    [SerializeField] private Vector2 cellSpacing = new Vector2(8f, 8f);
    [SerializeField] private bool clearExistingBoardOnInitialize = true;

    private BoardCell[,] cells;

    public BoardCell[,] Cells => cells;
    public int Columns => columns;
    public int Rows => rows;
    public Transform BoardRoot => boardRoot;
    public Transform CellsRoot => boardRoot;
    public bool IsBusy { get; private set; }

    private void Start()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        if (boardRoot == null)
        {
            boardRoot = transform;
        }

        if (cellPrefab == null)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot create a board because {nameof(cellPrefab)} is not assigned.", this);
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot fill a board because {nameof(itemPrefab)} is not assigned.", this);
            return;
        }

        if (spawnableItems == null || spawnableItems.Count == 0)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot fill a board because {nameof(spawnableItems)} is empty.", this);
            return;
        }

        if (clearExistingBoardOnInitialize)
        {
            ClearBoardRoot();
        }

        cells = new BoardCell[columns, rows];

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = CreateCell(x, y);
                cells[x, y] = cell;
                SpawnItemInCell(cell);
            }
        }

        RefreshOrders();
    }

    public bool TryMerge(BoardCell firstCell, BoardCell secondCell)
    {
        if (!CanMerge(firstCell, secondCell))
        {
            return false;
        }

        return TryMerge(firstCell.CurrentItem, secondCell.CurrentItem);
    }

    public bool TryMerge(MergeItem sourceItem, MergeItem targetItem)
    {
        if (IsBusy)
        {
            return false;
        }

        if (sourceItem == null || targetItem == null)
        {
            return false;
        }

        var sourceCell = sourceItem.CurrentCell;
        var targetCell = targetItem.CurrentCell;

        if (sourceCell == null || targetCell == null || sourceCell == targetCell)
        {
            return false;
        }

        if (!AreNeighbors(sourceCell, targetCell))
        {
            return false;
        }

        var sourceData = sourceItem.Data;
        if (sourceData == null || sourceData != targetItem.Data || sourceData.NextLevelItem == null)
        {
            return false;
        }

        if (!TrySpendMergeEnergy())
        {
            Debug.Log("Not enough energy", this);
            return false;
        }

        IsBusy = true;

        try
        {
            // Merge logic: dragged source item upgrades the target item, then the source cell becomes empty.
            targetItem.SetData(sourceData.NextLevelItem);
            targetCell.SetItem(targetItem);
            sourceCell.Clear();
            Destroy(sourceItem.gameObject);

            CollapseColumns();
            RefillBoard();
        }
        finally
        {
            IsBusy = false;
        }

        return true;
    }

    public bool CanMerge(BoardCell firstCell, BoardCell secondCell)
    {
        if (firstCell == null || secondCell == null || firstCell == secondCell)
        {
            return false;
        }

        if (!AreNeighbors(firstCell, secondCell))
        {
            return false;
        }

        var firstItem = firstCell.CurrentItem;
        var secondItem = secondCell.CurrentItem;
        return firstItem != null && firstItem.CanMergeWith(secondItem);
    }

    public BoardCell GetCellAtScreenPosition(Vector2 screenPosition, Camera uiCamera)
    {
        if (cells == null)
        {
            return null;
        }

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = cells[x, y];
                if (cell == null || cell.RectTransform == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(cell.RectTransform, screenPosition, uiCamera))
                {
                    return cell;
                }
            }
        }

        return null;
    }

    public void ResetBoard()
    {
        ClearBoardRoot();
        cells = null;
        InitializeBoard();
    }

    public void ClearBoard()
    {
        if (cells == null)
        {
            return;
        }

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = cells[x, y];
                if (cell == null)
                {
                    continue;
                }

                var item = cell.RemoveItem();
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
        }

        RefreshOrders();
    }

    public void CollapseColumns()
    {
        if (cells == null)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot collapse columns because the board is not initialized.", this);
            return;
        }

        Debug.Log($"{nameof(BoardManager)}: CollapseColumns started.", this);
        var columnItems = new List<MergeItem>(rows);

        for (var x = 0; x < columns; x++)
        {
            columnItems.Clear();

            // Collapse columns: collect existing items from bottom to top, then place them back from y = 0 upward.
            for (var y = 0; y < rows; y++)
            {
                var cell = cells[x, y];
                if (cell == null || cell.CurrentItem == null)
                {
                    continue;
                }

                columnItems.Add(cell.RemoveItem());
            }

            for (var y = 0; y < rows; y++)
            {
                var cell = cells[x, y];
                if (cell != null)
                {
                    cell.Clear();
                }
            }

            for (var itemIndex = 0; itemIndex < columnItems.Count; itemIndex++)
            {
                var targetCell = cells[x, itemIndex];
                if (targetCell != null)
                {
                    targetCell.SetItem(columnItems[itemIndex]);
                }
            }
        }
    }

    public void RefillBoard()
    {
        if (cells == null)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot refill because the board is not initialized.", this);
            return;
        }

        Debug.Log($"{nameof(BoardManager)}: RefillBoard started.", this);

        // Refill board: create new low-level items only for empty cells left after collapse.
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = cells[x, y];
                if (cell != null && cell.IsEmpty())
                {
                    SpawnItemInCell(cell);
                }
            }
        }

        var emptyCellsCount = CountEmptyCells();
        if (emptyCellsCount > 0)
        {
            Debug.LogWarning($"{nameof(BoardManager)}: RefillBoard finished with {emptyCellsCount} empty cells. Check {nameof(spawnableItems)} and {nameof(itemPrefab)}.", this);
        }

        RefreshOrders();
    }

    public void ShuffleBoard()
    {
        if (cells == null)
        {
            return;
        }

        var items = new List<MergeItem>(columns * rows);
        var itemData = new List<MergeItemData>(columns * rows);

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = cells[x, y];
                var item = cell != null ? cell.CurrentItem : null;

                if (item == null || item.Data == null)
                {
                    continue;
                }

                items.Add(item);
                itemData.Add(item.Data);
                cell.RemoveItem();
            }
        }

        Shuffle(itemData);

        var itemIndex = 0;
        for (var y = 0; y < rows && itemIndex < items.Count; y++)
        {
            for (var x = 0; x < columns && itemIndex < items.Count; x++)
            {
                var cell = cells[x, y];
                if (cell == null)
                {
                    continue;
                }

                var item = items[itemIndex];
                item.SetData(itemData[itemIndex]);
                cell.SetItem(item);
                itemIndex++;
            }
        }

        Debug.Log($"{nameof(BoardManager)}: ShuffleBoard completed.", this);
        RefreshOrders();
    }

    [ContextMenu("Refresh Board Debug")]
    public void RefreshBoardDebug()
    {
        CollapseColumns();
        RefillBoard();
    }

    public BoardCell GetCell(int x, int y)
    {
        return IsInsideBoard(x, y) ? cells[x, y] : null;
    }

    public bool IsInsideBoard(int x, int y)
    {
        return cells != null
            && x >= 0
            && x < columns
            && y >= 0
            && y < rows;
    }

    public MergeItem GetItemAt(int x, int y)
    {
        var cell = GetCell(x, y);
        return cell != null ? cell.CurrentItem : null;
    }

    public void SetItemAt(int x, int y, MergeItem item)
    {
        var cell = GetCell(x, y);
        if (cell == null)
        {
            return;
        }

        cell.SetItem(item);
    }

    private BoardCell CreateCell(int x, int y)
    {
        var cell = Instantiate(cellPrefab, boardRoot);
        cell.name = $"Cell_{x}_{y}";
        cell.Setup(x, y);
        ConfigureCellRect(cell.RectTransform, x, y);
        return cell;
    }

    private void SpawnItemInCell(BoardCell cell)
    {
        var itemData = GetRandomSpawnableItem();
        if (itemData == null)
        {
            return;
        }

        var item = Instantiate(itemPrefab, cell.RectTransform);
        item.name = $"Item_{itemData.ItemId}_{cell.X}_{cell.Y}";
        item.SetData(itemData);
        SetItemAt(cell.X, cell.Y, item);
    }

    private bool TrySpendMergeEnergy()
    {
        if (energyManager == null)
        {
            energyManager = FindFirstObjectByType<EnergyManager>();
        }

        if (energyManager == null)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot merge because {nameof(energyManager)} is not assigned and no {nameof(EnergyManager)} exists in the scene.", this);
            return false;
        }

        return energyManager.SpendEnergy();
    }

    private void RefreshOrders()
    {
        if (orderManager == null)
        {
            orderManager = FindFirstObjectByType<OrderManager>();
        }

        if (orderManager != null)
        {
            orderManager.RefreshOrders();
        }
        else
        {
            Debug.LogWarning($"{nameof(BoardManager)} on '{name}' has no {nameof(OrderManager)} assigned. Order UI will not refresh.", this);
        }
    }

    private MergeItemData GetRandomSpawnableItem()
    {
        if (spawnableItems == null || spawnableItems.Count == 0)
        {
            return null;
        }

        var validItemCount = 0;

        for (var i = 0; i < spawnableItems.Count; i++)
        {
            var itemData = spawnableItems[i];
            if (IsValidRefillItem(itemData))
            {
                validItemCount++;
            }
        }

        if (validItemCount == 0)
        {
            Debug.LogWarning($"{nameof(BoardManager)} has no spawnable items with level 1-{MaxRefillItemLevel}.", this);
            return null;
        }

        var randomIndex = Random.Range(0, validItemCount);

        for (var i = 0; i < spawnableItems.Count; i++)
        {
            var itemData = spawnableItems[i];
            if (!IsValidRefillItem(itemData))
            {
                continue;
            }

            if (randomIndex == 0)
            {
                return itemData;
            }

            randomIndex--;
        }

        return null;
    }

    private static bool IsValidRefillItem(MergeItemData itemData)
    {
        return itemData != null && itemData.Level >= 1 && itemData.Level <= MaxRefillItemLevel;
    }

    private int CountEmptyCells()
    {
        if (cells == null)
        {
            return 0;
        }

        var emptyCellsCount = 0;

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = cells[x, y];
                if (cell != null && cell.IsEmpty())
                {
                    emptyCellsCount++;
                }
            }
        }

        return emptyCellsCount;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private void ConfigureCellRect(RectTransform cellRectTransform, int x, int y)
    {
        if (cellRectTransform == null)
        {
            return;
        }

        cellRectTransform.anchorMin = new Vector2(0f, 1f);
        cellRectTransform.anchorMax = new Vector2(0f, 1f);
        cellRectTransform.pivot = new Vector2(0f, 1f);
        cellRectTransform.sizeDelta = cellSize;

        var visualY = rows - 1 - y;
        cellRectTransform.anchoredPosition = new Vector2(
            x * (cellSize.x + cellSpacing.x),
            -visualY * (cellSize.y + cellSpacing.y));
    }

    private void ClearBoardRoot()
    {
        if (boardRoot == null)
        {
            return;
        }

        for (var i = boardRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(boardRoot.GetChild(i).gameObject);
        }
    }

    private static bool AreNeighbors(BoardCell firstCell, BoardCell secondCell)
    {
        var delta = firstCell.GridPosition - secondCell.GridPosition;
        return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1;
    }
}
