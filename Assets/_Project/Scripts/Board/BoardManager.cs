using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BoardManager : MonoBehaviour
{
    private const int DefaultColumns = 6;
    private const int DefaultRows = 7;

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
    [SerializeField] private Vector2 cellSize = new Vector2(156f, 156f);
    [SerializeField] private Vector2 cellSpacing = new Vector2(10.4f, 10.4f);
    [SerializeField] private bool clearExistingBoardOnInitialize = true;

    [Header("Gravity Animation")]
    [SerializeField] private bool animateGravity = true;
    [SerializeField] private float fallDuration = 0.22f;
    [SerializeField] private float fallBouncePixels = 14f;
    [SerializeField] private float refillFallOffset = 220f;

    private BoardCell[,] cells;
    private bool isInitialized;

    public BoardCell[,] Cells => cells;
    public int Columns => columns;
    public int Rows => rows;
    public Transform BoardRoot => boardRoot;
    public Transform CellsRoot => boardRoot;
    public bool IsBusy { get; private set; }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializeBoard();
        }
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
        isInitialized = true;

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

    public void SetSpawnableItems(IReadOnlyList<MergeItemData> items)
    {
        spawnableItems.Clear();

        if (items == null)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                spawnableItems.Add(items[i]);
            }
        }
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

        // Merge logic: dragged source item upgrades the target item, then the source cell becomes empty.
        targetItem.SetData(sourceData.NextLevelItem);
        targetCell.SetItem(targetItem);
        targetItem.PlayMergePopEffect();
        sourceCell.Clear();
        Destroy(sourceItem.gameObject);

        if (animateGravity && isActiveAndEnabled)
        {
            StartCoroutine(ResolveBoardAfterMergeCoroutine());
        }
        else
        {
            CollapseColumns();
            RefillBoard();
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
        isInitialized = false;
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
        CollapseColumnsInternal(false, null);
    }

    private void CollapseColumnsInternal(bool animateItems, List<MergeItem> animatedItems)
    {
        if (cells == null)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot collapse columns because the board is not initialized.", this);
            return;
        }

        Debug.Log($"{nameof(BoardManager)}: CollapseColumns started.", this);
        var columnItems = new List<MergeItem>(rows);
        var columnCells = new List<BoardCell>(rows);

        for (var x = 0; x < columns; x++)
        {
            columnItems.Clear();
            columnCells.Clear();

            for (var y = 0; y < rows; y++)
            {
                var cell = cells[x, y];
                if (cell != null)
                {
                    columnCells.Add(cell);
                }
            }

            columnCells.Sort(CompareCellsBottomToTop);

            // Collapse columns: collect existing items from visual bottom to top, then place them back from bottom upward.
            // This keeps gravity correct even when a GridLayoutGroup or anchors make logical y different from screen y.
            for (var cellIndex = 0; cellIndex < columnCells.Count; cellIndex++)
            {
                var cell = columnCells[cellIndex];
                if (cell.CurrentItem != null)
                {
                    columnItems.Add(cell.RemoveItem());
                }
            }

            for (var cellIndex = 0; cellIndex < columnCells.Count; cellIndex++)
            {
                columnCells[cellIndex].Clear();
            }

            for (var itemIndex = 0; itemIndex < columnItems.Count && itemIndex < columnCells.Count; itemIndex++)
            {
                var item = columnItems[itemIndex];
                var startWorldPosition = item.transform.position;
                columnCells[itemIndex].SetItem(item);
                PlayFallFromWorldPosition(item, startWorldPosition, animateItems, animatedItems);
            }
        }
    }

    public void RefillBoard()
    {
        RefillBoardInternal(false, null);
        RefreshOrders();
    }

    private void RefillBoardInternal(bool animateItems, List<MergeItem> animatedItems)
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
                    SpawnItemInCell(cell, animateItems, animatedItems);
                }
            }
        }

        var emptyCellsCount = CountEmptyCells();
        if (emptyCellsCount > 0)
        {
            Debug.LogWarning($"{nameof(BoardManager)}: RefillBoard finished with {emptyCellsCount} empty cells. Check {nameof(spawnableItems)} and {nameof(itemPrefab)}.", this);
        }

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
        SpawnItemInCell(cell, false, null);
    }

    private void SpawnItemInCell(BoardCell cell, bool animateItem, List<MergeItem> animatedItems)
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

        if (animateItem)
        {
            PlayNewItemFall(item, animatedItems);
        }
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
            if (itemData != null)
            {
                validItemCount++;
            }
        }

        if (validItemCount == 0)
        {
            Debug.LogWarning($"{nameof(BoardManager)} has no valid spawnable items.", this);
            return null;
        }

        var randomIndex = Random.Range(0, validItemCount);

        for (var i = 0; i < spawnableItems.Count; i++)
        {
            var itemData = spawnableItems[i];
            if (itemData == null)
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

    private IEnumerator ResolveBoardAfterMergeCoroutine()
    {
        var animatedItems = new List<MergeItem>(columns * rows);

        CollapseColumnsInternal(true, animatedItems);
        RefillBoardInternal(true, animatedItems);

        while (HasActiveFallAnimations(animatedItems))
        {
            yield return null;
        }

        RefreshOrders();
        IsBusy = false;
    }

    private void PlayFallFromWorldPosition(MergeItem item, Vector3 startWorldPosition, bool animateItem, List<MergeItem> animatedItems)
    {
        if (!animateItem || item == null || item.transform is not RectTransform itemRectTransform)
        {
            return;
        }

        var targetWorldPosition = itemRectTransform.position;
        if ((targetWorldPosition - startWorldPosition).sqrMagnitude < 0.01f)
        {
            return;
        }

        itemRectTransform.position = startWorldPosition;
        item.PlayFallToCellEffect(fallDuration, fallBouncePixels);
        animatedItems?.Add(item);
    }

    private void PlayNewItemFall(MergeItem item, List<MergeItem> animatedItems)
    {
        if (item == null || item.transform is not RectTransform itemRectTransform)
        {
            return;
        }

        itemRectTransform.anchoredPosition = new Vector2(0f, Mathf.Max(0f, refillFallOffset));
        item.PlayFallToCellEffect(fallDuration, fallBouncePixels, true);
        animatedItems?.Add(item);
    }

    private static bool HasActiveFallAnimations(List<MergeItem> animatedItems)
    {
        if (animatedItems == null)
        {
            return false;
        }

        for (var i = animatedItems.Count - 1; i >= 0; i--)
        {
            var item = animatedItems[i];
            if (item == null)
            {
                animatedItems.RemoveAt(i);
                continue;
            }

            if (item.IsAnimatingFall)
            {
                return true;
            }

            animatedItems.RemoveAt(i);
        }

        return false;
    }

    private static int CompareCellsBottomToTop(BoardCell firstCell, BoardCell secondCell)
    {
        var firstY = GetCellWorldY(firstCell);
        var secondY = GetCellWorldY(secondCell);
        var positionComparison = firstY.CompareTo(secondY);

        if (positionComparison != 0)
        {
            return positionComparison;
        }

        return secondCell.Y.CompareTo(firstCell.Y);
    }

    private static float GetCellWorldY(BoardCell cell)
    {
        return cell != null && cell.RectTransform != null
            ? cell.RectTransform.position.y
            : 0f;
    }
}
