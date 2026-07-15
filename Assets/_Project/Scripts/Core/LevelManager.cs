using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelManager : MonoBehaviour
{
    private const string SavedLevelIndexKey = "MergeGame.NextLevelIndex";
    private const string DefaultMainMenuSceneName = "MainMenuScreen";
    private const string DefaultLevelsAssetFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string LoosePanelName = "LoosePanel";
    private const string LevelWindowStartName = "LevelWindowStart";
    private const int BuyMovesGoldCost = 900;
    private const int BoughtMovesAmount = 5;
    private const float ShopLivesRefreshInterval = 1f;

    [Header("Levels")]
    [SerializeField] private List<LevelData> levels = new List<LevelData>();
    [SerializeField] private int currentLevelIndex;

    [Header("Editor Setup")]
    [SerializeField] private bool autoFillLevelsFromProjectFolder = true;
    [SerializeField] private string levelsAssetFolder = DefaultLevelsAssetFolder;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private BottomItemTrayController bottomItemTrayController;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private GameObject victoryLevelWindow;
    [SerializeField] private Transform victoryRewardsContainer;
    [SerializeField] private RewardItemView victoryRewardItemPrefab;
    [SerializeField] private Sprite victoryCoinsIcon;
    [SerializeField] private AudioClip victoryOpenSound;
    [SerializeField] private Button victoryContinueButton;
    [SerializeField] private Button victoryWatchAdsButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject looseLevelWindow;
    [SerializeField] private Button buyMovesButton;
    [SerializeField] private GameObject shopScreen;
    [SerializeField] private TMP_Text shopPlayerLivesAmountText;
    [SerializeField] private TMP_Text shopPlayerGoldAmountText;
    [SerializeField] private GameObject loosePopup;
    [SerializeField] private GameObject loosePanel;
    [SerializeField] private GameObject loseLevelWindowStart;
    [SerializeField] private TMP_Text loseLevelWindowHeaderLabel;
    [SerializeField] private RectTransform loseLevelWindowTaskItemsRoot;
    [SerializeField] private GameObject loseLevelWindowTaskItemPrefab;
    [SerializeField] private Button loosePopupQuitButton;
    [SerializeField] private Button loseLevelStartButton;
    [SerializeField] private Button loseLevelExitButton;
    [SerializeField] private Button exitLevelButton;
    [SerializeField] private Button restartLevelButton;
    [SerializeField] private Button giveUpButton;

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;

    private bool isLoadingLevel;
    private bool levelCompleteHandled;
    private bool levelLoseHandled;
    private bool waitingVictoryLevelWindowClose;
    private bool waitingLoosePanelTap;
    private bool lifeConsumedForCurrentLevel;
    private float nextShopLivesRefreshTime;

    public IReadOnlyList<LevelData> Levels => levels;
    public int CurrentLevelIndex => currentLevelIndex;
    public LevelData CurrentLevel => IsInsideLevels(currentLevelIndex) ? levels[currentLevelIndex] : null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoFillLevelsFromProjectFolder)
        {
            FillLevelsFromProjectFolder();
        }
    }
