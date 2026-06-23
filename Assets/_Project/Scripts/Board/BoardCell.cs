using UnityEngine;

public sealed class BoardCell : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int x;
    [SerializeField] private int y;

    [Header("UI")]
    [SerializeField] private RectTransform rectTransform;

    [Header("Runtime Item")]
    [SerializeField] private MergeItem currentItem;

    public int X => x;
    public int Y => y;
    public Vector2Int GridPosition => new Vector2Int(x, y);
    public RectTransform RectTransform => rectTransform;
    public Transform ItemRoot => rectTransform;
    public MergeItem CurrentItem => currentItem;

    private void Awake()
    {
        CacheRectTransform();
    }

    private void Reset()
    {
        CacheRectTransform();
    }

    private void OnValidate()
    {
        CacheRectTransform();
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public void Setup(int cellX, int cellY)
    {
        x = cellX;
        y = cellY;
        CacheRectTransform();
    }

    public void SetItem(MergeItem item)
    {
        if (currentItem == item)
        {
            AttachItemToCell(item);
            return;
        }

        if (currentItem != null)
        {
            currentItem.SetCell(null);
        }

        if (item != null && item.CurrentCell != null && item.CurrentCell != this)
        {
            item.CurrentCell.RemoveItem();
        }

        currentItem = item;

        if (currentItem == null)
        {
            return;
        }

        currentItem.SetCell(this);
        AttachItemToCell(currentItem);
    }

    public MergeItem RemoveItem()
    {
        var removedItem = currentItem;
        currentItem = null;

        if (removedItem != null)
        {
            removedItem.SetCell(null);
        }

        return removedItem;
    }

    public void Clear()
    {
        RemoveItem();
    }

    private void CacheRectTransform()
    {
        rectTransform = rectTransform != null ? rectTransform : transform as RectTransform;
    }

    private void AttachItemToCell(MergeItem item)
    {
        if (item == null || rectTransform == null)
        {
            return;
        }

        var itemTransform = item.transform;
        itemTransform.SetParent(rectTransform, false);
        itemTransform.localRotation = Quaternion.identity;
        itemTransform.localScale = Vector3.one;

        if (itemTransform is RectTransform itemRectTransform)
        {
            itemRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            itemRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            itemRectTransform.pivot = new Vector2(0.5f, 0.5f);
            itemRectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            itemTransform.localPosition = Vector3.zero;
        }
    }
}
