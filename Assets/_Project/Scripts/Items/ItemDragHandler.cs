using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private MergeItem item;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BottomItemTrayController bottomItemTrayController;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private RectTransform dragRoot;

    [Header("Drag Settings")]
    [SerializeField] private bool returnToStartOnInvalidDrop = true;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private BoardCell sourceCell;
    private Transform startParent;
    private int startSiblingIndex;
    private Vector2 startAnchoredPosition;
    private bool isDragging;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        item = item != null ? item : GetComponent<MergeItem>();
        ResolveReferences();
        parentCanvas = parentCanvas != null ? parentCanvas : GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveReferences();

        if (boardManager == null)
        {
            Debug.LogError($"{nameof(ItemDragHandler)} on '{name}' cannot start drag because {nameof(boardManager)} is not assigned and no {nameof(BoardManager)} was found.", this);
            isDragging = false;
            return;
        }

        if (boardManager != null && boardManager.IsBusy)
        {
            isDragging = false;
            return;
        }

        if (item == null || rectTransform == null)
        {
            isDragging = false;
            return;
        }

        isDragging = true;
        sourceCell = item.CurrentCell;
        startParent = rectTransform.parent;
        startSiblingIndex = rectTransform.GetSiblingIndex();
        startAnchoredPosition = rectTransform.anchoredPosition;

        var root = GetDragRoot();
        rectTransform.SetParent(root, false);
        rectTransform.SetAsLastSibling();
        SetBlocksRaycasts(false);
        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        MoveToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        SetBlocksRaycasts(true);

        if (item == null || sourceCell == null)
        {
            ReturnToSourceCell();
            return;
        }

        var targetCell = FindTargetCell(eventData);

        if (CanUseTrayDestroyItemOnBoardTarget(targetCell))
        {
            var sourceWasGeneratedSlot = IsGeneratedSlot(sourceCell);

            if (bottomItemTrayController == null || !bottomItemTrayController.TrySpendMoveForBoardPlacement())
            {
                ReturnToSourceCell();
                sourceCell = null;
                startParent = null;
                return;
            }

            if (boardManager == null || !boardManager.TryUseDestroyBothItemOnCell(item, targetCell))
            {
                ReturnToSourceCell();
            }
            else if (sourceWasGeneratedSlot && bottomItemTrayController != null)
            {
                bottomItemTrayController.GenerateFreeItemInGeneratedSlot();
            }
        }
        else if (CanDropFromTray(targetCell))
        {
            var sourceWasGeneratedSlot = IsGeneratedSlot(sourceCell);
            var targetIsBoardCell = IsBoardCell(targetCell);
            var targetIsFrozenSlot = IsFrozenSlot(targetCell);

            if (targetIsBoardCell
                && (bottomItemTrayController == null || !bottomItemTrayController.TrySpendMoveForBoardPlacement()))
            {
                ReturnToSourceCell();
                sourceCell = null;
                startParent = null;
                return;
            }

            targetCell.SetItem(item);

            if (targetIsBoardCell && boardManager != null)
            {
                var didMerge = boardManager.TryMergeWithAdjacentItem(targetCell);
                if (!didMerge)
                {
                    boardManager.RefreshOrders();
                }

                if (sourceWasGeneratedSlot && bottomItemTrayController != null)
                {
                    bottomItemTrayController.GenerateFreeItemInGeneratedSlot();
                }
            }
            else if (sourceWasGeneratedSlot && targetIsFrozenSlot && bottomItemTrayController != null)
            {
                bottomItemTrayController.GenerateFreeItemInGeneratedSlot();
            }
        }
        else if (CanDropBoardItemForAdjacentMerge(targetCell))
        {
            targetCell.SetItem(item);

            if (boardManager == null || !boardManager.TryMergeWithAdjacentItem(targetCell))
            {
                ReturnToSourceCell();
            }
        }
        else
        {
            ReturnToSourceCell();
        }

        sourceCell = null;
        startParent = null;
    }

    private bool CanDropFromTray(BoardCell targetCell)
    {
        if (sourceCell == null || targetCell == null || targetCell == sourceCell || item == null)
        {
            return false;
        }

        if (!IsTraySlot(sourceCell))
        {
            return false;
        }

        if (IsBoardCell(targetCell))
        {
            return targetCell.IsEmpty() && !IsDestroyBothItem(item);
        }

        return IsGeneratedSlot(sourceCell)
            && IsFrozenSlot(targetCell)
            && targetCell.IsEmpty();
    }

    private bool CanDropBoardItemForAdjacentMerge(BoardCell targetCell)
    {
        return sourceCell != null
            && targetCell != null
            && targetCell != sourceCell
            && item != null
            && boardManager != null
            && IsBoardCell(sourceCell)
            && IsBoardCell(targetCell)
            && targetCell.IsEmpty();
    }

    private bool CanUseTrayDestroyItemOnBoardTarget(BoardCell targetCell)
    {
        return sourceCell != null
            && targetCell != null
            && targetCell != sourceCell
            && item != null
            && IsTraySlot(sourceCell)
            && IsBoardCell(targetCell)
            && !targetCell.IsEmpty()
            && IsDestroyBothItem(item);
    }

    private BoardCell FindTargetCell(PointerEventData eventData)
    {
        ResolveReferences();

        var uiCamera = GetUICamera(eventData);
        var trayCell = bottomItemTrayController != null
            ? bottomItemTrayController.GetSlotAtScreenPosition(eventData.position, uiCamera)
            : null;

        if (trayCell != null)
        {
            return trayCell;
        }

        return boardManager != null
            ? boardManager.GetCellAtScreenPosition(eventData.position, uiCamera)
            : null;
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        var root = GetDragRoot();
        if (root == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, GetUICamera(eventData), out var localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    private void ReturnToSourceCell()
    {
        if (!returnToStartOnInvalidDrop || rectTransform == null)
        {
            return;
        }

        if (sourceCell != null)
        {
            sourceCell.SetItem(item);
            return;
        }

        if (startParent != null)
        {
            rectTransform.SetParent(startParent, false);
            rectTransform.SetSiblingIndex(startSiblingIndex);
            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }

    private RectTransform GetDragRoot()
    {
        if (dragRoot != null)
        {
            return dragRoot;
        }

        if (parentCanvas != null)
        {
            return parentCanvas.transform as RectTransform;
        }

        return startParent as RectTransform;
    }

    private Camera GetUICamera(PointerEventData eventData)
    {
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return eventData.pressEventCamera != null ? eventData.pressEventCamera : parentCanvas.worldCamera;
    }

    private void SetBlocksRaycasts(bool blocksRaycasts)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = blocksRaycasts;
        }
    }

    private void ResolveReferences()
    {
        if (boardManager == null)
        {
            boardManager = GetComponentInParent<BoardManager>();
        }

        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        if (bottomItemTrayController == null)
        {
            bottomItemTrayController = GetComponentInParent<BottomItemTrayController>();
        }

        if (bottomItemTrayController == null)
        {
            bottomItemTrayController = FindFirstObjectByType<BottomItemTrayController>(FindObjectsInactive.Include);
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
    }

    private bool IsBoardCell(BoardCell cell)
    {
        return boardManager != null && boardManager.ContainsCell(cell);
    }

    private bool IsTraySlot(BoardCell cell)
    {
        return bottomItemTrayController != null && bottomItemTrayController.IsTraySlot(cell);
    }

    private bool IsGeneratedSlot(BoardCell cell)
    {
        return bottomItemTrayController != null && bottomItemTrayController.IsGeneratedItemSlot(cell);
    }

    private bool IsFrozenSlot(BoardCell cell)
    {
        return bottomItemTrayController != null && bottomItemTrayController.IsFrozenItemSlot(cell);
    }

    private static bool IsDestroyBothItem(MergeItem targetItem)
    {
        return targetItem != null
            && targetItem.Data != null
            && targetItem.Data.DestroyBothOnAnyNeighborMerge;
    }

    private static bool AreNeighborCells(BoardCell firstCell, BoardCell secondCell)
    {
        var deltaX = Mathf.Abs(firstCell.X - secondCell.X);
        var deltaY = Mathf.Abs(firstCell.Y - secondCell.Y);
        return deltaX + deltaY == 1;
    }
}