#endif

    public static int GetSavedLevelIndex(int defaultIndex = 0)
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(SavedLevelIndexKey, defaultIndex));
    }

    public static int GetSavedLevelNumber(int defaultIndex = 0)
    {
        return GetSavedLevelIndex(defaultIndex) + 1;
    }

    public static void ResetSavedProgress()
    {
        PlayerPrefs.DeleteKey(SavedLevelIndexKey);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        ResolveReferences();
        RefreshShopPlayerResources();

        if (levels.Count == 0)
        {
            Debug.LogWarning($"{nameof(LevelManager)} on '{name}' has no levels assigned.", this);
            return;
        }

        var savedLevelIndex = GetSavedLevelIndex(currentLevelIndex);
        LoadLevel(Mathf.Clamp(savedLevelIndex, 0, levels.Count - 1));
    }

    private void OnEnable()
    {
        PlayerGold.CoinsChanged += HandlePlayerGoldChanged;
    }

    private void OnDisable()
    {
        PlayerGold.CoinsChanged -= HandlePlayerGoldChanged;
    }

    private void Update()
    {
        RefreshShopPlayerLivesWhileVisible();

        if (waitingLoosePanelTap && IsTapStarted())
        {
            ShowLoseLevelStartWindow();
            return;
        }

        if (isLoadingLevel || levelCompleteHandled || levelLoseHandled || orderManager == null || orderManager.IsAutoClaimingCompletedOrders)
        {
            return;
        }

        if (orderManager.AreAllOrdersClaimed)
        {
            levelCompleteHandled = true;
            CompleteCurrentLevelAndReturnToMenu();
            return;
        }

        if (IsOutOfMoves())
        {
            levelLoseHandled = true;
            ConsumeLifeForFailedAttempt();
            ShowLooseLevelWindow();
        }
    }

    private void OnDestroy()
    {
        RemoveVictoryWindowButtonListeners();

        if (loosePopupQuitButton != null)
        {
            loosePopupQuitButton.onClick.RemoveListener(LoadMainMenuScene);
        }

        if (buyMovesButton != null)
        {
            buyMovesButton.onClick.RemoveListener(BuyMovesAndContinue);
        }

        if (loseLevelStartButton != null)
        {
            loseLevelStartButton.onClick.RemoveListener(RestartLevelFromLoseWindow);
        }

        if (loseLevelExitButton != null)
        {
            loseLevelExitButton.onClick.RemoveListener(ExitLoseWindowToMainMenu);
        }

        if (exitLevelButton != null)
        {
            exitLevelButton.onClick.RemoveListener(ExitLevelToMainMenu);
        }

        if (restartLevelButton != null)
        {
            restartLevelButton.onClick.RemoveListener(RestartCurrentLevelFromWindow);
        }

        if (giveUpButton != null)
        {
            giveUpButton.onClick.RemoveListener(GiveUpCurrentLevel);
        }
    }

    public void LoadLevel(int index)
    {
        ResolveReferences();

        if (!IsInsideLevels(index))
        {
            Debug.LogWarning($"{nameof(LevelManager)} cannot load level index {index}.", this);
            return;
        }

        var level = levels[index];
        if (level == null)
        {
            Debug.LogWarning($"{nameof(LevelManager)} cannot load level index {index} because level data is missing.", this);
            return;
        }

        isLoadingLevel = true;
        currentLevelIndex = index;
        levelCompleteHandled = false;
        levelLoseHandled = false;
        lifeConsumedForCurrentLevel = false;
        waitingVictoryLevelWindowClose = false;
        waitingLoosePanelTap = false;
        SetVictoryLevelWindowActive(false);
        SetLooseLevelWindowActive(false);
        SetShopScreenActive(false);
        SetLoosePopupActive(false);
        SetLoosePanelActive(false);
        SetLoseLevelWindowActive(false);
        RefreshLevelText(level);

        if (energyManager != null)
        {
            energyManager.SetEnergy(level.MovesCount, level.MovesCount);
        }
        else
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot apply moves because {nameof(energyManager)} is not assigned.", this);
        }

        if (orderManager != null)
        {
            orderManager.SetOrders(level.Orders);
        }
        else
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot apply orders because {nameof(orderManager)} is not assigned.", this);
        }

        if (boardManager != null)
        {
            boardManager.ConfigureBoard(level);
            boardManager.SetInitialBoardItems(level.InitialBoardItems);

            if (level.HasWeightedSpawnableItems)
            {
                boardManager.SetSpawnableItems(level.WeightedSpawnableItems);
            }
            else
            {
                boardManager.SetSpawnableItems(level.SpawnableItems);
            }

            boardManager.ResetBoard();
        }
        else
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot apply board balance because {nameof(boardManager)} is not assigned.", this);
        }

        if (bottomItemTrayController != null)
        {
            bottomItemTrayController.PrepareForLevel(level.GeneratorSpawnableItems);
        }

        isLoadingLevel = false;
    }

    public void LoadNextLevel()
    {
        var nextLevelIndex = currentLevelIndex + 1;
        if (!IsInsideLevels(nextLevelIndex))
        {
            Debug.Log("All levels completed", this);
            return;
        }

        LoadLevel(nextLevelIndex);
    }

    public void LoadPreviousLevel()
    {
        var previousLevelIndex = currentLevelIndex - 1;
        if (!IsInsideLevels(previousLevelIndex))
        {
            Debug.Log("Already at first level", this);
            return;
        }

        LoadLevel(previousLevelIndex);
    }

    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public void ResetProgressAndLoadFirstLevel()
    {
        ResetSavedProgress();

        if (levels.Count == 0)
        {
            Debug.LogWarning($"{nameof(LevelManager)} on '{name}' cannot reset progress because no levels are assigned.", this);
            return;
        }

        LoadLevel(0);
    }

    public void ExitLevelToMainMenu()
    {
        ConsumeLifeForFailedAttempt();
        LoadMainMenuScene();
    }

    public void RestartCurrentLevelFromWindow()
    {
        ConsumeLifeForFailedAttempt();
        RestartLevel();
    }

    public void GiveUpCurrentLevel()
    {
        ConsumeLifeForFailedAttempt();
        LoadMainMenuScene();
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

        if (bottomItemTrayController == null)
        {
            bottomItemTrayController = FindFirstObjectByType<BottomItemTrayController>(FindObjectsInactive.Include);
        }

        if (currencyManager == null)
        {
            currencyManager = FindFirstObjectByType<CurrencyManager>();
        }

        if (victoryLevelWindow == null)
        {
            victoryLevelWindow = FindSceneObject("VictoryLevelWindow");
        }

        if (victoryRewardsContainer == null)
        {
            var rewardsContainerObject = FindChildObject(victoryLevelWindow, "RewardsContainer");
            victoryRewardsContainer = rewardsContainerObject != null ? rewardsContainerObject.transform : null;
        }

        if (victoryContinueButton == null)
        {
            victoryContinueButton = FindButtonInRoot(victoryLevelWindow, "BtnContinue");
        }

        if (victoryWatchAdsButton == null)
        {
            victoryWatchAdsButton = FindButtonInRoot(victoryLevelWindow, "BtnWatchADS");
        }

        if (looseLevelWindow == null)
        {
            looseLevelWindow = FindSceneObject("LooseLevelWindow");
        }

        if (buyMovesButton == null)
        {
            buyMovesButton = FindButtonInRoot(looseLevelWindow, "BuyMovesButton");
        }

        if (buyMovesButton != null)
        {
            buyMovesButton.onClick.RemoveListener(BuyMovesAndContinue);
            buyMovesButton.onClick.AddListener(BuyMovesAndContinue);
        }

        if (shopScreen == null)
        {
            shopScreen = FindSceneObject("ShopScreen");
        }

        ResolveShopHeaderResourceTexts();

        if (loosePopup == null)
        {
            loosePopup = FindSceneObject("LoosePopup");
        }

        if (loosePanel == null)
        {
            loosePanel = FindChildObject(loosePopup, LoosePanelName);
        }

        if (loseLevelWindowStart == null)
        {
            loseLevelWindowStart = FindChildObject(loosePopup, LevelWindowStartName);
        }

        ResolveLoseLevelWindowReferences();

        if (loosePopupQuitButton == null)
        {
            loosePopupQuitButton = FindButtonInRoot(loosePopup, "QuitButton");
        }

        if (loosePopupQuitButton != null)
        {
            loosePopupQuitButton.onClick.RemoveListener(LoadMainMenuScene);
            loosePopupQuitButton.onClick.AddListener(LoadMainMenuScene);
        }

        if (loseLevelStartButton != null)
        {
            loseLevelStartButton.onClick.RemoveListener(RestartLevelFromLoseWindow);
            loseLevelStartButton.onClick.AddListener(RestartLevelFromLoseWindow);
        }

        if (loseLevelExitButton != null)
        {
            loseLevelExitButton.onClick.RemoveListener(ExitLoseWindowToMainMenu);
            loseLevelExitButton.onClick.AddListener(ExitLoseWindowToMainMenu);
        }

        if (exitLevelButton == null)
        {
            exitLevelButton = FindSceneButton("ExitLevelButton");
        }

        if (exitLevelButton != null)
        {
            exitLevelButton.onClick.RemoveListener(ExitLevelToMainMenu);
            exitLevelButton.onClick.AddListener(ExitLevelToMainMenu);
        }

        if (restartLevelButton == null)
        {
            restartLevelButton = FindSceneButton("RestartLevelButton");
        }

        if (restartLevelButton != null)
        {
            restartLevelButton.onClick.RemoveListener(RestartCurrentLevelFromWindow);
            restartLevelButton.onClick.AddListener(RestartCurrentLevelFromWindow);
        }

        if (giveUpButton == null)
        {
            giveUpButton = FindSceneButton("GiveUpButton");
        }

        if (giveUpButton != null)
        {
            giveUpButton.onClick.RemoveListener(GiveUpCurrentLevel);
            giveUpButton.onClick.AddListener(GiveUpCurrentLevel);
        }

        if (levelText == null)
        {
            levelText = FindLevelText();
        }
    }

    private void CompleteCurrentLevelAndReturnToMenu()
    {
        SaveNextLevelIndex();
        var coinReward = GrantCurrentLevelReward();
        ShowVictoryLevelWindow(coinReward);
    }

    private void SaveNextLevelIndex()
    {
        var nextLevelIndex = currentLevelIndex + 1;
        if (!IsInsideLevels(nextLevelIndex))
        {
            Debug.Log("All levels completed", this);
            return;
        }

        PlayerPrefs.SetInt(SavedLevelIndexKey, nextLevelIndex);
        PlayerPrefs.Save();
    }

    private void ShowVictoryLevelWindow(int coinReward)
    {
        ResolveReferences();

        if (victoryLevelWindow == null || victoryRewardsContainer == null || victoryRewardItemPrefab == null || victoryContinueButton == null)
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot show VictoryLevelWindow because reward references are missing.", this);
            LoadMainMenuScene();
            return;
        }

        ClearVictoryRewardItems();
        waitingVictoryLevelWindowClose = true;
        AddVictoryWindowButtonListeners();
        victoryLevelWindow.transform.SetAsLastSibling();
        SetVictoryLevelWindowActive(true);

        if (coinReward > 0)
        {
            var rewardItem = Instantiate(victoryRewardItemPrefab, victoryRewardsContainer);
            rewardItem.Set(victoryCoinsIcon, coinReward);
            rewardItem.PlayAppearEffect();
        }

        if (victoryOpenSound != null)
        {
            UIAudioController.Instance?.PlayUISound(victoryOpenSound);
        }
    }

    private void ContinueToNextLevel()
    {
        if (!waitingVictoryLevelWindowClose)
        {
            return;
        }

        waitingVictoryLevelWindowClose = false;
        RemoveVictoryWindowButtonListeners();

        var nextLevelIndex = currentLevelIndex + 1;
        if (!IsInsideLevels(nextLevelIndex))
        {
            LoadMainMenuScene();
            return;
        }

        LoadSceneWithTransition(SceneManager.GetActiveScene().name);
    }

    private void LoadMainMenuScene()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            mainMenuSceneName = DefaultMainMenuSceneName;
        }

