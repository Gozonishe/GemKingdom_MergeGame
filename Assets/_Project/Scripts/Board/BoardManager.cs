using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BoardManager : MonoBehaviour
{
    private const int DefaultColumns = 6;
    private const int DefaultRows = 6;
    private const int RequiredConnectedMergeItemCount = 3;
    private const int MaxCascadeMergeSteps = 32;
    private const string SpiderRemovedByDynamiteReason = "Dynamite";
    private const string SpiderRemovedByTrapReason = "Trapped";

    [Header("Board Size")]
    [SerializeField] private int columns = DefaultColumns;
    [SerializeField] private int rows = DefaultRows;

    [Header("UI References")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private BoardCell cellPrefab;
    [SerializeField] private MergeItem itemPrefab;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private List<InitialBoardItemDefinition> initialBoardItems = new List<InitialBoardItemDefinition>();
    [SerializeField] private List<SpawnableItemDefinition> weightedSpawnableItems = new List<SpawnableItemDefinition>();
    [HideInInspector]
    [SerializeField] private List<MergeItemData> spawnableItems = new List<MergeItemData>();

    [Header("Layout")]
    [SerializeField] private Vector2 cellSize = new Vector2(137.3f, 137.3f);
    [SerializeField] private Vector2 cellSpacing = new Vector2(11.4f, 11.4f);
    [SerializeField] private bool clearExistingBoardOnInitialize = true;

    [Header("Gravity Animation")]
    [SerializeField] private bool animateGravity = true;
    [SerializeField] private float fallDuration = 0.22f;
    [SerializeField] private float fallBouncePixels = 14f;
    [SerializeField] private float refillFallOffset = 220f;

    [Header("Merge Animation")]
    [SerializeField] private bool animateConnectedMerge = true;
    [SerializeField, Min(0f)] private float connectedMergeMoveDuration = 0.16f;
    [SerializeField, Min(0f)] private float connectedMergeResultDelay = 0.06f;
    [SerializeField, Range(0f, 1f)] private float connectedMergeEndAlpha = 0.25f;
    [SerializeField, Range(0.1f, 1f)] private float connectedMergeEndScale = 0.75f;

    [Header("Audio")]
    [SerializeField] private AudioClip playerPlaceSound;
    [SerializeField] private AudioClip mergeSound;
    [SerializeField, Min(0f)] private float mergeSoundDelay = 0.06f;
    [SerializeField] private AudioClip dynamiteSound;

    [Header("Spider Movement")]
    [SerializeField] private bool animateSpiderMovement = true;
    [SerializeField, Min(0f)] private float spiderMoveDuration = 0.18f;

    private BoardCell[,] cells;
    private bool isInitialized;

    private struct MovingMergeItem
    {
        public MergeItem Item;
        public RectTransform RectTransform;
        public CanvasGroup CanvasGroup;
        public Vector3 StartWorldPosition;
        public Vector3 StartScale;
        public float StartAlpha;
    }

    private readonly List<MergeItem> spiderMovementBuffer = new List<MergeItem>(8);
    private readonly List<BoardCell> spiderNeighborBuffer = new List<BoardCell>(4);

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
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot create start items because {nameof(itemPrefab)} is not assigned.", this);
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
            }
        }

        FillInitialBoard();
        RefreshOrders();
    }

    public void SetBoardSize(int newColumns, int newRows)
    {
        columns = Mathf.Max(1, newColumns);
        rows = Mathf.Max(1, newRows);
    }

    public void SetInitialBoardItems(IReadOnlyList<InitialBoardItemDefinition> items)
    {
        initialBoardItems.Clear();

        if (items == null)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item != null && item.ItemData != null && item.Count > 0)
            {
                initialBoardItems.Add(item);
            }
        }
    }

    public void SetSpawnableItems(IReadOnlyList<MergeItemData> items)
    {
        weightedSpawnableItems.Clear();
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

    public void SetSpawnableItems(IReadOnlyList<SpawnableItemDefinition> items)
    {
        weightedSpawnableItems.Clear();
        spawnableItems.Clear();

        if (items == null)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item != null && item.ItemData != null && item.Weight > 0)
            {
                weightedSpawnableItems.Add(item);
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

    public bool TryMergeWithAdjacentItem(BoardCell placedItemCell)
    {
        if (IsBusy || placedItemCell == null || !ContainsCell(placedItemCell) || placedItemCell.CurrentItem == null)
        {
            return false;
        }

        var placedItem = placedItemCell.CurrentItem;
        var adjacentDestroyCell = FindAdjacentDestroyBothCell(placedItemCell, placedItem);
        if (adjacentDestroyCell != null)
        {
            return TryMerge(placedItem, adjacentDestroyCell.CurrentItem);
        }

        return TryResolveConnectedMergeCascade(placedItemCell);
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

        if (!ContainsCell(sourceCell) || !ContainsCell(targetCell))
        {
            return false;
        }

        if (!AreNeighbors(sourceCell, targetCell))
        {
            return false;
        }

        var sourceData = sourceItem.Data;
        var targetData = targetItem.Data;
        if (sourceData == null || targetData == null)
        {
            return false;
        }

        if (CanDestroyBothByAnyNeighborMerge(sourceData, targetData))
        {
            return TryDestroyBothByAnyNeighborMerge(sourceCell, targetCell, sourceItem, targetItem);
        }

        return false;
    }

    public bool TryUseDestroyBothItemOnCell(MergeItem destroyItem, BoardCell targetCell)
    {
        if (IsBusy || destroyItem == null || targetCell == null || !ContainsCell(targetCell))
        {
            return false;
        }

        var targetItem = targetCell.CurrentItem;
        var destroyItemData = destroyItem.Data;
        var targetItemData = targetItem != null ? targetItem.Data : null;

        if (destroyItemData == null
            || targetItem == null
            || targetItem == destroyItem
            || !destroyItemData.DestroyBothOnAnyNeighborMerge)
        {
            return false;
        }

        IsBusy = true;

        destroyItem.PlayDisappearEffectAt(targetItem.transform);

        var sourceCell = destroyItem.CurrentCell;
        if (sourceCell != null)
        {
            sourceCell.Clear();
        }

        targetCell.Clear();
        NotifyItemDestroyed(destroyItemData);
        NotifyItemDestroyed(targetItemData);
        PlayDynamiteSound();
        Destroy(destroyItem.gameObject);
        Destroy(targetItem.gameObject);
        if (targetItemData != null && targetItemData.IsSpider)
        {
            Debug.Log($"{nameof(BoardManager)} removed spider: {SpiderRemovedByDynamiteReason}.", this);
        }

        ApplyAdjacentMergeReactions(targetCell);
        ResolveSpidersAfterPlayerPlacement();
        RefreshOrders();
        IsBusy = false;

        return true;
    }

    public bool CanMerge(BoardCell firstCell, BoardCell secondCell)
    {
        if (firstCell == null || secondCell == null || firstCell == secondCell)
        {
            return false;
        }

        if (!ContainsCell(firstCell) || !ContainsCell(secondCell))
        {
            return false;
        }

        if (!AreNeighbors(firstCell, secondCell))
        {
            return false;
        }

        var firstItem = firstCell.CurrentItem;
        var secondItem = secondCell.CurrentItem;
        return firstItem != null
            && secondItem != null
            && CanDestroyBothByAnyNeighborMerge(firstItem.Data, secondItem.Data);
    }

    public bool HasMergeableAdjacentItem(BoardCell placedItemCell)
    {
        if (placedItemCell == null || !ContainsCell(placedItemCell) || placedItemCell.CurrentItem == null)
        {
            return false;
        }

        var item = placedItemCell.CurrentItem;
        if (FindAdjacentDestroyBothCell(placedItemCell, item) != null)
        {
            return true;
        }

        var itemData = item.Data;
        if (itemData == null || itemData.ReactToAdjacentMerge || itemData.NextLevelItem == null)
        {
            return false;
        }

        var connectedCells = new List<BoardCell>(RequiredConnectedMergeItemCount);
        CollectConnectedMatchingCells(placedItemCell, itemData, connectedCells);
        return connectedCells.Count >= RequiredConnectedMergeItemCount;
    }

    public void ResolveSpidersAfterPlayerPlacement()
    {
        if (cells == null)
        {
            return;
        }

        CollectSpidersOnBoard(spiderMovementBuffer);

        for (var i = 0; i < spiderMovementBuffer.Count; i++)
        {
            var spider = spiderMovementBuffer[i];
            if (!IsActiveSpiderOnBoard(spider))
            {
                continue;
            }

            var spiderCell = spider.CurrentCell;
            GetFreeOrthogonalNeighbors(spiderCell, spiderNeighborBuffer);

            if (spiderNeighborBuffer.Count == 0)
            {
                RemoveSpider(spider, SpiderRemovedByTrapReason);
                continue;
            }

            var targetCell = spiderNeighborBuffer[Random.Range(0, spiderNeighborBuffer.Count)];
            MoveSpider(spider, targetCell);
        }

        spiderMovementBuffer.Clear();
        spiderNeighborBuffer.Clear();
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
                PlayFallFromWorldPosition(item, startWorldPosition, animateGravity && animateItems, animatedItems);
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

        // Refill board: create level-configured random items only for empty cells left after collapse.
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = cells[x, y];
                if (cell != null && cell.IsEmpty())
                {
                    SpawnItemInCell(cell, animateGravity && animateItems, animatedItems);
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

    public bool ContainsCell(BoardCell targetCell)
    {
        if (targetCell == null || cells == null)
        {
            return false;
        }

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                if (cells[x, y] == targetCell)
                {
                    return true;
                }
            }
        }

        return false;
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

    public MergeItem CreateItemInCell(BoardCell cell, MergeItemData itemData, bool replaceExistingItem)
    {
        if (cell == null || itemData == null)
        {
            return null;
        }

        if (itemPrefab == null)
        {
            Debug.LogError($"{nameof(BoardManager)} on '{name}' cannot create item because {nameof(itemPrefab)} is not assigned.", this);
            return null;
        }

        if (!cell.IsEmpty())
        {
            if (!replaceExistingItem)
            {
                return null;
            }

            var existingItem = cell.RemoveItem();
            if (existingItem != null)
            {
                Destroy(existingItem.gameObject);
            }
        }

        var item = Instantiate(itemPrefab, cell.RectTransform);
        item.name = $"Item_{itemData.ItemId}_{cell.X}_{cell.Y}";
        item.SetData(itemData);
        cell.SetItem(item);
        return item;
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

    private void SpawnSpecificItemInCell(BoardCell cell, MergeItemData itemData)
    {
        CreateItemInCell(cell, itemData, true);
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

    private void FillInitialBoard()
    {
        if (cells == null || initialBoardItems == null || initialBoardItems.Count == 0)
        {
            return;
        }

        var freeCells = new List<BoardCell>(columns * rows);
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var cell = cells[x, y];
                if (cell != null && cell.IsEmpty())
                {
                    freeCells.Add(cell);
                }
            }
        }

        Shuffle(freeCells);

        var requestedItemsCount = CountInitialBoardItemRequests();
        var nextFreeCellIndex = 0;
        for (var i = 0; i < initialBoardItems.Count && nextFreeCellIndex < freeCells.Count; i++)
        {
            var definition = initialBoardItems[i];
            if (definition == null || definition.ItemData == null || definition.Count <= 0)
            {
                continue;
            }

            for (var count = 0; count < definition.Count && nextFreeCellIndex < freeCells.Count; count++)
            {
                SpawnSpecificItemInCell(freeCells[nextFreeCellIndex], definition.ItemData);
                nextFreeCellIndex++;
            }
        }

        if (requestedItemsCount > freeCells.Count)
        {
            Debug.LogWarning($"{nameof(BoardManager)} on '{name}' has {requestedItemsCount} requested start items for {freeCells.Count} board cells. Extra items were skipped.", this);
        }
    }

    private int CountInitialBoardItemRequests()
    {
        if (initialBoardItems == null)
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < initialBoardItems.Count; i++)
        {
            var item = initialBoardItems[i];
            if (item != null && item.ItemData != null && item.Count > 0)
            {
                count += item.Count;
            }
        }

        return count;
    }

    private bool TryDestroyBothByAnyNeighborMerge(BoardCell sourceCell, BoardCell targetCell, MergeItem sourceItem, MergeItem targetItem)
    {
        IsBusy = true;

        var sourceData = sourceItem != null ? sourceItem.Data : null;
        var targetData = targetItem != null ? targetItem.Data : null;

        var effectItem = sourceData != null && sourceData.DestroyBothOnAnyNeighborMerge
            ? sourceItem
            : targetItem;
        var effectTarget = effectItem == sourceItem ? targetItem : sourceItem;
        effectItem.PlayDisappearEffectAt(effectTarget.transform);

        sourceCell.Clear();
        targetCell.Clear();
        NotifyItemDestroyed(sourceData);
        NotifyItemDestroyed(targetData);
        Destroy(sourceItem.gameObject);
        Destroy(targetItem.gameObject);
        ApplyAdjacentMergeReactions(targetCell);
        ResolveSpidersAfterPlayerPlacement();
        RefreshOrders();
        IsBusy = false;

        return true;
    }

    private static bool CanDestroyBothByAnyNeighborMerge(MergeItemData firstData, MergeItemData secondData)
    {
        return firstData != null
            && secondData != null
            && !firstData.IsSpider
            && !secondData.IsSpider
            && (firstData.DestroyBothOnAnyNeighborMerge || secondData.DestroyBothOnAnyNeighborMerge);
    }

    public void RefreshOrders()
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
        var weightedItem = GetRandomWeightedSpawnableItem();
        if (weightedItem != null)
        {
            return weightedItem;
        }

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

    private void ApplyAdjacentMergeReactions(BoardCell mergeResultCell)
    {
        var affectedCells = new HashSet<BoardCell>();
        AddAdjacentCells(mergeResultCell, affectedCells);

        affectedCells.Remove(mergeResultCell);

        foreach (var cell in affectedCells)
        {
            ApplyAdjacentMergeReaction(cell);
        }
    }

    private void AddAdjacentCells(BoardCell centerCell, HashSet<BoardCell> targetCells)
    {
        if (centerCell == null || targetCells == null)
        {
            return;
        }

        AddCellIfInside(centerCell.X + 1, centerCell.Y, targetCells);
        AddCellIfInside(centerCell.X - 1, centerCell.Y, targetCells);
        AddCellIfInside(centerCell.X, centerCell.Y + 1, targetCells);
        AddCellIfInside(centerCell.X, centerCell.Y - 1, targetCells);
    }

    private void AddCellIfInside(int x, int y, HashSet<BoardCell> targetCells)
    {
        var cell = GetCell(x, y);
        if (cell != null)
        {
            targetCells.Add(cell);
        }
    }

    private BoardCell FindAdjacentDestroyBothCell(BoardCell placedItemCell, MergeItem placedItem)
    {
        if (placedItemCell == null || placedItem == null || placedItem.Data == null)
        {
            return null;
        }

        var rightCell = GetCell(placedItemCell.X + 1, placedItemCell.Y);
        if (CanDestroyBothWithCell(placedItem, rightCell))
        {
            return rightCell;
        }

        var leftCell = GetCell(placedItemCell.X - 1, placedItemCell.Y);
        if (CanDestroyBothWithCell(placedItem, leftCell))
        {
            return leftCell;
        }

        var topCell = GetCell(placedItemCell.X, placedItemCell.Y + 1);
        if (CanDestroyBothWithCell(placedItem, topCell))
        {
            return topCell;
        }

        var bottomCell = GetCell(placedItemCell.X, placedItemCell.Y - 1);
        if (CanDestroyBothWithCell(placedItem, bottomCell))
        {
            return bottomCell;
        }

        return null;
    }

    private bool CanDestroyBothWithCell(MergeItem placedItem, BoardCell targetCell)
    {
        var targetItem = targetCell != null ? targetCell.CurrentItem : null;
        return placedItem != null
            && targetItem != null
            && CanDestroyBothByAnyNeighborMerge(placedItem.Data, targetItem.Data);
    }

    private void CollectConnectedMatchingCells(BoardCell startCell, MergeItemData itemData, List<BoardCell> result)
    {
        if (startCell == null || itemData == null || result == null)
        {
            return;
        }

        result.Clear();

        var visitedCells = new HashSet<BoardCell>();
        var pendingCells = new List<BoardCell> { startCell };
        visitedCells.Add(startCell);

        for (var index = 0; index < pendingCells.Count; index++)
        {
            var cell = pendingCells[index];
            if (!DoesCellContainItemData(cell, itemData))
            {
                continue;
            }

            result.Add(cell);
            AddMatchingNeighborCell(cell.X + 1, cell.Y, itemData, visitedCells, pendingCells);
            AddMatchingNeighborCell(cell.X - 1, cell.Y, itemData, visitedCells, pendingCells);
            AddMatchingNeighborCell(cell.X, cell.Y + 1, itemData, visitedCells, pendingCells);
            AddMatchingNeighborCell(cell.X, cell.Y - 1, itemData, visitedCells, pendingCells);
        }
    }

    private void AddMatchingNeighborCell(
        int x,
        int y,
        MergeItemData itemData,
        HashSet<BoardCell> visitedCells,
        List<BoardCell> pendingCells)
    {
        var cell = GetCell(x, y);
        if (cell == null || visitedCells == null || pendingCells == null || visitedCells.Contains(cell))
        {
            return;
        }

        visitedCells.Add(cell);

        if (DoesCellContainItemData(cell, itemData))
        {
            pendingCells.Add(cell);
        }
    }

    private static bool DoesCellContainItemData(BoardCell cell, MergeItemData itemData)
    {
        var item = cell != null ? cell.CurrentItem : null;
        return item != null && item.Data == itemData;
    }

    private bool TryMergeConnectedGroup(BoardCell mergeResultCell, List<BoardCell> connectedCells, MergeItemData nextLevelItem)
    {
        if (mergeResultCell == null || connectedCells == null || connectedCells.Count < RequiredConnectedMergeItemCount || nextLevelItem == null)
        {
            return false;
        }

        var resultItem = mergeResultCell.CurrentItem;
        if (resultItem == null)
        {
            return false;
        }

        for (var i = 0; i < connectedCells.Count; i++)
        {
            var cell = connectedCells[i];
            if (cell == null || cell == mergeResultCell)
            {
                continue;
            }

            var item = cell.CurrentItem;
            cell.Clear();

            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        resultItem.SetData(nextLevelItem, true);
        mergeResultCell.SetItem(resultItem);
        PlayMergeSound();
        resultItem.PlayMergePopEffect();
        ApplyAdjacentMergeReactions(mergeResultCell);
        return true;
    }

    private bool TryResolveConnectedMergeCascade(BoardCell mergeResultCell)
    {
        var connectedCells = new List<BoardCell>(columns * rows);
        if (!TryCollectConnectedMergeGroup(mergeResultCell, connectedCells, out _))
        {
            return false;
        }

        IsBusy = true;
        StartCoroutine(ResolveConnectedMergeCascadeCoroutine(mergeResultCell));
        return true;
    }

    private IEnumerator ResolveConnectedMergeCascadeCoroutine(BoardCell mergeResultCell)
    {
        var didMerge = false;
        var mergeSteps = 0;
        var connectedCells = new List<BoardCell>(columns * rows);
        var consumedItems = new List<MergeItem>(columns * rows);

        while (mergeSteps < MaxCascadeMergeSteps
            && TryCollectConnectedMergeGroup(mergeResultCell, connectedCells, out var nextLevelItem))
        {
            // Merge logic: the last placed/generated item stays in place and upgrades.
            // Other matched items visually slide into it before the upgraded sprite appears.
            if (!ExtractConnectedMergeItems(mergeResultCell, connectedCells, consumedItems))
            {
                break;
            }

            yield return PlayConnectedMergeStep(mergeResultCell, consumedItems, nextLevelItem);

            didMerge = true;
            mergeSteps++;
        }

        if (mergeSteps >= MaxCascadeMergeSteps)
        {
            Debug.LogWarning($"{nameof(BoardManager)} stopped cascade merge after {MaxCascadeMergeSteps} steps. Check item data chains for loops.", this);
        }

        if (didMerge)
        {
            ResolveSpidersAfterPlayerPlacement();
            RefreshOrders();
        }

        IsBusy = false;
    }

    private bool ExtractConnectedMergeItems(BoardCell mergeResultCell, List<BoardCell> connectedCells, List<MergeItem> consumedItems)
    {
        consumedItems.Clear();

        if (mergeResultCell == null || mergeResultCell.CurrentItem == null || connectedCells == null)
        {
            return false;
        }

        for (var i = 0; i < connectedCells.Count; i++)
        {
            var cell = connectedCells[i];
            if (cell == null || cell == mergeResultCell)
            {
                continue;
            }

            var item = cell.RemoveItem();
            if (item != null)
            {
                consumedItems.Add(item);
            }
        }

        return consumedItems.Count > 0;
    }

    private IEnumerator PlayConnectedMergeStep(BoardCell mergeResultCell, List<MergeItem> consumedItems, MergeItemData nextLevelItem)
    {
        var resultItem = mergeResultCell != null ? mergeResultCell.CurrentItem : null;
        if (resultItem == null || nextLevelItem == null)
        {
            DestroyConsumedItems(consumedItems);
            yield break;
        }

        PlayMergeSound();

        if (animateConnectedMerge && connectedMergeMoveDuration > 0f)
        {
            yield return MoveConsumedItemsToResult(consumedItems, resultItem);
        }

        DestroyConsumedItems(consumedItems);

        if (connectedMergeResultDelay > 0f)
        {
            yield return new WaitForSeconds(connectedMergeResultDelay);
        }

        resultItem.SetData(nextLevelItem, true);
        mergeResultCell.SetItem(resultItem);
        resultItem.PlayMergePopEffect();
        ApplyAdjacentMergeReactions(mergeResultCell);
    }

    private void PlayMergeSound()
    {
        if (mergeSound == null)
        {
            return;
        }

        if (mergeSoundDelay <= 0f)
        {
            UIAudioController.Instance?.PlayUISound(mergeSound);
            return;
        }

        StartCoroutine(PlayMergeSoundDelayed(mergeSound, mergeSoundDelay));
    }

    public void PlayPlayerPlaceSound()
    {
        if (playerPlaceSound == null)
        {
            return;
        }

        UIAudioController.Instance?.PlayUISound(playerPlaceSound);
    }

    private IEnumerator PlayMergeSoundDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        UIAudioController.Instance?.PlayUISound(clip);
    }

    private void PlayDynamiteSound()
    {
        if (dynamiteSound == null)
        {
            return;
        }

        UIAudioController.Instance?.PlayUISound(dynamiteSound);
    }

    private IEnumerator MoveConsumedItemsToResult(List<MergeItem> consumedItems, MergeItem resultItem)
    {
        if (consumedItems == null || consumedItems.Count == 0 || resultItem == null)
        {
            yield break;
        }

        var resultTransform = resultItem.transform as RectTransform;
        if (resultTransform == null)
        {
            yield break;
        }

        var movingItems = new List<MovingMergeItem>(consumedItems.Count);
        var targetWorldPosition = resultTransform.position;

        for (var i = 0; i < consumedItems.Count; i++)
        {
            var item = consumedItems[i];
            if (item == null || item.transform is not RectTransform itemRectTransform)
            {
                continue;
            }

            var canvasGroup = item.GetComponent<CanvasGroup>();
            movingItems.Add(new MovingMergeItem
            {
                Item = item,
                RectTransform = itemRectTransform,
                CanvasGroup = canvasGroup,
                StartWorldPosition = itemRectTransform.position,
                StartScale = itemRectTransform.localScale,
                StartAlpha = canvasGroup != null ? canvasGroup.alpha : 1f
            });

            itemRectTransform.SetParent(boardRoot, true);
            itemRectTransform.SetAsLastSibling();
        }

        if (movingItems.Count == 0)
        {
            yield break;
        }

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, connectedMergeMoveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            for (var i = 0; i < movingItems.Count; i++)
            {
                var movingItem = movingItems[i];
                if (movingItem.RectTransform == null)
                {
                    continue;
                }

                movingItem.RectTransform.position = Vector3.LerpUnclamped(movingItem.StartWorldPosition, targetWorldPosition, easedProgress);
                movingItem.RectTransform.localScale = Vector3.LerpUnclamped(
                    movingItem.StartScale,
                    movingItem.StartScale * connectedMergeEndScale,
                    easedProgress);

                if (movingItem.CanvasGroup != null)
                {
                    movingItem.CanvasGroup.alpha = Mathf.Lerp(movingItem.StartAlpha, connectedMergeEndAlpha, easedProgress);
                }
            }

            yield return null;
        }

        for (var i = 0; i < movingItems.Count; i++)
        {
            var movingItem = movingItems[i];
            if (movingItem.RectTransform != null)
            {
                movingItem.RectTransform.position = targetWorldPosition;
            }
        }
    }

    private static void DestroyConsumedItems(List<MergeItem> consumedItems)
    {
        if (consumedItems == null)
        {
            return;
        }

        for (var i = 0; i < consumedItems.Count; i++)
        {
            var item = consumedItems[i];
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        consumedItems.Clear();
    }

    private bool TryCollectConnectedMergeGroup(BoardCell mergeResultCell, List<BoardCell> connectedCells, out MergeItemData nextLevelItem)
    {
        nextLevelItem = null;

        var resultItem = mergeResultCell != null ? mergeResultCell.CurrentItem : null;
        var resultData = resultItem != null ? resultItem.Data : null;
        if (resultData == null || resultData.IsSpider || resultData.ReactToAdjacentMerge || resultData.NextLevelItem == null)
        {
            return false;
        }

        if (!CanCreateMergeResultForOrders(resultData, resultData.NextLevelItem))
        {
            return false;
        }

        CollectConnectedMatchingCells(mergeResultCell, resultData, connectedCells);
        if (connectedCells.Count < RequiredConnectedMergeItemCount)
        {
            return false;
        }

        nextLevelItem = resultData.NextLevelItem;
        return true;
    }

    private bool CanCreateMergeResultForOrders(MergeItemData sourceItem, MergeItemData resultItem)
    {
        if (orderManager == null)
        {
            orderManager = FindFirstObjectByType<OrderManager>();
        }

        return orderManager == null || orderManager.CanCreateMergeResult(sourceItem, resultItem);
    }

    private void ApplyAdjacentMergeReaction(BoardCell cell)
    {
        var item = cell != null ? cell.CurrentItem : null;
        var itemData = item != null ? item.Data : null;

        if (itemData == null || !itemData.ReactToAdjacentMerge)
        {
            return;
        }

        // Adjacent merge reaction: blockers advance through their data chain, then disappear when the chain ends.
        if (itemData.NextLevelItem != null)
        {
            item.SetData(itemData.NextLevelItem, true);
            item.PlayMergePopEffect();
            return;
        }

        item.PlayDisappearEffect();
        cell.Clear();
        NotifyItemDestroyed(itemData);
        Destroy(item.gameObject);
    }

    private void NotifyItemDestroyed(MergeItemData itemData)
    {
        if (itemData == null)
        {
            return;
        }

        if (orderManager == null)
        {
            orderManager = FindFirstObjectByType<OrderManager>();
        }

        if (orderManager != null)
        {
            orderManager.RegisterItemDestroyed(itemData);
        }
    }

    private void CollectSpidersOnBoard(List<MergeItem> result)
    {
        result.Clear();

        if (cells == null)
        {
            return;
        }

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                var item = GetItemAt(x, y);
                if (IsSpiderItem(item))
                {
                    result.Add(item);
                }
            }
        }
    }

    private void GetFreeOrthogonalNeighbors(BoardCell centerCell, List<BoardCell> result)
    {
        result.Clear();

        if (centerCell == null)
        {
            return;
        }

        AddFreeSpiderNeighbor(centerCell.X + 1, centerCell.Y, result);
        AddFreeSpiderNeighbor(centerCell.X - 1, centerCell.Y, result);
        AddFreeSpiderNeighbor(centerCell.X, centerCell.Y + 1, result);
        AddFreeSpiderNeighbor(centerCell.X, centerCell.Y - 1, result);
    }

    private void AddFreeSpiderNeighbor(int x, int y, List<BoardCell> result)
    {
        var cell = GetCell(x, y);
        if (IsCellFreeForSpider(cell))
        {
            result.Add(cell);
        }
    }

    private bool IsCellFreeForSpider(BoardCell cell)
    {
        return cell != null && ContainsCell(cell) && cell.IsEmpty();
    }

    private bool IsActiveSpiderOnBoard(MergeItem spider)
    {
        return IsSpiderItem(spider)
            && spider.CurrentCell != null
            && ContainsCell(spider.CurrentCell)
            && spider.CurrentCell.CurrentItem == spider;
    }

    private static bool IsSpiderItem(MergeItem item)
    {
        return item != null && item.Data != null && item.Data.IsSpider;
    }

    private void MoveSpider(MergeItem spider, BoardCell targetCell)
    {
        if (!IsActiveSpiderOnBoard(spider) || !IsCellFreeForSpider(targetCell))
        {
            return;
        }

        var startWorldPosition = spider.transform.position;
        targetCell.SetItem(spider);
        PlaySpiderMoveFromWorldPosition(spider, startWorldPosition);
    }

    private void RemoveSpider(MergeItem spider, string reason)
    {
        if (!IsActiveSpiderOnBoard(spider))
        {
            return;
        }

        var spiderData = spider.Data;
        var spiderCell = spider.CurrentCell;

        if (reason == SpiderRemovedByTrapReason)
        {
            spider.PlayDisappearEffect();
        }

        spiderCell.Clear();
        NotifyItemDestroyed(spiderData);
        Destroy(spider.gameObject);
        Debug.Log($"{nameof(BoardManager)} removed spider: {reason}.", this);
    }

    private void PlaySpiderMoveFromWorldPosition(MergeItem spider, Vector3 startWorldPosition)
    {
        if (!animateSpiderMovement || spiderMoveDuration <= 0f || spider == null || spider.transform is not RectTransform spiderRectTransform)
        {
            return;
        }

        var targetWorldPosition = spiderRectTransform.position;
        if ((targetWorldPosition - startWorldPosition).sqrMagnitude < 0.01f)
        {
            return;
        }

        spiderRectTransform.position = startWorldPosition;
        StartCoroutine(MoveSpiderToCellCoroutine(spiderRectTransform, startWorldPosition, targetWorldPosition));
    }

    private IEnumerator MoveSpiderToCellCoroutine(RectTransform spiderRectTransform, Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, spiderMoveDuration);

        while (elapsed < duration)
        {
            if (spiderRectTransform == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            spiderRectTransform.position = Vector3.LerpUnclamped(startWorldPosition, targetWorldPosition, easedProgress);
            yield return null;
        }

        if (spiderRectTransform != null)
        {
            spiderRectTransform.position = targetWorldPosition;
            spiderRectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private MergeItemData GetRandomWeightedSpawnableItem()
    {
        if (weightedSpawnableItems == null || weightedSpawnableItems.Count == 0)
        {
            return null;
        }

        var totalWeight = 0;
        for (var i = 0; i < weightedSpawnableItems.Count; i++)
        {
            var item = weightedSpawnableItems[i];
            if (item != null && item.ItemData != null && item.Weight > 0)
            {
                totalWeight += item.Weight;
            }
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning($"{nameof(BoardManager)} has weighted spawnable items, but their total weight is 0.", this);
            return null;
        }

        var randomWeight = Random.Range(0, totalWeight);
        for (var i = 0; i < weightedSpawnableItems.Count; i++)
        {
            var item = weightedSpawnableItems[i];
            if (item == null || item.ItemData == null || item.Weight <= 0)
            {
                continue;
            }

            if (randomWeight < item.Weight)
            {
                return item.ItemData;
            }

            randomWeight -= item.Weight;
        }

        return null;
    }

    private bool HasValidSpawnableItems()
    {
        if (weightedSpawnableItems != null)
        {
            for (var i = 0; i < weightedSpawnableItems.Count; i++)
            {
                var item = weightedSpawnableItems[i];
                if (item != null && item.ItemData != null && item.Weight > 0)
                {
                    return true;
                }
            }
        }

        if (spawnableItems != null)
        {
            for (var i = 0; i < spawnableItems.Count; i++)
            {
                if (spawnableItems[i] != null)
                {
                    return true;
                }
            }
        }

        return false;
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
