using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class DebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private CurrencyManager currencyManager;

    [Header("Buttons")]
    [SerializeField] private Button addEnergyButton;
    [SerializeField] private Button shuffleBoardButton;
    [SerializeField] private Button completeRandomOrderButton;
    [FormerlySerializedAs("refillBoardButton")]
    [SerializeField] private Button resetProgressButton;
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

    private void ResetProgress()
    {
        ResolveReferences();
        currencyManager?.ResetCurrencies();
        levelManager?.ResetProgressAndLoadFirstLevel();
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

        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.AddListener(ResetProgress);
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

        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.RemoveListener(ResetProgress);
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

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (currencyManager == null)
        {
            currencyManager = FindFirstObjectByType<CurrencyManager>();
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

        if (resetProgressButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(resetProgressButton)}.", this);
        }

        if (resetBoardButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(resetBoardButton)}.", this);
        }
    }
}