#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif
        LoadSceneWithTransition(mainMenuSceneName);
    }

    private void LoadSceneWithTransition(string sceneName)
    {
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene(sceneName);
            return;
        }

        Debug.LogWarning($"{nameof(LevelManager)} on '{name}' is loading scene '{sceneName}' without {nameof(SceneTransition)} because no instance exists.", this);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private bool IsOutOfMoves()
    {
        return energyManager != null
            && energyManager.CurrentEnergy <= 0
            && (orderManager == null || !orderManager.AreAllOrdersReadyOrClaimed)
            && (boardManager == null || !boardManager.IsBusy)
            && (victoryLevelWindow == null || !victoryLevelWindow.activeInHierarchy);
    }

    private void ShowLooseLevelWindow()
    {
        ResolveReferences();
        waitingLoosePanelTap = false;
        SetLoosePopupActive(false);

        if (looseLevelWindow == null || buyMovesButton == null)
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot show LooseLevelWindow because required references are missing.", this);
            LoadMainMenuScene();
            return;
        }

        buyMovesButton.interactable = true;
        looseLevelWindow.transform.SetAsLastSibling();
        SetLooseLevelWindowActive(true);
    }

    private void BuyMovesAndContinue()
    {
        if (!levelLoseHandled)
        {
            return;
        }

        ResolveReferences();

        if (energyManager == null)
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot add moves because {nameof(energyManager)} is missing.", this);
            return;
        }

        buyMovesButton.interactable = false;

        var spentGold = currencyManager != null
            ? currencyManager.SpendCoins(BuyMovesGoldCost)
            : PlayerGold.SpendCoins(BuyMovesGoldCost);

        if (!spentGold)
        {
            buyMovesButton.interactable = true;
            OpenShopScreen();
            return;
        }

        energyManager.AddEnergy(BoughtMovesAmount);
        levelLoseHandled = false;
        SetLooseLevelWindowActive(false);
    }

    private void OpenShopScreen()
    {
        if (shopScreen == null)
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot open ShopScreen because it was not found.", this);
            return;
        }

        RefreshShopPlayerResources();
        shopScreen.transform.SetAsLastSibling();
        SetShopScreenActive(true);
    }

    private void ShowLoosePopup()
    {
        ResolveReferences();

        if (loosePopup == null)
        {
            Debug.LogWarning($"{nameof(LevelManager)} on '{name}' cannot show LoosePopup because it was not found. Returning to main menu.", this);
            LoadMainMenuScene();
            return;
        }

        SetLoosePopupActive(true);
        SetLoosePanelActive(true);
        SetLoseLevelWindowActive(false);
        waitingLoosePanelTap = true;
    }

    private void ShowLoseLevelStartWindow()
    {
        waitingLoosePanelTap = false;
        ResolveReferences();
        RefreshLoseLevelWindow(CurrentLevel);
        SetLoosePanelActive(false);
        SetLoseLevelWindowActive(true);
    }

    private void RestartLevelFromLoseWindow()
    {
        waitingLoosePanelTap = false;
        SetLoosePopupActive(false);
        RestartLevel();
    }

    private void ExitLoseWindowToMainMenu()
    {
        waitingLoosePanelTap = false;
        SetLoosePopupActive(false);
        LoadMainMenuScene();
    }

    private int GrantCurrentLevelReward()
    {
        var coinReward = CurrentLevel != null ? Mathf.Max(0, CurrentLevel.CoinReward) : 0;
        if (coinReward <= 0)
        {
            return 0;
        }

        ResolveReferences();

        if (currencyManager != null)
        {
            currencyManager.AddCoins(coinReward);
        }
        else
        {
            PlayerGold.AddCoins(coinReward);
        }

        return coinReward;
    }

    private void ConsumeLifeForFailedAttempt()
    {
        if (lifeConsumedForCurrentLevel || levelCompleteHandled)
        {
            return;
        }

        lifeConsumedForCurrentLevel = true;
        PlayerLives.ConsumeLife();
        RefreshShopPlayerLivesAmount();
    }

    private void RefreshShopPlayerResources()
    {
        ResolveShopHeaderResourceTexts();
        RefreshShopPlayerLivesAmount();
        RefreshShopPlayerGoldAmount(PlayerGold.CurrentCoins);
    }

    private void RefreshShopPlayerLivesAmount()
    {
        if (shopPlayerLivesAmountText == null)
        {
            ResolveShopHeaderResourceTexts();
        }

        if (shopPlayerLivesAmountText == null)
        {
            return;
        }

        PlayerLives.RefreshState();
        shopPlayerLivesAmountText.text = PlayerLives.CurrentLives.ToString();
    }

    private void RefreshShopPlayerLivesWhileVisible()
    {
        if (shopScreen == null
            || !shopScreen.activeInHierarchy
            || Time.unscaledTime < nextShopLivesRefreshTime)
        {
            return;
        }

        nextShopLivesRefreshTime = Time.unscaledTime + ShopLivesRefreshInterval;
        RefreshShopPlayerLivesAmount();
    }

    private void RefreshShopPlayerGoldAmount(int coins)
    {
        if (shopPlayerGoldAmountText == null)
        {
            ResolveShopHeaderResourceTexts();
        }

        if (shopPlayerGoldAmountText != null)
        {
            shopPlayerGoldAmountText.text = Mathf.Max(0, coins).ToString();
        }
    }

    private void HandlePlayerGoldChanged(int coins)
    {
        RefreshShopPlayerGoldAmount(coins);
    }

    private void ResolveShopHeaderResourceTexts()
    {
        if (shopScreen == null)
        {
            return;
        }

        var header = FindChildObject(shopScreen, "Header");
        var searchRoot = header != null ? header : shopScreen;

        if (shopPlayerLivesAmountText == null)
        {
            var livesButton = FindActiveChildObject(searchRoot, "BtnPlayerLives");
            shopPlayerLivesAmountText = FindTextInRoot(livesButton, "AmountText");
        }

        if (shopPlayerGoldAmountText == null)
        {
            var goldButton = FindActiveChildObject(searchRoot, "BtnPlayerGold");
            shopPlayerGoldAmountText = FindTextInRoot(goldButton, "GoldAmount");
        }
    }

    private void AddVictoryWindowButtonListeners()
    {
        RemoveVictoryWindowButtonListeners();

        if (victoryContinueButton == null)
        {
            return;
        }

        victoryContinueButton.interactable = true;
        victoryContinueButton.onClick.AddListener(ContinueToNextLevel);

        if (victoryWatchAdsButton != null)
        {
            victoryWatchAdsButton.onClick.RemoveListener(ContinueToNextLevel);
        }
    }

    private void RemoveVictoryWindowButtonListeners()
    {
        if (victoryContinueButton != null)
        {
            victoryContinueButton.onClick.RemoveListener(ContinueToNextLevel);
        }

        if (victoryWatchAdsButton != null)
        {
            victoryWatchAdsButton.onClick.RemoveListener(ContinueToNextLevel);
        }
    }

    private void ClearVictoryRewardItems()
    {
        if (victoryRewardsContainer == null)
        {
            return;
        }

        for (var i = victoryRewardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(victoryRewardsContainer.GetChild(i).gameObject);
        }
    }

    private void SetVictoryLevelWindowActive(bool isActive)
    {
        if (victoryLevelWindow != null && victoryLevelWindow.activeSelf != isActive)
        {
            victoryLevelWindow.SetActive(isActive);
        }
    }

    private void SetLooseLevelWindowActive(bool isActive)
    {
        if (looseLevelWindow != null && looseLevelWindow.activeSelf != isActive)
        {
            looseLevelWindow.SetActive(isActive);
        }
    }

    private void SetShopScreenActive(bool isActive)
    {
        if (shopScreen != null && shopScreen.activeSelf != isActive)
        {
            shopScreen.SetActive(isActive);
        }
    }

    private void SetLoosePopupActive(bool isActive)
    {
        if (loosePopup != null && loosePopup.activeSelf != isActive)
        {
            loosePopup.SetActive(isActive);
        }
    }

    private void SetLoosePanelActive(bool isActive)
    {
        if (loosePanel != null && loosePanel.activeSelf != isActive)
        {
            loosePanel.SetActive(isActive);
        }
    }

    private void SetLoseLevelWindowActive(bool isActive)
    {
        if (loseLevelWindowStart != null && loseLevelWindowStart.activeSelf != isActive)
        {
            loseLevelWindowStart.SetActive(isActive);
        }
    }

    private void ResolveLoseLevelWindowReferences()
    {
        if (loseLevelWindowStart == null)
        {
            return;
        }

        if (loseLevelWindowHeaderLabel == null)
        {
            loseLevelWindowHeaderLabel = FindTextInRoot(loseLevelWindowStart, "HeaderLabel");
        }

        if (loseLevelWindowTaskItemsRoot == null)
        {
            loseLevelWindowTaskItemsRoot = FindRectInRoot(loseLevelWindowStart, "TasksItems");
            loseLevelWindowTaskItemsRoot = loseLevelWindowTaskItemsRoot != null ? loseLevelWindowTaskItemsRoot : FindRectInRoot(loseLevelWindowStart, "Task Items");
        }

        if (loseLevelStartButton == null)
        {
            loseLevelStartButton = FindButtonInRoot(loseLevelWindowStart, "BtnStartLevel");
            loseLevelStartButton = loseLevelStartButton != null ? loseLevelStartButton : FindButtonInRoot(loseLevelWindowStart, "ButtonBG");
        }

        if (loseLevelExitButton == null)
        {
            loseLevelExitButton = FindButtonInRoot(loseLevelWindowStart, "BtnRed");
        }
    }

    private void RefreshLoseLevelWindow(LevelData level)
    {
        ResolveLoseLevelWindowReferences();

        if (level == null)
        {
            return;
        }

        if (loseLevelWindowHeaderLabel != null)
        {
            loseLevelWindowHeaderLabel.text = $"Level {level.LevelNumber}";
        }

        if (loseLevelWindowTaskItemsRoot == null)
        {
            return;
        }

        var template = ResolveLoseLevelTaskItemTemplate();
        if (template == null)
        {
            Debug.LogWarning($"{nameof(LevelManager)} on '{name}' cannot fill {LevelWindowStartName} because TaskItem template was not found.", this);
            return;
        }

        ClearLoseLevelTaskItems(template);

        var orders = level.Orders;
        for (var i = 0; i < orders.Count; i++)
        {
            var order = orders[i];
            if (order == null)
            {
                continue;
            }

            var taskItem = Instantiate(template, loseLevelWindowTaskItemsRoot);
            taskItem.name = $"TaskItem_{i + 1}";
            taskItem.SetActive(true);
            ApplyTaskItem(taskItem, order);
        }

        SetSceneTemplateActive(template, false);
    }

    private GameObject ResolveLoseLevelTaskItemTemplate()
    {
        if (loseLevelWindowTaskItemPrefab != null)
        {
            return loseLevelWindowTaskItemPrefab;
        }

        if (loseLevelWindowTaskItemsRoot == null)
        {
            return null;
        }

        var existing = loseLevelWindowTaskItemsRoot.Find("TaskItem");
        if (existing != null)
        {
            return existing.gameObject;
        }

        return loseLevelWindowTaskItemsRoot.childCount > 0
            ? loseLevelWindowTaskItemsRoot.GetChild(0).gameObject
            : null;
    }

    private void ClearLoseLevelTaskItems(GameObject template)
    {
        if (loseLevelWindowTaskItemsRoot == null)
        {
            return;
        }

        for (var i = loseLevelWindowTaskItemsRoot.childCount - 1; i >= 0; i--)
        {
            var child = loseLevelWindowTaskItemsRoot.GetChild(i);
            if (template != null && template.scene.IsValid() && child == template.transform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private static void ApplyTaskItem(GameObject taskItem, OrderDefinition order)
    {
        if (taskItem == null || order == null)
        {
            return;
        }

        var taskImage = FindChildImage(taskItem.transform, "TaskImage");
        taskImage = taskImage != null ? taskImage : FindChildImage(taskItem.transform, "Image");
        if (taskImage != null)
        {
            taskImage.sprite = order.RequiredItem != null ? order.RequiredItem.Icon : null;
            taskImage.enabled = taskImage.sprite != null;
            taskImage.preserveAspect = true;
        }

        var itemCount = FindChildText(taskItem.transform, "ItemCount");
        if (itemCount != null)
        {
            itemCount.text = order.RequiredAmount.ToString();
        }
    }

    private static bool IsTapStarted()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        for (var i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInsideLevels(int index)
    {
        return index >= 0 && index < levels.Count;
    }

    private void RefreshLevelText(LevelData level)
    {
        if (levelText != null && level != null)
        {
            levelText.text = $"Level {level.LevelNumber}";
        }
    }

    private static TMP_Text FindLevelText()
    {
        var topBar = FindSceneObject("TopBar");
        var topBarLevelText = FindChildText(topBar != null ? topBar.transform : null, "LevelText");
        if (topBarLevelText != null)
        {
            return topBarLevelText;
        }

        var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "LevelText")
            {
                return texts[i];
            }
        }

        return null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null || target.name != objectName)
            {
                continue;
            }

            var scene = target.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                return target.gameObject;
            }
        }

        return null;
    }

    private static Button FindSceneButton(string buttonName)
    {
        var buttonObject = FindSceneObject(buttonName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    private static Button FindButtonInRoot(GameObject root, string buttonName)
    {
        if (root == null)
        {
            return null;
        }

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button != null && button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private static GameObject FindChildObject(GameObject root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target != null && target.name == childName)
            {
                return target.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindActiveChildObject(GameObject root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        GameObject inactiveFallback = null;
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null || target.name != childName)
            {
                continue;
            }

            if (target.gameObject.activeSelf)
            {
                return target.gameObject;
            }

            inactiveFallback = inactiveFallback != null ? inactiveFallback : target.gameObject;
        }

        return inactiveFallback;
    }

    private static TMP_Text FindTextInRoot(GameObject root, string textName)
    {
        if (root == null)
        {
            return null;
        }

        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text != null && text.name == textName)
            {
                return text;
            }
        }

        return null;
    }

    private static RectTransform FindRectInRoot(GameObject root, string rectName)
    {
        if (root == null)
        {
            return null;
        }

        var rects = root.GetComponentsInChildren<RectTransform>(true);
        for (var i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            if (rect != null && rect.name == rectName)
            {
                return rect;
            }
        }

        return null;
    }

    private static Image FindChildImage(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        var images = root.GetComponentsInChildren<Image>(true);
        for (var i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image != null && image.name == childName)
            {
                return image;
            }
        }

        return null;
    }

    private static TMP_Text FindChildText(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text != null && text.name == childName)
            {
                return text;
            }
        }

        return null;
    }

    private static void SetSceneTemplateActive(GameObject template, bool isActive)
    {
        if (template != null && template.scene.IsValid() && template.activeSelf != isActive)
        {
            template.SetActive(isActive);
        }
    }

