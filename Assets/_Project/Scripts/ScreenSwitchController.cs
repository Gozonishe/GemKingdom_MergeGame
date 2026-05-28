using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class ScreenSwitchController : MonoBehaviour
{
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
    [SerializeField] private RectTransform mainButtonContent;
    [SerializeField] private RectTransform shopButtonContent;
    [SerializeField] private RectTransform clanButtonContent;
    [SerializeField] private RectTransform locationButtonContent;
    [SerializeField] private RectTransform rankingButtonContent;
    [FormerlySerializedAs("selectedMainContentYOffset")]
    [SerializeField] private float selectedContentYOffset = 50f;
    [SerializeField] private ScreenId defaultScreen = ScreenId.Main;
    [SerializeField] private bool showDefaultScreenOnAwake = true;

    private Vector2 mainButtonContentDefaultPosition;
    private Vector2 shopButtonContentDefaultPosition;
    private Vector2 clanButtonContentDefaultPosition;
    private Vector2 locationButtonContentDefaultPosition;
    private Vector2 rankingButtonContentDefaultPosition;

    private void Awake()
    {
        ResolveMissingReferences();
        CacheDefaultPositions();
        SubscribeButtons();

        if (showDefaultScreenOnAwake)
        {
            Show(defaultScreen);
        }
    }

    private void OnEnable()
    {
        transform.SetAsLastSibling();
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

        if (clanButton != null)
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
        UpdateButtonContentOffsets(screenId);
        transform.SetAsLastSibling();
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
        mainButtonContent = mainButtonContent != null ? mainButtonContent : FindButtonContent(mainButton);
        shopButtonContent = shopButtonContent != null ? shopButtonContent : FindButtonContent(shopButton);
        clanButtonContent = clanButtonContent != null ? clanButtonContent : FindButtonContent(clanButton);
        locationButtonContent = locationButtonContent != null ? locationButtonContent : FindButtonContent(locationButton);
        rankingButtonContent = rankingButtonContent != null ? rankingButtonContent : FindButtonContent(rankingButton);
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

    private static Button FindSceneButton(string objectName)
    {
        var target = FindSceneObject(objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static Button FindSceneButtonInRoot(string rootName, string buttonName)
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
            if (target.name == buttonName && target.TryGetComponent<Button>(out var button))
            {
                return button;
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
}
