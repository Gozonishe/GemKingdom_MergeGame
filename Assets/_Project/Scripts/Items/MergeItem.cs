using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class MergeItem : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private MergeItemData data;

    [Header("Visuals")]
    [SerializeField] private Image iconImage;

    [Header("Runtime State")]
    [field: SerializeField] public BoardCell CurrentCell { get; private set; }

    public MergeItemData Data => data;
    public int Level => data != null ? data.Level : 0;

    private void Awake()
    {
        iconImage = iconImage != null ? iconImage : GetComponent<Image>();
        RefreshVisuals();
    }

    private void Reset()
    {
        iconImage = GetComponent<Image>();
    }

    private void OnValidate()
    {
        iconImage = iconImage != null ? iconImage : GetComponent<Image>();
        RefreshVisuals();
    }

    public void SetData(MergeItemData itemData)
    {
        data = itemData;
        RefreshVisuals();
    }

    public int GetLevel()
    {
        return Level;
    }

    public bool CanMergeWith(MergeItem otherItem)
    {
        return otherItem != null
            && data != null
            && data.CanMergeToNextLevel
            && data == otherItem.Data;
    }

    public void SetCell(BoardCell cell)
    {
        CurrentCell = cell;
    }

    public void RefreshVisuals()
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = data != null ? data.Icon : null;
    }
}