#if UNITY_EDITOR
    private void FillLevelsFromProjectFolder()
    {
        if (string.IsNullOrWhiteSpace(levelsAssetFolder))
        {
            levelsAssetFolder = DefaultLevelsAssetFolder;
        }

        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(LevelData)}", new[] { levelsAssetFolder });
        if (guids == null || guids.Length == 0)
        {
            return;
        }

        var foundLevels = new List<LevelData>(guids.Length);
        for (var i = 0; i < guids.Length; i++)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            var level = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level != null && !foundLevels.Contains(level))
            {
                foundLevels.Add(level);
            }
        }

        foundLevels.Sort(CompareLevelsByNumber);

        if (HaveSameLevels(levels, foundLevels))
        {
            return;
        }

        levels.Clear();
        levels.AddRange(foundLevels);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static int CompareLevelsByNumber(LevelData first, LevelData second)
    {
        if (first == second)
        {
            return 0;
        }

        if (first == null)
        {
            return 1;
        }

        if (second == null)
        {
            return -1;
        }

        var numberComparison = first.LevelNumber.CompareTo(second.LevelNumber);
        return numberComparison != 0
            ? numberComparison
            : string.CompareOrdinal(first.name, second.name);
    }

    private static bool HaveSameLevels(IReadOnlyList<LevelData> first, IReadOnlyList<LevelData> second)
    {
        if (first == null || second == null || first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (first[i] != second[i])
            {
                return false;
            }
        }

        return true;
    }
