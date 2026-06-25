using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private MergeItem item;
    [SerializeField] private BoardManager boardManager;
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
        boardManager = boardManager != null ? boardManager : GetComponentInParent<BoardManager>();
        parentCanvas = parentCanvas != null ? parentCanvas : GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (boardManager == null)
        {
            boardManager = GetComponentInParent<BoardManager>();
        }

        if (boardManager == null)
        {
            Debug.LogError($"{nameof(ItemDragHandler)} on '{name}' cannot start drag because {nameof(boardManager)} is not assigned and no parent {nameof(BoardManager)} was found.", this);
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
        var targetItem = targetCell != null ? targetCell.CurrentItem : null;

        if (!CanDropMerge(targetCell, targetItem) || boardManager == null || !boardManager.TryMerge(item, targetItem))
        {
            ReturnToSourceCell();
        }

        sourceCell = null;
        startParent = null;
    }

    private bool CanDropMerge(BoardCell targetCell, MergeItem targetItem)
    {
        return targetCell != null
            && sourceCell != null
            && targetCell != sourceCell
            && AreNeighborCells(sourceCell, targetCell)
            && targetItem != null
            && item != null
            && (boardManager != null ? boardManager.CanMerge(sourceCell, targetCell) : item.CanMergeWith(targetItem));
    }

    private BoardCell FindTargetCell(PointerEventData eventData)
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        return boardManager != null
            ? boardManager.GetCellAtScreenPosition(eventData.position, GetUICamera(eventData))
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

    private static bool AreNeighborCells(BoardCell firstCell, BoardCell secondCell)
    {
        var deltaX = Mathf.Abs(firstCell.X - secondCell.X);
        var deltaY = Mathf.Abs(firstCell.Y - secondCell.Y);
        return deltaX + deltaY == 1;
    }
}
