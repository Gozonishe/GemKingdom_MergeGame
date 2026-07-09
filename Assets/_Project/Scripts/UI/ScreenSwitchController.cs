using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class ScreenSwitchController : MonoBehaviour
{
    private const string DefaultLevelsAssetFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string CollectionScreenButtonName = "CollectionScreenButton";
    private const int RestoreLivesGoldCost = 900;

    private enum ScreenId
    {
        Main,
        Shop,
        Clan,
        Location,
        Ranking
    }

    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject shopScreen;
    [SerializeField] private GameObject clanScreen;
    [SerializeField] private GameObject locationScreen;
    [SerializeField] private GameObject rankingScreen;
    [SerializeField] private Button mainButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button clanButton;
    [SerializeField] private Button locationButton;
    [SerializeField] private Button rankingButton;
    [SerializeField] private Button playerGoldButton;
    [SerializeField] private Button getLivesClanButton;
    [SerializeField] private Button continueRaceButton;
    [SerializeField] private Button mainLevelButton;
    [SerializeField] private Button mergeGameButton;
    [SerializeField] private Button playerLivesButton;
    [SerializeField] private TMP_Text mergeGameLevelText;
    [SerializeField] private List<TMP_Text> levelObjectNumberTexts = new List<TMP_Text>();
    [SerializeField] private TMP_Text playerGoldAmountText;
    [SerializeField] private TMP_Text playerLivesAmountText;
    [SerializeField] private GameObject livesCounterRoot;
    [SerializeField] private TMP_Text livesCounterAmountText;
    [SerializeField] private GameObject livesAddWindowRoot;
    [SerializeField] private Button livesAddWindowBuyButton;
    [SerializeField] private TMP_Text livesAddWindowTimerText;
    [SerializeField] private TMP_Text levelStartWindowHeaderLabel;
    [SerializeField] private List<LevelData> levelStartLevels = new List<LevelData>();
    [SerializeField] private RectTransform levelStartTaskItemsRoot;
    [SerializeField] private GameObject levelStartTaskItemPrefab;
    [SerializeField] private bool autoFillLevelStartLevelsFromProjectFolder = true;
    [SerializeField] private string levelStartLevelsAssetFolder = DefaultLevelsAssetFolder;
    [SerializeField] private string mergeGameSceneName = "MergeGameScreen";
    [SerializeField] private string mergeGameLevelTextFormat = "Level {0}";
    [SerializeField] private RectTransform mainButtonContent;
    [SerializeField] private RectTransform shopButtonContent;
    [SerializeField] private RectTransform clanButtonContent;
    [SerializeField] private RectTransform locationButtonContent;
    [SerializeField] private RectTransform rankingButtonContent;
    [FormerlySerializedAs("selectedMainContentYOffset")]
    [SerializeField] private float selectedContentYOffset = 50f;
    [SerializeField] private ScreenId defaultScreen = ScreenId.Main;
    [SerializeField] private bool showDefaultScreenOnAwake = true;
    [SerializeField] private float livesUiRefreshInterval = 0.25f;

    private Vector2 mainButtonContentDefaultPosition;
    private Vector2 shopButtonContentDefaultPosition;
    private Vector2 clanButtonContentDefaultPosition;
    private Vector2 locationButtonContentDefaultPosition;
    private Vector2 rankingButtonContentDefaultPosition;
    private Button subscribedContinueRaceButton;
    private Button subscribedMainLevelButton;
    private Button subscribedMergeGameButton;
    private Button subscribedPlayerLivesButton;
    private Button subscribedLivesAddWindowBuyButton;
    private readonly List<Button> levelButtons = new List<Button>();
    private readonly List<Button> subscribedLevelButtons = new List<Button>();
    private float nextLivesUiRefreshTime;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoFillLevelStartLevelsFromProjectFolder)
        {
            FillLevelStartLevelsFromProjectFolder();
        }
    }