#endif
}

public static class PlayerLives
{
    public const int MaxLives = 5;
    public const int RestoreIntervalSeconds = 30 * 60;

    private const string LivesKey = "PlayerLives.Current";
    private const string NextLifeTicksKey = "PlayerLives.NextLifeUtcTicks";

    private static readonly long RestoreIntervalTicks = TimeSpan.FromSeconds(RestoreIntervalSeconds).Ticks;

    public static int CurrentLives
    {
        get
        {
            RefreshState();
            return GetStoredLives();
        }
    }

    public static bool HasLives => CurrentLives > 0;
    public static bool IsMaxLives => CurrentLives >= MaxLives;

    public static TimeSpan TimeUntilNextLife
    {
        get
        {
            RefreshState();

            if (GetStoredLives() >= MaxLives)
            {
                return TimeSpan.Zero;
            }

            var nextLifeTicks = GetNextLifeTicks();
            if (nextLifeTicks <= 0)
            {
                return TimeSpan.FromSeconds(RestoreIntervalSeconds);
            }

            var remainingTicks = Math.Max(0L, nextLifeTicks - DateTime.UtcNow.Ticks);
            return TimeSpan.FromTicks(remainingTicks);
        }
    }

    public static bool ConsumeLife()
    {
        RefreshState();

        var lives = GetStoredLives();
        if (lives <= 0)
        {
            return false;
        }

        lives--;
        var nextLifeTicks = GetNextLifeTicks();
        if (lives < MaxLives && nextLifeTicks <= 0)
        {
            nextLifeTicks = DateTime.UtcNow.Ticks + RestoreIntervalTicks;
        }

        SaveState(lives, nextLifeTicks);
        return true;
    }

