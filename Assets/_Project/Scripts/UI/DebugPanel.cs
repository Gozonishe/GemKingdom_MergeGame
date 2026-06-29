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
    [FormerlySerializedAs("completeRandomOrderButton")]
    [SerializeField] private Button previousLevelButton;
    [FormerlySerializedAs("refillBoardButton")]
    [SerializeField] private Button resetProgressButton;
    [FormerlySerializedAs("resetBoardButton")]
    [SerializeField] private Button nextLevelButton;

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

    private void LoadPreviousLevel()
    {
        ResolveReferences();
        levelManager?.LoadPreviousLevel();
    }

    private void ResetProgress()
    {
        ResolveReferences();
        currencyManager?.ResetCurrencies();
        levelManager?.ResetProgressAndLoadFirstLevel();
    }

    private void LoadNextLevel()
    {
        ResolveReferences();
        levelManager?.LoadNextLevel();
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

        if (previousLevelButton != null)
        {
            previousLevelButton.onClick.AddListener(LoadPreviousLevel);
        }

        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.AddListener(ResetProgress);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(LoadNextLevel);
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

        if (previousLevelButton != null)
        {
            previousLevelButton.onClick.RemoveListener(LoadPreviousLevel);
        }

        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.RemoveListener(ResetProgress);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(LoadNextLevel);
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

        if (previousLevelButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(previousLevelButton)}.", this);
        }

        if (resetProgressButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(resetProgressButton)}.", this);
        }

        if (nextLevelButton == null)
        {
            Debug.LogError($"{nameof(DebugPanel)} on '{name}' is missing {nameof(nextLevelButton)}.", this);
        }
    }
}