#endif

    private void Awake()
    {
        ResolveMissingReferences();
        CacheDefaultPositions();
        SubscribeButtons();

        if (showDefaultScreenOnAwake)
        {
            Show(defaultScreen);
        }

        RefreshMergeGameLevelText();
        RefreshLevelStartTasks();
        RefreshPlayerGoldUi();
        RefreshPlayerLivesUi();
    }

    private void OnEnable()
    {
        transform.SetAsLastSibling();
        RefreshMergeGameLevelText();
        RefreshLevelStartTasks();
        RefreshPlayerGoldUi();
        RefreshPlayerLivesUi();
    }

    private void Start()
    {
        ResolveMissingReferences();
        SubscribeMainLevelButton();
        SubscribeMergeGameButton();
        SubscribePlayerLivesButton();
        SubscribeLevelButtons();
        SubscribeLivesAddWindowBuyButton();
        RefreshMergeGameLevelText();
        RefreshLevelStartTasks();
        RefreshPlayerGoldUi();
        RefreshPlayerLivesUi();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextLivesUiRefreshTime)
        {
            return;
        }

        nextLivesUiRefreshTime = Time.unscaledTime + livesUiRefreshInterval;
        RefreshPlayerGoldUi();
        RefreshPlayerLivesUi();
    }

    private void OnDestroy()
    {
        UnsubscribeButtons();
    }

    public void ShowMain()
    {
        Show(ScreenId.Main);
    }

    public void ShowShop()
    {
        Show(ScreenId.Shop);
    }

    public void ShowClan()
    {
        Show(ScreenId.Clan);
    }

    public void ShowLocation()
    {
        Show(ScreenId.Location);
    }

    public void ShowRanking()
    {
        Show(ScreenId.Ranking);
    }

    private void SubscribeButtons()
    {
        if (mainButton != null)
        {
            mainButton.onClick.AddListener(ShowMain);
        }

        if (shopButton != null)
        {
            shopButton.onClick.AddListener(ShowShop);
        }

        if (playerGoldButton != null)
        {
            playerGoldButton.onClick.AddListener(ShowShop);
        }

        if (getLivesClanButton != null)
        {
            getLivesClanButton.onClick.AddListener(ShowClan);
        }

        SubscribeContinueRaceButton();
        SubscribeMainLevelButton();
        SubscribeMergeGameButton();
        SubscribePlayerLivesButton();
        SubscribeLevelButtons();
        SubscribeLivesAddWindowBuyButton();

        if (clanButton != null && !IsCollectionScreenButton(clanButton))
        {
            clanButton.onClick.AddListener(ShowClan);
        }

        if (locationButton != null)
        {
            locationButton.onClick.AddListener(ShowLocation);
        }

        if (rankingButton != null)
        {
            rankingButton.onClick.AddListener(ShowRanking);
        }
    }

    private void UnsubscribeButtons()
    {
        if (mainButton != null)
        {
            mainButton.onClick.RemoveListener(ShowMain);
        }

        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(ShowShop);
        }

        if (playerGoldButton != null)
        {
            playerGoldButton.onClick.RemoveListener(ShowShop);
        }

        if (getLivesClanButton != null)
        {
            getLivesClanButton.onClick.RemoveListener(ShowClan);
        }

        UnsubscribeContinueRaceButton();
        UnsubscribeMainLevelButton();
        UnsubscribeMergeGameButton();
        UnsubscribePlayerLivesButton();
        UnsubscribeLevelButtons();
        UnsubscribeLivesAddWindowBuyButton();

        if (clanButton != null)
        {
            clanButton.onClick.RemoveListener(ShowClan);
        }

        if (locationButton != null)
        {
            locationButton.onClick.RemoveListener(ShowLocation);
        }

        if (rankingButton != null)
        {
            rankingButton.onClick.RemoveListener(ShowRanking);
        }
    }

    private void Show(ScreenId screenId)
    {
        SetActive(mainScreen, screenId == ScreenId.Main);
        SetActive(shopScreen, screenId == ScreenId.Shop);
        SetActive(clanScreen, screenId == ScreenId.Clan);
        SetActive(locationScreen, screenId == ScreenId.Location);
        SetActive(rankingScreen, screenId == ScreenId.Ranking);

        if (screenId == ScreenId.Ranking)
        {
            BindContinueRaceButton();
        }

        UpdateButtonContentOffsets(screenId);
        transform.SetAsLastSibling();
    }

    private static bool IsCollectionScreenButton(Button button)
    {
        return button != null && button.gameObject.name == CollectionScreenButtonName;
    }

    private void TryLoadMergeGameScreen()
    {
        PlayerLives.RefreshState();

        if (!PlayerLives.HasLives)
        {
            OpenLivesAddWindow();
            return;
        }

        LoadMergeGameScreen();
    }

    private void LoadMergeGameScreen()
    {
        if (string.IsNullOrWhiteSpace(mergeGameSceneName))
        {
            mergeGameSceneName = "MergeGameScreen";
        }

        if (string.IsNullOrWhiteSpace(mergeGameSceneName))
        {
            Debug.LogError($"{nameof(ScreenSwitchController)} on '{name}' cannot load merge game because {nameof(mergeGameSceneName)} is empty.", this);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif
        SceneManager.LoadScene(mergeGameSceneName, LoadSceneMode.Single);
    }

    private void ResolveMissingReferences()
    {
        mainScreen = mainScreen != null ? mainScreen : FindSceneObject("MainScreen");
        shopScreen = shopScreen != null ? shopScreen : FindSceneObject("ShopScreen");
        clanScreen = clanScreen != null ? clanScreen : FindSceneObject("ClanScreen");
        locationScreen = locationScreen != null ? locationScreen : FindSceneObject("LocationScreen");
        rankingScreen = rankingScreen != null ? rankingScreen : FindSceneObject("RankingScreen");

        mainButton = mainButton != null ? mainButton : FindSceneButton("MainScreenButton");
        shopButton = shopButton != null ? shopButton : FindSceneButton("ShopScreenButton");
        clanButton = clanButton != null ? clanButton : FindSceneButton("ClanScreenButton");
        locationButton = locationButton != null ? locationButton : FindSceneButton("LocationScreenButton");
        rankingButton = rankingButton != null ? rankingButton : FindSceneButton("RankingScreenButton");
        playerGoldButton = playerGoldButton != null ? playerGoldButton : FindSceneButton("BtnPlayerGold");
        getLivesClanButton = getLivesClanButton != null ? getLivesClanButton : FindSceneButtonInRoot("GetLivesScreen", "BtnGreen");
        continueRaceButton = continueRaceButton != null ? continueRaceButton : FindSceneButtonInRoot("RankingScreen", "BtnContinueRace");
        mainLevelButton = mainLevelButton != null ? mainLevelButton : FindSceneButtonInRoot("MainScreen", "BtnMain");
        mainLevelButton = mainLevelButton != null ? mainLevelButton : FindSceneButton("BtnMain");
        mergeGameButton = mergeGameButton != null ? mergeGameButton : FindSceneButtonInRoot("LevelStartWindow", "BtnStartLevel");
        mergeGameButton = mergeGameButton != null ? mergeGameButton : FindSceneButton("BtnStartLevel");
        playerLivesButton = playerLivesButton != null ? playerLivesButton : FindSceneButton("BtnPlayerLives");
        mergeGameLevelText = mergeGameLevelText != null ? mergeGameLevelText : FindTextInButton(FindSceneButtonInRoot("MainScreen", "BtnMain"), "TextNumber");
        mergeGameLevelText = mergeGameLevelText != null ? mergeGameLevelText : FindSceneTextInRoot("MainScreen", "TextNumber");
        ResolveLevelObjectNumberTexts();
        playerGoldAmountText = playerGoldAmountText != null ? playerGoldAmountText : FindTextInButton(playerGoldButton, "GoldAmount");
        playerGoldAmountText = playerGoldAmountText != null ? playerGoldAmountText : FindSceneTextInRoot("BtnPlayerGold", "GoldAmount");
        playerLivesAmountText = playerLivesAmountText != null ? playerLivesAmountText : FindTextInButton(playerLivesButton, "AmountText");
        livesCounterRoot = livesCounterRoot != null ? livesCounterRoot : FindSceneObject("LivesCounter");
        livesCounterAmountText = livesCounterAmountText != null ? livesCounterAmountText : FindSceneTextInRoot("BtnPlayerLives", "LivesCounter");
        livesCounterAmountText = livesCounterAmountText != null ? livesCounterAmountText : FindSceneTextInRoot("LivesCounter", "LivesCounter");
        livesAddWindowRoot = livesAddWindowRoot != null ? livesAddWindowRoot : FindSceneObject("LivesAddWindow");
        livesAddWindowBuyButton = livesAddWindowBuyButton != null ? livesAddWindowBuyButton : FindSceneButtonOrChildButtonInRoot("LivesAddWindow", "BtnBuyLives");
        livesAddWindowBuyButton = livesAddWindowBuyButton != null ? livesAddWindowBuyButton : FindSceneButtonOrChildButtonInRoot("LivesAddWindow", "BtnBuy");
        livesAddWindowTimerText = livesAddWindowTimerText != null ? livesAddWindowTimerText : FindSceneTextByPath("LivesAddWindow", "Container/Content/Group/Timer/Counter");
        ResolveLevelButtons();
        levelStartWindowHeaderLabel = levelStartWindowHeaderLabel != null ? levelStartWindowHeaderLabel : FindSceneTextByPath("LevelStartWindow", "Bg/HeaderLabel");
        levelStartWindowHeaderLabel = levelStartWindowHeaderLabel != null ? levelStartWindowHeaderLabel : FindSceneTextInRoot("LevelStartWindow", "HeaderLabel");
        levelStartTaskItemsRoot = levelStartTaskItemsRoot != null ? levelStartTaskItemsRoot : FindSceneRectByPath("LevelStartWindow", "Content/TaskPlane/TasksItems");
        levelStartTaskItemsRoot = levelStartTaskItemsRoot != null ? levelStartTaskItemsRoot : FindSceneRectByPath("LevelStartWindow", "Content/TaskPlane/Task Items");
        levelStartTaskItemsRoot = levelStartTaskItemsRoot != null ? levelStartTaskItemsRoot : FindSceneRectInRoot("LevelStartWindow", "TasksItems");
        levelStartTaskItemsRoot = levelStartTaskItemsRoot != null ? levelStartTaskItemsRoot : FindSceneRectInRoot("LevelStartWindow", "Task Items");
        mainButtonContent = mainButtonContent != null ? mainButtonContent : FindButtonContent(mainButton);
        shopButtonContent = shopButtonContent != null ? shopButtonContent : FindButtonContent(shopButton);
        clanButtonContent = clanButtonContent != null ? clanButtonContent : FindButtonContent(clanButton);
        locationButtonContent = locationButtonContent != null ? locationButtonContent : FindButtonContent(locationButton);
        rankingButtonContent = rankingButtonContent != null ? rankingButtonContent : FindButtonContent(rankingButton);
    }

    private void RefreshPlayerGoldUi()
    {
        if (playerGoldAmountText == null)
        {
            ResolveMissingReferences();
        }

        if (playerGoldAmountText != null)
        {
            playerGoldAmountText.text = PlayerGold.CurrentCoins.ToString();
        }
    }

    private void RefreshPlayerLivesUi()
    {
        if (playerLivesAmountText == null || livesCounterRoot == null || livesCounterAmountText == null || livesAddWindowTimerText == null)
        {
            ResolveMissingReferences();
        }

        PlayerLives.RefreshState();
        var lives = PlayerLives.CurrentLives;
        var hasMaxLives = lives >= PlayerLives.MaxLives;

        if (playerLivesAmountText != null)
        {
            playerLivesAmountText.text = hasMaxLives ? "MAX" : FormatLivesTimer(PlayerLives.TimeUntilNextLife);
        }

        if (livesAddWindowTimerText != null)
        {
            livesAddWindowTimerText.text = hasMaxLives ? "MAX" : FormatLivesTimer(PlayerLives.TimeUntilNextLife);
        }

        SetActive(livesCounterRoot, !hasMaxLives);

        if (livesCounterAmountText != null)
        {
            livesCounterAmountText.text = lives.ToString();
        }
    }

    private static string FormatLivesTimer(System.TimeSpan remainingTime)
    {
        var totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)remainingTime.TotalSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private void OpenLivesAddWindow()
    {
        if (livesAddWindowRoot == null || livesAddWindowBuyButton == null)
        {
            ResolveMissingReferences();
            SubscribeLivesAddWindowBuyButton();
        }

        if (livesAddWindowRoot == null)
        {
            Debug.LogWarning($"{nameof(ScreenSwitchController)} on '{name}' cannot open LivesAddWindow because it was not found.", this);
            return;
        }

        EnsureWindowReceivesInput(livesAddWindowRoot);
        livesAddWindowRoot.transform.SetAsLastSibling();
        livesAddWindowRoot.SetActive(true);
    }

    private void BuyLivesAndCloseWindow()
    {
        if (!PlayerGold.SpendCoins(RestoreLivesGoldCost))
        {
            SetActive(livesAddWindowRoot, false);
            RefreshPlayerGoldUi();
            ShowShop();
            return;
        }

        PlayerLives.RestoreToMax();
        SetActive(livesAddWindowRoot, false);
        RefreshPlayerGoldUi();
        RefreshPlayerLivesUi();
    }

    private void HandlePlayerLivesButtonClicked()
    {
        PlayerLives.RefreshState();
        RefreshPlayerLivesUi();

        if (PlayerLives.IsMaxLives)
        {
            SetActive(livesAddWindowRoot, false);
            return;
        }

        OpenLivesAddWindow();
    }

    private static void EnsureWindowReceivesInput(GameObject windowRoot)
    {
        if (windowRoot == null || windowRoot.GetComponent<Canvas>() == null)
        {
            return;
        }

        if (windowRoot.GetComponent<GraphicRaycaster>() == null)
        {
            windowRoot.AddComponent<GraphicRaycaster>();
        }

        var canvasGroup = windowRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void BindContinueRaceButton()
    {
        continueRaceButton = continueRaceButton != null ? continueRaceButton : FindSceneButtonInRoot("RankingScreen", "BtnContinueRace");
        SubscribeContinueRaceButton();
    }

    private void RefreshMergeGameLevelText()
    {
        if (mergeGameLevelText == null)
        {
            mergeGameLevelText = FindTextInButton(FindSceneButtonInRoot("MainScreen", "BtnMain"), "TextNumber");
            mergeGameLevelText = mergeGameLevelText != null ? mergeGameLevelText : FindSceneTextInRoot("MainScreen", "TextNumber");
        }

        if (levelStartWindowHeaderLabel == null)
        {
            levelStartWindowHeaderLabel = FindSceneTextByPath("LevelStartWindow", "Bg/HeaderLabel");
            levelStartWindowHeaderLabel = levelStartWindowHeaderLabel != null ? levelStartWindowHeaderLabel : FindSceneTextInRoot("LevelStartWindow", "HeaderLabel");
        }

        var levelNumber = LevelManager.GetSavedLevelNumber();
        var levelText = string.Format(mergeGameLevelTextFormat, levelNumber);

        if (mergeGameLevelText != null)
        {
            mergeGameLevelText.text = levelText;
        }

        RefreshLevelObjectNumberTexts(levelNumber);

        if (levelStartWindowHeaderLabel != null)
        {
            levelStartWindowHeaderLabel.text = levelText;
        }
    }

    private void RefreshLevelObjectNumberTexts(int currentLevelNumber)
    {
        if (HasMissingLevelObjectNumberTexts())
        {
            ResolveLevelObjectNumberTexts();
        }

        for (var i = 0; i < 3; i++)
        {
            var levelObjectNumberText = levelObjectNumberTexts[i];
            if (levelObjectNumberText != null)
            {
                levelObjectNumberText.text = (currentLevelNumber + i).ToString();
            }
        }
    }

    private void ResolveLevelObjectNumberTexts()
    {
        if (levelObjectNumberTexts == null)
        {
            levelObjectNumberTexts = new List<TMP_Text>();
        }

        while (levelObjectNumberTexts.Count < 3)
        {
            levelObjectNumberTexts.Add(null);
        }

        for (var i = 0; i < 3; i++)
        {
            if (levelObjectNumberTexts[i] != null)
            {
                continue;
            }

            var levelObjectName = $"LevelObject_{i + 1}";
            levelObjectNumberTexts[i] = FindSceneTextByPath(levelObjectName, "LvlNumber");
            levelObjectNumberTexts[i] = levelObjectNumberTexts[i] != null
                ? levelObjectNumberTexts[i]
                : FindSceneTextInRoot(levelObjectName, "LvlNumber");
        }
    }

    private void ResolveLevelButtons()
    {
        levelButtons.Clear();
        var foundButtons = FindSceneButtons("BtnLevel");

        for (var i = 0; i < foundButtons.Count; i++)
        {
            var levelButton = foundButtons[i];
            if (levelButton != null && levelButton != mainLevelButton && !levelButtons.Contains(levelButton))
            {
                levelButtons.Add(levelButton);
            }
        }
    }

    private bool HasMissingLevelObjectNumberTexts()
    {
        if (levelObjectNumberTexts == null || levelObjectNumberTexts.Count < 3)
        {
            return true;
        }

        for (var i = 0; i < 3; i++)
        {
            if (levelObjectNumberTexts[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshLevelStartTasks()
    {
        if (levelStartTaskItemsRoot == null)
        {
            levelStartTaskItemsRoot = FindSceneRectByPath("LevelStartWindow", "Content/TaskPlane/TasksItems");
            levelStartTaskItemsRoot = levelStartTaskItemsRoot != null ? levelStartTaskItemsRoot : FindSceneRectByPath("LevelStartWindow", "Content/TaskPlane/Task Items");
            levelStartTaskItemsRoot = levelStartTaskItemsRoot != null ? levelStartTaskItemsRoot : FindSceneRectInRoot("LevelStartWindow", "TasksItems");
            levelStartTaskItemsRoot = levelStartTaskItemsRoot != null ? levelStartTaskItemsRoot : FindSceneRectInRoot("LevelStartWindow", "Task Items");
        }

        if (levelStartTaskItemsRoot == null)
        {
            return;
        }

        var currentLevel = GetCurrentLevelStartLevel();
        var orders = currentLevel != null ? currentLevel.Orders : null;
        var template = ResolveLevelStartTaskItemTemplate();

        if (template == null)
        {
            Debug.LogWarning($"{nameof(ScreenSwitchController)} on '{name}' cannot refresh level start tasks because no TaskItem prefab or template was found.", this);
            return;
        }

        ClearLevelStartTaskItems(template);

        if (orders == null)
        {
            SetTaskTemplateActive(template, false);
            return;
        }

        for (var i = 0; i < orders.Count; i++)
        {
            var order = orders[i];
            if (order == null)
            {
                continue;
            }

            var taskItem = Instantiate(template, levelStartTaskItemsRoot);
            taskItem.name = $"TaskItem_{i + 1}";
            taskItem.SetActive(true);
            ApplyTaskItem(taskItem, order);
        }

        SetTaskTemplateActive(template, false);
    }

    private LevelData GetCurrentLevelStartLevel()
    {
        if (levelStartLevels == null || levelStartLevels.Count == 0)
        {
            return null;
        }

        var savedLevelIndex = Mathf.Clamp(LevelManager.GetSavedLevelIndex(), 0, levelStartLevels.Count - 1);
        return levelStartLevels[savedLevelIndex];
    }

    private GameObject ResolveLevelStartTaskItemTemplate()
    {
        if (levelStartTaskItemPrefab != null)
        {
            return levelStartTaskItemPrefab;
        }

        if (levelStartTaskItemsRoot == null)
        {
            return null;
        }

        var existing = levelStartTaskItemsRoot.Find("TaskItem");
        if (existing != null)
        {
            return existing.gameObject;
        }

        return levelStartTaskItemsRoot.childCount > 0
            ? levelStartTaskItemsRoot.GetChild(0).gameObject
            : null;
    }

    private void ClearLevelStartTaskItems(GameObject template)
    {
        for (var i = levelStartTaskItemsRoot.childCount - 1; i >= 0; i--)
        {
            var child = levelStartTaskItemsRoot.GetChild(i);
            if (template != null && child == template.transform)
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

    private static void SetTaskTemplateActive(GameObject template, bool isActive)
    {
        if (template != null && template.scene.IsValid())
        {
            template.SetActive(isActive);
        }
    }

    private void SubscribeContinueRaceButton()
    {
        if (continueRaceButton == null)
        {
            return;
        }

        if (subscribedContinueRaceButton == continueRaceButton)
        {
            return;
        }

        UnsubscribeContinueRaceButton();
        continueRaceButton.onClick.AddListener(ShowMain);
        subscribedContinueRaceButton = continueRaceButton;
    }

    private void SubscribeMergeGameButton()
    {
        if (mergeGameButton == null)
        {
            return;
        }

        if (subscribedMergeGameButton == mergeGameButton)
        {
            return;
        }

        UnsubscribeMergeGameButton();
        mergeGameButton.onClick.AddListener(TryLoadMergeGameScreen);
        subscribedMergeGameButton = mergeGameButton;
    }

    private void SubscribeMainLevelButton()
    {
        if (mainLevelButton == null)
        {
            return;
        }

        if (subscribedMainLevelButton == mainLevelButton)
        {
            return;
        }

        UnsubscribeMainLevelButton();
        mainLevelButton.onClick.AddListener(TryLoadMergeGameScreen);
        subscribedMainLevelButton = mainLevelButton;
    }

    private void SubscribePlayerLivesButton()
    {
        if (playerLivesButton == null)
        {
            return;
        }

        if (subscribedPlayerLivesButton == playerLivesButton)
        {
            return;
        }

        UnsubscribePlayerLivesButton();
        playerLivesButton.onClick.AddListener(HandlePlayerLivesButtonClicked);
        subscribedPlayerLivesButton = playerLivesButton;
    }

    private void SubscribeLevelButtons()
    {
        ResolveLevelButtons();

        for (var i = 0; i < levelButtons.Count; i++)
        {
            var levelButton = levelButtons[i];
            if (levelButton == null || subscribedLevelButtons.Contains(levelButton))
            {
                continue;
            }

            levelButton.onClick.AddListener(TryLoadMergeGameScreen);
            subscribedLevelButtons.Add(levelButton);
        }
    }

    private void SubscribeLivesAddWindowBuyButton()
    {
        if (livesAddWindowBuyButton == null)
        {
            livesAddWindowBuyButton = FindSceneButtonOrChildButtonInRoot("LivesAddWindow", "BtnBuyLives");
            livesAddWindowBuyButton = livesAddWindowBuyButton != null ? livesAddWindowBuyButton : FindSceneButtonOrChildButtonInRoot("LivesAddWindow", "BtnBuy");
        }

        if (livesAddWindowBuyButton == null || subscribedLivesAddWindowBuyButton == livesAddWindowBuyButton)
        {
            return;
        }

        UnsubscribeLivesAddWindowBuyButton();
        livesAddWindowBuyButton.onClick.AddListener(BuyLivesAndCloseWindow);
        subscribedLivesAddWindowBuyButton = livesAddWindowBuyButton;
    }

    private void UnsubscribeContinueRaceButton()
    {
        if (subscribedContinueRaceButton == null)
        {
            return;
        }

        subscribedContinueRaceButton.onClick.RemoveListener(ShowMain);
        subscribedContinueRaceButton = null;
    }

    private void UnsubscribeMergeGameButton()
    {
        if (subscribedMergeGameButton == null)
        {
            return;
        }

        subscribedMergeGameButton.onClick.RemoveListener(TryLoadMergeGameScreen);
        subscribedMergeGameButton = null;
    }

    private void UnsubscribeMainLevelButton()
    {
        if (subscribedMainLevelButton == null)
        {
            return;
        }

        subscribedMainLevelButton.onClick.RemoveListener(TryLoadMergeGameScreen);
        subscribedMainLevelButton = null;
    }

    private void UnsubscribePlayerLivesButton()
    {
        if (subscribedPlayerLivesButton == null)
        {
            return;
        }

        subscribedPlayerLivesButton.onClick.RemoveListener(HandlePlayerLivesButtonClicked);
        subscribedPlayerLivesButton = null;
    }

    private void UnsubscribeLevelButtons()
    {
        for (var i = 0; i < subscribedLevelButtons.Count; i++)
        {
            var levelButton = subscribedLevelButtons[i];
            if (levelButton != null)
            {
                levelButton.onClick.RemoveListener(TryLoadMergeGameScreen);
            }
        }

        subscribedLevelButtons.Clear();
    }

    private void UnsubscribeLivesAddWindowBuyButton()
    {
        if (subscribedLivesAddWindowBuyButton == null)
        {
            return;
        }

        subscribedLivesAddWindowBuyButton.onClick.RemoveListener(BuyLivesAndCloseWindow);
        subscribedLivesAddWindowBuyButton = null;
    }

    private void CacheDefaultPositions()
    {
        mainButtonContentDefaultPosition = GetAnchoredPosition(mainButtonContent);
        shopButtonContentDefaultPosition = GetAnchoredPosition(shopButtonContent);
        clanButtonContentDefaultPosition = GetAnchoredPosition(clanButtonContent);
        locationButtonContentDefaultPosition = GetAnchoredPosition(locationButtonContent);
        rankingButtonContentDefaultPosition = GetAnchoredPosition(rankingButtonContent);
    }

    private void UpdateButtonContentOffsets(ScreenId selectedScreen)
    {
        SetButtonContentOffset(mainButtonContent, mainButtonContentDefaultPosition, selectedScreen == ScreenId.Main);
        SetButtonContentOffset(shopButtonContent, shopButtonContentDefaultPosition, selectedScreen == ScreenId.Shop);
        SetButtonContentOffset(clanButtonContent, clanButtonContentDefaultPosition, selectedScreen == ScreenId.Clan);
        SetButtonContentOffset(locationButtonContent, locationButtonContentDefaultPosition, selectedScreen == ScreenId.Location);
        SetButtonContentOffset(rankingButtonContent, rankingButtonContentDefaultPosition, selectedScreen == ScreenId.Ranking);
    }

    private void SetButtonContentOffset(RectTransform content, Vector2 defaultPosition, bool isSelected)
    {
        if (content == null)
        {
            return;
        }

        var targetPosition = defaultPosition;
        if (isSelected)
        {
            targetPosition.y += selectedContentYOffset;
        }

        content.anchoredPosition = targetPosition;
    }

    private static Vector2 GetAnchoredPosition(RectTransform target)
    {
        return target != null ? target.anchoredPosition : Vector2.zero;
    }

    private static RectTransform FindButtonContent(Button button)
    {
        if (button == null)
        {
            return null;
        }

        return button.transform.Find("Content") as RectTransform;
    }

    private static TMP_Text FindTextInButton(Button button, string textName)
    {
        if (button == null)
        {
            return null;
        }

        var texts = button.GetComponentsInChildren<TMP_Text>(true);
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

    private static TMP_Text FindSceneTextInRoot(string rootName, string textName)
    {
        var root = FindSceneObject(rootName);
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

    private static TMP_Text FindSceneTextByPath(string rootName, string path)
    {
        var root = FindSceneObject(rootName);
        if (root == null)
        {
            return null;
        }

        var target = root.transform.Find(path);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static RectTransform FindSceneRectByPath(string rootName, string path)
    {
        var root = FindSceneObject(rootName);
        if (root == null)
        {
            return null;
        }

        var target = root.transform.Find(path);
        return target as RectTransform;
    }

    private static RectTransform FindSceneRectInRoot(string rootName, string rectName)
    {
        var root = FindSceneObject(rootName);
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

    private static Button FindSceneButton(string objectName)
    {
        var target = FindSceneObject(objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static List<Button> FindSceneButtons(string objectName)
    {
        var result = new List<Button>();
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null || target.name != objectName)
            {
                continue;
            }

            var scene = target.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            var button = target.GetComponent<Button>();
            if (button != null && !result.Contains(button))
            {
                result.Add(button);
            }
        }

        return result;
    }

    private static Button FindSceneButtonInRoot(string rootName, string buttonName)
    {
        var root = FindSceneObject(rootName);
        if (root == null)
        {
            return null;
        }

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private static Button FindSceneButtonOrChildButtonInRoot(string rootName, string objectName)
    {
        var root = FindSceneObject(rootName);
        if (root == null)
        {
            return null;
        }

        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            if (target == null || target.name != objectName)
            {
                continue;
            }

            var button = target.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }

            return target.GetComponentInChildren<Button>(true);
        }

        return null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (var i = 0; i < transforms.Length; i++)
        {
            var target = transforms[i];
            var scene = target.gameObject.scene;

            if (!scene.IsValid() || !scene.isLoaded || target.name != objectName)
            {
                continue;
            }

            return target.gameObject;
        }

        return null;
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target == null || target.activeSelf == isActive)
        {
            return;
        }

        target.SetActive(isActive);
    }

#if UNITY_EDITOR
    private void FillLevelStartLevelsFromProjectFolder()
    {
        if (string.IsNullOrWhiteSpace(levelStartLevelsAssetFolder))
        {
            levelStartLevelsAssetFolder = DefaultLevelsAssetFolder;
        }

        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(LevelData)}", new[] { levelStartLevelsAssetFolder });
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

        if (HaveSameLevels(levelStartLevels, foundLevels))
        {
            return;
        }

        levelStartLevels.Clear();
        levelStartLevels.AddRange(foundLevels);
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
