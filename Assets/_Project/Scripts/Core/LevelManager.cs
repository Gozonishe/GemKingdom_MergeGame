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
    [SerializeField] private RewardPopup rewardPopup;
    [SerializeField] private TMP_Text levelText;
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

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;

    private bool isLoadingLevel;
    private bool levelCompleteHandled;
    private bool levelLoseHandled;
    private bool waitingRewardPopupClose;
    private bool waitingLoosePanelTap;

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

        if (levels.Count == 0)
        {
            Debug.LogWarning($"{nameof(LevelManager)} on '{name}' has no levels assigned.", this);
            return;
        }

        var savedLevelIndex = GetSavedLevelIndex(currentLevelIndex);
        LoadLevel(Mathf.Clamp(savedLevelIndex, 0, levels.Count - 1));
    }

    private void Update()
    {
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
            ShowLoosePopup();
        }
    }

    private void OnDestroy()
    {
        if (rewardPopup != null)
        {
            rewardPopup.Hidden -= HandleRewardPopupHidden;
        }

        if (loosePopupQuitButton != null)
        {
            loosePopupQuitButton.onClick.RemoveListener(LoadMainMenuScene);
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
        waitingRewardPopupClose = false;
        waitingLoosePanelTap = false;
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
            boardManager.SetBoardSize(level.BoardColumns, level.BoardRows);
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

        if (rewardPopup == null)
        {
            rewardPopup = FindFirstObjectByType<RewardPopup>(FindObjectsInactive.Include);
        }

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

        if (levelText == null)
        {
            levelText = FindLevelText();
        }
    }

    private void CompleteCurrentLevelAndReturnToMenu()
    {
        SaveNextLevelIndex();

        if (rewardPopup != null && rewardPopup.IsVisible)
        {
            waitingRewardPopupClose = true;
            rewardPopup.Hidden -= HandleRewardPopupHidden;
            rewardPopup.Hidden += HandleRewardPopupHidden;
            return;
        }

        LoadMainMenuScene();
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

    private void HandleRewardPopupHidden()
    {
        if (rewardPopup != null)
        {
            rewardPopup.Hidden -= HandleRewardPopupHidden;
        }

        if (!waitingRewardPopupClose)
        {
            return;
        }

        waitingRewardPopupClose = false;
        LoadMainMenuScene();
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
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    private bool IsOutOfMoves()
    {
        return energyManager != null
            && energyManager.CurrentEnergy <= 0
            && (orderManager == null || !orderManager.AreAllOrdersReadyOrClaimed)
            && (boardManager == null || !boardManager.IsBusy)
            && (rewardPopup == null || !rewardPopup.IsVisible);
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