    public static void RestoreToMax()
    {
        SaveState(MaxLives, 0L);
    }

    public static void RefreshState()
    {
        if (!PlayerPrefs.HasKey(LivesKey))
        {
            SaveState(MaxLives, 0L);
            return;
        }

        var storedLives = PlayerPrefs.GetInt(LivesKey, MaxLives);
        var lives = Mathf.Clamp(storedLives, 0, MaxLives);
        var nextLifeTicks = GetNextLifeTicks();
        var originalLives = lives;
        var originalNextLifeTicks = nextLifeTicks;

        if (lives >= MaxLives)
        {
            if (storedLives != MaxLives || nextLifeTicks != 0L)
            {
                SaveState(MaxLives, 0L);
            }

            return;
        }

        var nowTicks = DateTime.UtcNow.Ticks;
        if (nextLifeTicks <= 0)
        {
            nextLifeTicks = nowTicks + RestoreIntervalTicks;
        }

        while (lives < MaxLives && nowTicks >= nextLifeTicks)
        {
            lives++;
            nextLifeTicks = lives < MaxLives ? nextLifeTicks + RestoreIntervalTicks : 0L;
        }

        if (storedLives != originalLives || lives != originalLives || nextLifeTicks != originalNextLifeTicks)
        {
            SaveState(lives, nextLifeTicks);
        }
    }

    private static int GetStoredLives()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(LivesKey, MaxLives), 0, MaxLives);
    }

    private static long GetNextLifeTicks()
    {
        var rawTicks = PlayerPrefs.GetString(NextLifeTicksKey, string.Empty);
        return long.TryParse(rawTicks, out var ticks) ? ticks : 0L;
    }

    private static void SaveState(int lives, long nextLifeTicks)
    {
        PlayerPrefs.SetInt(LivesKey, Mathf.Clamp(lives, 0, MaxLives));

        if (nextLifeTicks > 0)
        {
            PlayerPrefs.SetString(NextLifeTicksKey, nextLifeTicks.ToString());
        }
        else
        {
            PlayerPrefs.DeleteKey(NextLifeTicksKey);
        }

        PlayerPrefs.Save();
    }
}
