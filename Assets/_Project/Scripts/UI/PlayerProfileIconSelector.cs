using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerProfileIconSelector : MonoBehaviour
{
    private const string IconsContainerName = "Icons";
    private const string MainIconName = "PlayerIconMain";
    private const string MainIconFallbackParentName = "PlayerView";
    private const string PlayerIconName = "PlayerIcon";
    private const string IconImageName = "IconImg";
    private const string GoldFrameName = "FrameGold";
    private const string BlueFrameName = "FrameBlue";
    private const string SaveButtonName = "BtnSaveProfile";
    private const string CancelButtonName = "ProfileBtnRed";
    private const string HudTopPanelName = "HudTopPanel";
    private const string HudProfileButtonName = "PlayerProfileButton";
    private const string DefaultOptionId = "PlayerIcon1";
    private const string SavedOptionIdKey = "PlayerProfile.SelectedIconId";
    private const float SelectionBounceScale = 1.08f;
    private const float SelectionBounceGrowDuration = 0.08f;
    private const float SelectionBounceSettleDuration = 0.1f;

    private sealed class IconOption
    {
        public string Id;
        public Button Button;
        public Image IconImage;
        public GameObject GoldFrame;
        public GameObject BlueFrame;
        public UnityAction ClickAction;
    }

    private readonly List<IconOption> options = new List<IconOption>(8);

    private Image mainIconImage;
    private Image hudIconImage;
    private GameObject mainGoldFrame;
    private GameObject mainBlueFrame;
    private Button saveButton;
    private Button cancelButton;
    private IconOption savedOption;
    private IconOption selectedOption;
    private IconOption selectionCapturedOnDisable;
    private Coroutine selectionBounceCoroutine;
    private Transform bouncingIconTransform;
    private Vector3 bouncingIconBaseScale;
    private bool profileEditSessionActive;
    private bool initialized;
    private bool initializationWarningLogged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeLoadedSelectors()
    {
        var selectors = FindObjectsByType<PlayerProfileIconSelector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (var i = 0; i < selectors.Length; i++)
        {
            selectors[i].TryInitialize();
        }
    }

    private void Awake()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        if (TryInitialize())
        {
            BeginProfileEditSession();
        }
    }

    private void OnDestroy()
    {
        StopSelectionBounce();

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            if (option.Button != null && option.ClickAction != null)
            {
                option.Button.onClick.RemoveListener(option.ClickAction);
            }
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(SaveProfile);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelProfileChanges);
        }
    }

    private void OnDisable()
    {
        StopSelectionBounce();

        if (!profileEditSessionActive)
        {
            return;
        }

        selectionCapturedOnDisable = selectedOption;
        RestoreSavedOptionToViews();
        profileEditSessionActive = false;
    }

    private bool TryInitialize()
    {
        if (initialized)
        {
            return true;
        }

        var iconsContainer = FindDescendant(transform, IconsContainerName);
        var mainIconRoot = FindDescendant(transform, MainIconName);
        var saveButtonTransform = FindDescendant(transform, SaveButtonName);
        var cancelButtonTransform = FindDescendant(transform, CancelButtonName);
        var hudTopPanel = FindDescendant(transform.root, HudTopPanelName);
        var hudProfileButton = FindDescendant(hudTopPanel, HudProfileButtonName);
        var hudIconImageTransform = FindDescendant(hudProfileButton, IconImageName);

        if (mainIconRoot == null)
        {
            var playerView = FindDescendant(transform, MainIconFallbackParentName);
            mainIconRoot = FindDescendant(playerView, PlayerIconName);
        }

        if (iconsContainer == null
            || mainIconRoot == null
            || saveButtonTransform == null
            || cancelButtonTransform == null
            || hudIconImageTransform == null
            || !TryGetIconParts(mainIconRoot, out mainIconImage, out mainGoldFrame, out mainBlueFrame))
        {
            LogInitializationWarning();
            return false;
        }

        saveButton = saveButtonTransform.GetComponent<Button>();
        cancelButton = cancelButtonTransform.GetComponent<Button>();
        hudIconImage = hudIconImageTransform.GetComponent<Image>();
        if (saveButton == null || cancelButton == null || hudIconImage == null)
        {
            LogInitializationWarning();
            return false;
        }

        mainGoldFrame.SetActive(true);
        mainBlueFrame.SetActive(false);

        var buttons = iconsContainer.GetComponentsInChildren<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (!TryGetIconParts(button.transform, out var iconImage, out var goldFrame, out var blueFrame))
            {
                continue;
            }

            var option = new IconOption
            {
                Id = button.name,
                Button = button,
                IconImage = iconImage,
                GoldFrame = goldFrame,
                BlueFrame = blueFrame
            };

            option.ClickAction = () => SelectOption(option);
            options.Add(option);
        }

        initialized = options.Count > 0;
        if (!initialized)
        {
            LogInitializationWarning();
            return false;
        }

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            option.Button.onClick.AddListener(option.ClickAction);
        }

        saveButton.onClick.AddListener(SaveProfile);
        cancelButton.onClick.AddListener(CancelProfileChanges);
        LoadSavedOption();
        RestoreSavedOptionToViews();
        return true;
    }

    private void SelectOption(IconOption option)
    {
        if (option == null || option.IconImage == null || mainIconImage == null)
        {
            return;
        }

        mainIconImage.sprite = option.IconImage.sprite;
        selectedOption = option;
        ApplyFrameStates();
        PlaySelectionBounce(option.Button.transform);
    }

    private void BeginProfileEditSession()
    {
        selectionCapturedOnDisable = null;
        RestoreSavedOptionToViews();
        profileEditSessionActive = true;
    }

    private void SaveProfile()
    {
        var optionToSave = profileEditSessionActive ? selectedOption : selectionCapturedOnDisable;
        if (optionToSave == null)
        {
            optionToSave = savedOption;
        }

        if (optionToSave != null)
        {
            savedOption = optionToSave;
            PlayerPrefs.SetString(SavedOptionIdKey, savedOption.Id);
            PlayerPrefs.Save();
        }

        selectionCapturedOnDisable = null;
        profileEditSessionActive = false;
        RestoreSavedOptionToViews();

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void CancelProfileChanges()
    {
        selectionCapturedOnDisable = null;
        profileEditSessionActive = false;
        RestoreSavedOptionToViews();

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void LoadSavedOption()
    {
        var savedOptionId = PlayerPrefs.GetString(SavedOptionIdKey, DefaultOptionId);
        savedOption = FindOption(savedOptionId);

        if (savedOption == null)
        {
            savedOption = FindOption(DefaultOptionId);
        }

        if (savedOption == null && options.Count > 0)
        {
            savedOption = options[0];
        }
    }

    private IconOption FindOption(string optionId)
    {
        for (var i = 0; i < options.Count; i++)
        {
            if (options[i].Id == optionId)
            {
                return options[i];
            }
        }

        return null;
    }

    private void RestoreSavedOptionToViews()
    {
        selectedOption = savedOption;

        if (savedOption != null && savedOption.IconImage != null)
        {
            var savedSprite = savedOption.IconImage.sprite;

            if (mainIconImage != null)
            {
                mainIconImage.sprite = savedSprite;
            }

            if (hudIconImage != null)
            {
                hudIconImage.sprite = savedSprite;
            }
        }

        ApplyFrameStates();
    }

    private void PlaySelectionBounce(Transform iconTransform)
    {
        if (iconTransform == null || !Application.isPlaying)
        {
            return;
        }

        StopSelectionBounce();
        bouncingIconTransform = iconTransform;
        bouncingIconBaseScale = iconTransform.localScale;
        selectionBounceCoroutine = StartCoroutine(AnimateSelectionBounce(iconTransform));
    }

    private IEnumerator AnimateSelectionBounce(Transform iconTransform)
    {
        var enlargedScale = bouncingIconBaseScale * SelectionBounceScale;
        var elapsed = 0f;

        while (elapsed < SelectionBounceGrowDuration)
        {
            if (iconTransform == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / SelectionBounceGrowDuration);
            iconTransform.localScale = Vector3.LerpUnclamped(
                bouncingIconBaseScale,
                enlargedScale,
                Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < SelectionBounceSettleDuration)
        {
            if (iconTransform == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / SelectionBounceSettleDuration);
            iconTransform.localScale = Vector3.LerpUnclamped(
                enlargedScale,
                bouncingIconBaseScale,
                Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        if (iconTransform != null)
        {
            iconTransform.localScale = bouncingIconBaseScale;
        }

        selectionBounceCoroutine = null;
        bouncingIconTransform = null;
    }

    private void StopSelectionBounce()
    {
        if (selectionBounceCoroutine != null)
        {
            StopCoroutine(selectionBounceCoroutine);
            selectionBounceCoroutine = null;
        }

        if (bouncingIconTransform != null)
        {
            bouncingIconTransform.localScale = bouncingIconBaseScale;
            bouncingIconTransform = null;
        }
    }

    private void ApplyFrameStates()
    {
        if (mainGoldFrame != null)
        {
            mainGoldFrame.SetActive(true);
        }

        if (mainBlueFrame != null)
        {
            mainBlueFrame.SetActive(false);
        }

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var isSelected = option == selectedOption;

            if (option.GoldFrame != null)
            {
                option.GoldFrame.SetActive(isSelected);
            }

            if (option.BlueFrame != null)
            {
                option.BlueFrame.SetActive(!isSelected);
            }
        }
    }

    private void LogInitializationWarning()
    {
        if (initializationWarningLogged)
        {
            return;
        }

        initializationWarningLogged = true;
        Debug.LogWarning(
            $"{nameof(PlayerProfileIconSelector)} on '{name}' could not find profile buttons, icons or the HUD icon.",
            this);
    }

    private static bool TryGetIconParts(
        Transform iconRoot,
        out Image iconImage,
        out GameObject goldFrame,
        out GameObject blueFrame)
    {
        iconImage = null;
        goldFrame = null;
        blueFrame = null;

        if (iconRoot == null)
        {
            return false;
        }

        var iconImageTransform = FindDescendant(iconRoot, IconImageName);
        var goldFrameTransform = FindDescendant(iconRoot, GoldFrameName);
        var blueFrameTransform = FindDescendant(iconRoot, BlueFrameName);

        if (iconImageTransform == null || goldFrameTransform == null || blueFrameTransform == null)
        {
            return false;
        }

        iconImage = iconImageTransform.GetComponent<Image>();
        goldFrame = goldFrameTransform.gameObject;
        blueFrame = blueFrameTransform.gameObject;
        return iconImage != null;
    }

    private static Transform FindDescendant(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var result = FindDescendant(root.GetChild(i), targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
