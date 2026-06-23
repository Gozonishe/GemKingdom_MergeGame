using UnityEngine;
using UnityEngine.UI;

public sealed class DebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private OrderManager orderManager;

    [Header("Buttons")]
    [SerializeField] private Button addEnergyButton;
    [SerializeField] private Button shuffleBoardButton;
    [SerializeField] private Button completeRandomOrderButton;
    [SerializeField] private Button refillBoardButton;
    [SerializeField] private Button resetBoardButton;

    [Header("Debug Values")]
    [SerializeField] private int addEnergyAmount = 10;

    private void Awake()
    {
        ResolveReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        SubscribeButtons();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    private void AddEnergy()
    {
        ResolveReferences();
        energyManager?.AddEnergy(addEnergyAmount);
    }

    private void ShuffleBoard()
    {
        ResolveReferences();
        boardManager?.ShuffleBoard();
    }

    private void CompleteRandomOrder()
    {
        ResolveReferences();
        orderManager?.CompleteRandomOrderDebug();
    }

    private void RefillBoard()
    {
        ResolveReferences();
        boardManager?.RefillBoard();
    }

    private void ResetBoard()
    {
        ResolveReferences();
        boardManager?.ResetBoard();
    }

    private void SubscribeButtons()
    {
        if (addEnergyButton != null)
        {
            addEnergyButton.onClick.AddListener(AddEnergy);
        }

        if (shuffleBoardButton != null)
        {
            shuffleBoardButton.onClick.AddListener(ShuffleBoard);
        }

        if (completeRandomOrderButton != null)
        {
            completeRandomOrderButton.onClick.AddListener(CompleteRandomOrder);
        }

        if (refillBoardButton != null)
        {
            refillBoardButton.onClick.AddListener(RefillBoard);
        }

        if (resetBoardButton != null)
        {
            resetBoardButton.onClick.AddListener(ResetBoard);
        }
    }

    private void UnsubscribeButtons()
    {
        if (addEnergyButton != null)
        {
            addEnergyButton.onClick.RemoveListener(AddEnergy);
        }

        if (shuffleBoardButton != null)
        {
            shuffleBoardButton.onClick.RemoveListener(ShuffleBoard);
        }

        if (completeRandomOrderButton != null)
        {
            completeRandomOrderButton.onClick.RemoveListener(CompleteRandomOrder);
        }

        if (refillBoardButton != null)
        {
            refillBoardButton.onClick.RemoveListener(RefillBoard);
        }

        if (resetBoardButton != null)
        {
            resetBoardButton.onClick.RemoveListener(ResetBoard);
        }
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

        if (orderManager == null)
        {
            orderManager = FindFirstObjectByType<OrderManager>();
        }
    }

    private void ValidateReferences()
    {
        if (addEnergyButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(addEnergyButton)}.", this);
        }

        if (shuffleBoardButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(shuffleBoardButton)}.", this);
        }

        if (completeRandomOrderButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(completeRandomOrderButton)}.", this);
        }

        if (refillBoardButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(refillBoardButton)}.", this);
        }

        if (resetBoardButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(resetBoardButton)}.", this);
        }
    }
}
