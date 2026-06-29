using UnityEngine;
using UnityEngine.UI;

public sealed class BoardCell : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int x;
    [SerializeField] private int y;

    [Header("UI")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private bool dimBackgroundWhenOccupied = true;
    [SerializeField, Range(0f, 1f)] private float occupiedDarkenAmount = 0.333f;
    [SerializeField] private GameObject dropHighlightObject;

    [Header("Runtime Item")]
    [SerializeField] private MergeItem currentItem;

    private Color emptyBackgroundColor = Color.white;
    private bool hasCachedBackgroundColor;

    public int X => x;
    public int Y => y;
    public Vector2Int GridPosition => new Vector2Int(x, y);
    public RectTransform RectTransform => rectTransform;
    public Transform ItemRoot => itemRoot != null ? itemRoot : rectTransform;
    public MergeItem CurrentItem => currentItem;

    private void Awake()
    {
        CacheRectTransform();
        CacheItemRoot();
        CacheBackgroundImage();
        CacheDropHighlightObject();
        RefreshBackgroundColor();
        SetDropHighlightActive(false);
    }

    private void Reset()
    {
        CacheRectTransform();
        CacheItemRoot();
        CacheBackgroundImage();
        CacheDropHighlightObject();
        RefreshBackgroundColor();
        SetDropHighlightActive(false);
    }

    private void OnValidate()
    {
        CacheRectTransform();
        CacheItemRoot();
        CacheBackgroundImage();
        CacheDropHighlightObject();
        RefreshBackgroundColor();
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
            RefreshBackgroundColor();
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
            RefreshBackgroundColor();
            return;
        }

        currentItem.SetCell(this);
        AttachItemToCell(currentItem);
        RefreshBackgroundColor();
    }

    public MergeItem RemoveItem()
    {
        var removedItem = currentItem;
        currentItem = null;

        if (removedItem != null)
        {
            removedItem.SetCell(null);
        }

        RefreshBackgroundColor();
        return removedItem;
    }

    public void Clear()
    {
        RemoveItem();
    }

    public void SetDropHighlightActive(bool isActive)
    {
        if (dropHighlightObject != null && dropHighlightObject.activeSelf != isActive)
        {
            dropHighlightObject.SetActive(isActive);
        }
    }

    private void CacheRectTransform()
    {
        rectTransform = rectTransform != null ? rectTransform : transform as RectTransform;
    }

    private void CacheItemRoot()
    {
        if (itemRoot != null)
        {
            return;
        }

        var itemRootTransform = transform.Find("ItemRoot");
        if (itemRootTransform != null)
        {
            itemRoot = itemRootTransform as RectTransform;
        }
    }

    private void CacheBackgroundImage()
    {
        backgroundImage = backgroundImage != null ? backgroundImage : GetComponent<Image>();

        if (backgroundImage != null && !Application.isPlaying)
        {
            emptyBackgroundColor = backgroundImage.color;
            hasCachedBackgroundColor = true;
        }

        if (backgroundImage != null && !hasCachedBackgroundColor)
        {
            emptyBackgroundColor = backgroundImage.color;
            hasCachedBackgroundColor = true;
        }
    }

    private void CacheDropHighlightObject()
    {
        if (dropHighlightObject != null)
        {
            return;
        }

        var lightTransform = transform.Find("light");
        if (lightTransform == null)
        {
            lightTransform = transform.Find("Light");
        }

        if (lightTransform != null)
        {
            dropHighlightObject = lightTransform.gameObject;
        }
    }

    private void RefreshBackgroundColor()
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (!hasCachedBackgroundColor)
        {
            emptyBackgroundColor = backgroundImage.color;
            hasCachedBackgroundColor = true;
        }

        backgroundImage.color = currentItem != null && dimBackgroundWhenOccupied
            ? GetDarkenedColor(emptyBackgroundColor, occupiedDarkenAmount)
            : emptyBackgroundColor;
    }

    private static Color GetDarkenedColor(Color sourceColor, float darkenAmount)
    {
        var multiplier = 1f - Mathf.Clamp01(darkenAmount);
        return new Color(
            sourceColor.r * multiplier,
            sourceColor.g * multiplier,
            sourceColor.b * multiplier,
            sourceColor.a);
    }

    private void AttachItemToCell(MergeItem item)
    {
        if (item == null || rectTransform == null)
        {
            return;
        }

        var itemTransform = item.transform;
        itemTransform.SetParent(ItemRoot, false);
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
