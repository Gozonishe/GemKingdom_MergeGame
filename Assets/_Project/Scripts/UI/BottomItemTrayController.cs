using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BottomItemTrayController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button generateButton;
    [SerializeField] private BoardCell generatedItemSlot;
    [SerializeField] private BoardCell frozenItemSlot;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnergyManager energyManager;

    [Header("Generator Balance")]
    [SerializeField] private List<SpawnableItemDefinition> generatorSpawnableItems = new List<SpawnableItemDefinition>();

    [Header("Generated Item Spawn Animation")]
    [SerializeField] private bool animateGeneratedItemSpawn = true;
    [SerializeField, Range(0.1f, 1f)] private float generatedItemStartScale = 0.72f;
    [SerializeField, Range(1f, 1.5f)] private float generatedItemOvershootScale = 1.08f;
    [SerializeField, Min(0f)] private float generatedItemGrowDuration = 0.14f;
    [SerializeField, Min(0f)] private float generatedItemSettleDuration = 0.1f;

    public BoardCell GeneratedItemSlot => generatedItemSlot;
    public BoardCell FrozenItemSlot => frozenItemSlot;

    public bool IsGeneratedItemSlot(BoardCell cell)
    {
        return cell != null && cell == generatedItemSlot;
    }

    public bool IsFrozenItemSlot(BoardCell cell)
    {
        return cell != null && cell == frozenItemSlot;
    }

    public bool IsTraySlot(BoardCell cell)
    {
        return IsGeneratedItemSlot(cell) || IsFrozenItemSlot(cell);
    }

    public BoardCell GetSlotAtScreenPosition(Vector2 screenPosition, Camera uiCamera)
    {
        if (ContainsScreenPoint(generatedItemSlot, screenPosition, uiCamera))
        {
            return generatedItemSlot;
        }

        if (ContainsScreenPoint(frozenItemSlot, screenPosition, uiCamera))
        {
            return frozenItemSlot;
        }

        return null;
    }

    private void Awake()
    {
        ResolveReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(GenerateItem);
        }
    }

    private void OnDisable()
    {
        if (generateButton != null)
        {
            generateButton.onClick.RemoveListener(GenerateItem);
        }
    }

    public void SetGeneratorSpawnableItems(IReadOnlyList<SpawnableItemDefinition> items)
    {
        generatorSpawnableItems.Clear();

        if (items == null)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item != null && item.ItemData != null && item.Weight > 0)
            {
                generatorSpawnableItems.Add(item);
            }
        }
    }

    public void PrepareForLevel(IReadOnlyList<SpawnableItemDefinition> items)
    {
        SetGeneratorSpawnableItems(items);
        ClearSlot(generatedItemSlot);
        ClearSlot(frozenItemSlot);
        GenerateInitialItem();
    }

    public void GenerateItem()
    {
        ResolveReferences();

        if (boardManager == null || energyManager == null || generatedItemSlot == null)
        {
            Debug.LogError($"{nameof(BottomItemTrayController)} on '{name}' cannot generate item because required references are missing.", this);
            return;
        }

        if (!energyManager.HasEnoughEnergy())
        {
            Debug.Log($"{nameof(BottomItemTrayController)}: no moves left.", this);
            return;
        }

        var itemData = GetRandomGeneratorItem();
        if (itemData == null)
        {
            Debug.LogWarning($"{nameof(BottomItemTrayController)} on '{name}' has no generator items configured.", this);
            return;
        }

        if (!energyManager.SpendEnergy())
        {
            return;
        }

        var generatedItem = boardManager.CreateItemInCell(generatedItemSlot, itemData, true);
        PlayGeneratedItemSpawnAnimation(generatedItem);
    }

    public bool TrySpendMoveForBoardPlacement()
    {
        ResolveReferences();

        if (energyManager == null)
        {
            Debug.LogError($"{nameof(BottomItemTrayController)} on '{name}' cannot spend move because {nameof(energyManager)} is missing.", this);
            return false;
        }

        if (!energyManager.HasEnoughEnergy())
        {
            Debug.Log($"{nameof(BottomItemTrayController)}: no moves left.", this);
            return false;
        }

        return energyManager.SpendEnergy();
    }

    public void GenerateFreeItemInGeneratedSlot()
    {
        GenerateInitialItem();
    }

    private void GenerateInitialItem()
    {
        ResolveReferences();

        if (boardManager == null || generatedItemSlot == null)
        {
            Debug.LogError($"{nameof(BottomItemTrayController)} on '{name}' cannot create initial item because required references are missing.", this);
            return;
        }

        var itemData = GetRandomGeneratorItem();
        if (itemData == null)
        {
            Debug.LogWarning($"{nameof(BottomItemTrayController)} on '{name}' cannot create initial item because no generator items are configured.", this);
            return;
        }

        var generatedItem = boardManager.CreateItemInCell(generatedItemSlot, itemData, true);
        PlayGeneratedItemSpawnAnimation(generatedItem);
    }

    private void PlayGeneratedItemSpawnAnimation(MergeItem item)
    {
        if (!animateGeneratedItemSpawn || item == null)
        {
            return;
        }

        item.PlaySpawnBounceEffect(
            generatedItemStartScale,
            generatedItemOvershootScale,
            generatedItemGrowDuration,
            generatedItemSettleDuration);
    }

    private void ClearSlot(BoardCell slot)
    {
        if (slot == null || slot.IsEmpty())
        {
            return;
        }

        var existingItem = slot.RemoveItem();
        if (existingItem != null)
        {
            Destroy(existingItem.gameObject);
        }
    }

    private MergeItemData GetRandomGeneratorItem()
    {
        if (generatorSpawnableItems == null || generatorSpawnableItems.Count == 0)
        {
            return null;
        }

        var totalWeight = 0;
        for (var i = 0; i < generatorSpawnableItems.Count; i++)
        {
            var item = generatorSpawnableItems[i];
            if (item != null && item.ItemData != null && item.Weight > 0)
            {
                totalWeight += item.Weight;
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        var randomWeight = Random.Range(0, totalWeight);
        for (var i = 0; i < generatorSpawnableItems.Count; i++)
        {
            var item = generatorSpawnableItems[i];
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

    private void ResolveReferences()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        if (energyManager == null)
        {
            energyManager = FindFirstObjectByType<EnergyManager>();
        }
    }

    private void ValidateReferences()
    {
        if (generateButton == null)
        {
            Debug.LogError($"{nameof(BottomItemTrayController)} on '{name}' is missing {nameof(generateButton)}.", this);
        }

        if (generatedItemSlot == null)
        {
            Debug.LogError($"{nameof(BottomItemTrayController)} on '{name}' is missing {nameof(generatedItemSlot)}.", this);
        }

        if (frozenItemSlot == null)
        {
            Debug.LogError($"{nameof(BottomItemTrayController)} on '{name}' is missing {nameof(frozenItemSlot)}.", this);
        }
    }

    private static bool ContainsScreenPoint(BoardCell cell, Vector2 screenPosition, Camera uiCamera)
    {
        return cell != null
            && cell.RectTransform != null
            && RectTransformUtility.RectangleContainsScreenPoint(cell.RectTransform, screenPosition, uiCamera);
    }
}
