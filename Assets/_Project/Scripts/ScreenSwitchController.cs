using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private RectTransform mainButtonContent;
    [SerializeField] private float selectedMainContentYOffset = 50f;
    [SerializeField] private ScreenId defaultScreen = ScreenId.Main;
    [SerializeField] private bool showDefaultScreenOnAwake = true;

    private Vector2 mainButtonContentDefaultPosition;

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
        UpdateMainButtonContentOffset(screenId == ScreenId.Main);
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
        mainButtonContent = mainButtonContent != null ? mainButtonContent : FindButtonContent(mainButton);
    }

    private void CacheDefaultPositions()
    {
        if (mainButtonContent == null)
        {
            return;
        }

        mainButtonContentDefaultPosition = mainButtonContent.anchoredPosition;
    }

    private void UpdateMainButtonContentOffset(bool isSelected)
    {
        if (mainButtonContent == null)
        {
            return;
        }

        var targetPosition = mainButtonContentDefaultPosition;

        if (isSelected)
        {
            targetPosition.y += selectedMainContentYOffset;
        }

        mainButtonContent.anchoredPosition = targetPosition;
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
