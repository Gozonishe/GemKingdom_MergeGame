using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelManager : MonoBehaviour
{
    private const string SavedLevelIndexKey = "MergeGame.NextLevelIndex";
    private const string DefaultMainMenuSceneName = "MainMenuScreen";

    [Header("Levels")]
    [SerializeField] private List<LevelData> levels = new List<LevelData>();
    [SerializeField] private int currentLevelIndex;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private EnergyManager energyManager;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private RewardPopup rewardPopup;
    [SerializeField] private TMP_Text levelText;

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;

    private bool isLoadingLevel;
    private bool levelCompleteHandled;
    private bool waitingRewardPopupClose;

    public IReadOnlyList<LevelData> Levels => levels;
    public int CurrentLevelIndex => currentLevelIndex;
    public LevelData CurrentLevel => IsInsideLevels(currentLevelIndex) ? levels[currentLevelIndex] : null;

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
        if (isLoadingLevel || levelCompleteHandled || orderManager == null)
        {
            return;
        }

        if (orderManager.AreAllOrdersClaimed)
        {
            levelCompleteHandled = true;
            CompleteCurrentLevelAndReturnToMenu();
        }
    }

    private void OnDestroy()
    {
        if (rewardPopup != null)
        {
            rewardPopup.Hidden -= HandleRewardPopupHidden;
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
        waitingRewardPopupClose = false;
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
            boardManager.SetSpawnableItems(level.SpawnableItems);
            boardManager.ResetBoard();
        }
        else
        {
            Debug.LogError($"{nameof(LevelManager)} on '{name}' cannot apply board balance because {nameof(boardManager)} is not assigned.", this);
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

        if (rewardPopup == null)
        {
            rewardPopup = FindFirstObjectByType<RewardPopup>(FindObjectsInactive.Include);
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

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    private bool IsInsideLevels(int index)
    {
        return index >= 0 && index < levels.Count;
    }

    private void RefreshLevelText(LevelData level)
    {
        if (levelText == null)
        {
            levelText = CreateDefaultLevelText();
        }

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

    private TMP_Text CreateDefaultLevelText()
    {
        var parent = transform as RectTransform;
        if (parent == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            parent = canvas != null ? canvas.transform as RectTransform : null;
        }

        if (parent == null)
        {
            return null;
        }

        var textObject = new GameObject("LevelText", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        var rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.35f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.65f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 32f;
        text.color = Color.white;
        text.raycastTarget = false;

        var layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        return text;
    }
}
