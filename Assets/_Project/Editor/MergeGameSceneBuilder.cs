using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MergeGameSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/MergeGameScene.unity";
    private const string BoardCellPrefabPath = "Assets/_Project/Prefabs/Board/BoardCell.prefab";
    private const string MergeItemPrefabPath = "Assets/_Project/Prefabs/Items/MergeItem.prefab";
    private const string FullStoneDataPath = "Assets/_Project/ScriptableObjects/Items/Stone_dark/Item_Stone_Dark_Lvl_2.asset";
    private const string CrackedStoneDataPath = "Assets/_Project/ScriptableObjects/Items/Stone_dark/Item_Stone_Dark_Lvl_1.asset";

    [MenuItem("Tools/Merge-2 Puzzle/Build Merge Game Scene")]
    public static void BuildScene()
    {
        EnsureProjectFolders();

        var boardCellPrefab = GetOrCreateBoardCellPrefab();
        var mergeItemPrefab = GetOrCreateMergeItemPrefab();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MergeGameScene";

        CreateMainCamera();
        CreateEventSystem();

        var canvas = CreateCanvas();
        var safeArea = CreateUiObject("SafeArea", canvas.transform);
        StretchToParent(safeArea);

        var topBar = CreateTopBar(safeArea.transform, out var currencyView, out var energyView);
        var ordersPanel = CreateOrdersPanel(safeArea.transform, out var orderViews);
        var boardRoot = CreateBoardRoot(safeArea.transform);
        var bottomPanel = CreateBottomPanel(safeArea.transform, out var debugPanel);
        var rewardPopup = CreateRewardPopup(safeArea.transform);

        var energyManager = topBar.AddComponent<EnergyManager>();
        SetObjectReference(energyManager, "energyView", energyView);

        var currencyManager = topBar.AddComponent<CurrencyManager>();
        SetObjectReference(currencyManager, "currencyView", currencyView);

        var orderManager = ordersPanel.AddComponent<OrderManager>();
        SetObjectReference(orderManager, "boardManager", boardRoot.GetComponent<BoardManager>());
        SetObjectReference(orderManager, "currencyManager", currencyManager);
        SetObjectReference(orderManager, "rewardPopup", rewardPopup);
        SetObjectList(orderManager, "orderViews", orderViews);

        var boardManager = boardRoot.GetComponent<BoardManager>();
        SetObjectReference(boardManager, "boardRoot", boardRoot.transform);
        SetObjectReference(boardManager, "cellPrefab", boardCellPrefab);
        SetObjectReference(boardManager, "itemPrefab", mergeItemPrefab);
        SetObjectReference(boardManager, "fullStoneBlockerData", AssetDatabase.LoadAssetAtPath<MergeItemData>(FullStoneDataPath));
        SetObjectReference(boardManager, "crackedStoneBlockerData", AssetDatabase.LoadAssetAtPath<MergeItemData>(CrackedStoneDataPath));
        SetObjectReference(boardManager, "energyManager", energyManager);
        SetObjectReference(boardManager, "orderManager", orderManager);

        SetObjectReference(debugPanel, "boardManager", boardManager);
        SetObjectReference(debugPanel, "energyManager", energyManager);
        SetObjectReference(debugPanel, "orderManager", orderManager);

        // Manual setup still required:
        // 1. Create MergeItemData assets in Assets/_Project/ScriptableObjects/Items.
        // 2. Assign BoardManager.spawnableItems with low-level item data, usually level 1-2.
        // 3. Fill OrderManager.activeOrders with requiredItem, requiredAmount and rewards.
        // 4. Assign real sprites to each MergeItemData.icon.
        // 5. Tune layout sizes/colors to match the final art direction.

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Merge Game Scene", $"Scene created:\n{ScenePath}", "OK");
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder("Assets/_Project/Editor");
        EnsureFolder("Assets/_Project/Scenes");
        EnsureFolder("Assets/_Project/Prefabs");
        EnsureFolder("Assets/_Project/Prefabs/Board");
        EnsureFolder("Assets/_Project/Prefabs/Items");
        EnsureFolder("Assets/_Project/Prefabs/UI");
        EnsureFolder("Assets/_Project/ScriptableObjects");
        EnsureFolder("Assets/_Project/ScriptableObjects/Items");
    }

    private static BoardCell GetOrCreateBoardCellPrefab()
    {
        var existingPrefab = AssetDatabase.LoadAssetAtPath<BoardCell>(BoardCellPrefabPath);
        if (existingPrefab != null)
        {
            return existingPrefab;
        }

        var root = CreateUiObject("BoardCell", null);
        var image = root.AddComponent<Image>();
        image.color = new Color(0.13f, 0.18f, 0.25f, 0.85f);
        root.AddComponent<BoardCell>();
        SetRect(root, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(137.3f, 137.3f));

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, BoardCellPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<BoardCell>();
    }

    private static MergeItem GetOrCreateMergeItemPrefab()
    {
        var existingPrefab = AssetDatabase.LoadAssetAtPath<MergeItem>(MergeItemPrefabPath);
        if (existingPrefab != null)
        {
            return existingPrefab;
        }

        var root = CreateUiObject("MergeItem", null);
        var image = root.AddComponent<Image>();
        image.color = Color.white;
        root.AddComponent<CanvasGroup>();
        root.AddComponent<MergeItem>();
        root.AddComponent<ItemDragHandler>();
        SetRect(root, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(131.6f, 131.6f));

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, MergeItemPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<MergeItem>();
    }

    private static void CreateMainCamera()
    {
        var cameraObject = new GameObject("MainCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
        camera.orthographic = true;
        cameraObject.tag = "MainCamera";
    }

    private static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateTopBar(Transform parent, out UICurrencyView currencyView, out UIEnergyView energyView)
    {
        var topBar = CreateUiObject("TopBar", parent);
        AnchorTop(topBar, 0f, 120f);
        AddImage(topBar, new Color(0.08f, 0.12f, 0.18f, 0.95f));

        var coinsText = CreateText("CoinsText", topBar.transform, "Coins: 0", 36, TextAlignmentOptions.Left);
        AnchorStretch(coinsText.gameObject, new Vector2(24f, 0f), new Vector2(330f, 0f));

        var energyText = CreateText("MovesText", topBar.transform, "100", 36, TextAlignmentOptions.Center);
        AnchorStretch(energyText.gameObject, new Vector2(330f, 0f), new Vector2(330f, 0f));

        var starsText = CreateText("StarsText", topBar.transform, "Stars: 0", 36, TextAlignmentOptions.Right);
        AnchorStretch(starsText.gameObject, new Vector2(330f, 0f), new Vector2(24f, 0f));

        currencyView = topBar.AddComponent<UICurrencyView>();
        SetObjectReference(currencyView, "coinsText", coinsText);
        SetObjectReference(currencyView, "starsText", starsText);

        energyView = topBar.AddComponent<UIEnergyView>();
        SetObjectReference(energyView, "energyText", energyText);

        return topBar;
    }

    private static GameObject CreateOrdersPanel(Transform parent, out List<OrderView> orderViews)
    {
        var ordersPanel = CreateUiObject("OrdersPanel", parent);
        AnchorTop(ordersPanel, 130f, 220f);
        AddImage(ordersPanel, new Color(0.11f, 0.1f, 0.15f, 0.75f));

        var layout = ordersPanel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        orderViews = new List<OrderView>(3);
        for (var i = 0; i < 3; i++)
        {
            orderViews.Add(CreateOrderView($"OrderView_{i + 1}", ordersPanel.transform));
        }

        return ordersPanel;
    }

    private static OrderView CreateOrderView(string name, Transform parent)
    {
        var viewObject = CreateUiObject(name, parent);
        AddImage(viewObject, new Color(0.18f, 0.16f, 0.22f, 0.95f));
        viewObject.AddComponent<LayoutElement>().preferredWidth = 320f;

        var topContent = CreateUiObject("TopContent", viewObject.transform);
        AnchorStretch(topContent, new Vector2(8f, 4f), new Vector2(8f, 8f));
        var topRect = topContent.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 0.44f);
        topRect.anchorMax = Vector2.one;

        var claimContent = CreateUiObject("ClaimContent", viewObject.transform);
        AnchorStretch(claimContent, new Vector2(8f, 8f), new Vector2(8f, 4f));
        var claimRect = claimContent.GetComponent<RectTransform>();
        claimRect.anchorMin = Vector2.zero;
        claimRect.anchorMax = new Vector2(1f, 0.44f);

        var icon = CreateUiObject("RequiredItemIcon", topContent.transform);
        AddImage(icon, new Color(1f, 1f, 1f, 0.35f));
        AnchorStretch(icon, new Vector2(8f, 8f), new Vector2(6f, 8f));
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = new Vector2(0.36f, 1f);

        var amountText = CreateText("AmountText", topContent.transform, "0/0", 30, TextAlignmentOptions.Center);
        AnchorStretch(amountText.gameObject, new Vector2(6f, 0f), new Vector2(8f, 4f));
        var amountRect = amountText.GetComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(0.36f, 0.5f);
        amountRect.anchorMax = Vector2.one;

        var rewardText = CreateText("RewardText", topContent.transform, "0 coins  0 stars", 24, TextAlignmentOptions.Center);
        AnchorStretch(rewardText.gameObject, new Vector2(6f, 4f), new Vector2(8f, 0f));
        var rewardRect = rewardText.GetComponent<RectTransform>();
        rewardRect.anchorMin = new Vector2(0.36f, 0f);
        rewardRect.anchorMax = new Vector2(1f, 0.5f);

        var claimButton = CreateButton("ClaimButton", claimContent.transform, "Claim");
        StretchToParent(claimButton.gameObject);

        var orderView = viewObject.AddComponent<OrderView>();
        SetObjectReference(orderView, "requiredItemIcon", icon.GetComponent<Image>());
        SetObjectReference(orderView, "amountText", amountText);
        SetObjectReference(orderView, "rewardText", rewardText);
        SetObjectReference(orderView, "claimButton", claimButton);
        SetObjectReference(orderView, "topContent", topContent.GetComponent<RectTransform>());
        SetObjectReference(orderView, "claimContent", claimContent.GetComponent<RectTransform>());

        return orderView;
    }

    private static GameObject CreateBoardRoot(Transform parent)
    {
        var boardRoot = CreateUiObject("BoardRoot", parent);
        var rect = boardRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(943.8f, 1115.4f);
        rect.anchoredPosition = new Vector2(0f, -80f);

        boardRoot.AddComponent<BoardManager>();
        return boardRoot;
    }

    private static GameObject CreateBottomPanel(Transform parent, out DebugPanel debugPanel)
    {
        var bottomPanel = CreateUiObject("BottomPanel", parent);
        AnchorBottom(bottomPanel, 0f, 240f);
        AddImage(bottomPanel, new Color(0.08f, 0.1f, 0.14f, 0.95f));

        var layout = bottomPanel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 22, 22);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var addEnergyButton = CreateButton("AddEnergyButton", bottomPanel.transform, "Add Energy +10");
        var shuffleButton = CreateButton("ShuffleBoardButton", bottomPanel.transform, "Shuffle Board");
        var completeOrderButton = CreateButton("CompleteRandomOrderButton", bottomPanel.transform, "Complete Random Order");
        var refillButton = CreateButton("RefillBoardButton", bottomPanel.transform, "Refill Board");
        var resetButton = CreateButton("ResetBoardButton", bottomPanel.transform, "Reset Board");

        debugPanel = bottomPanel.AddComponent<DebugPanel>();
        SetObjectReference(debugPanel, "addEnergyButton", addEnergyButton);
        SetObjectReference(debugPanel, "shuffleBoardButton", shuffleButton);
        SetObjectReference(debugPanel, "completeRandomOrderButton", completeOrderButton);
        SetObjectReference(debugPanel, "refillBoardButton", refillButton);
        SetObjectReference(debugPanel, "resetBoardButton", resetButton);

        return bottomPanel;
    }

    private static RewardPopup CreateRewardPopup(Transform parent)
    {
        var root = CreateUiObject("RewardPopup", parent);
        StretchToParent(root);
        AddImage(root, new Color(0f, 0f, 0f, 0.65f));

        var panel = CreateUiObject("Panel", root.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 420f);
        panelRect.anchoredPosition = Vector2.zero;
        AddImage(panel, new Color(0.12f, 0.15f, 0.22f, 1f));

        var titleText = CreateText("TitleText", panel.transform, "Reward", 52, TextAlignmentOptions.Center);
        AnchorTop(titleText.gameObject, 24f, 90f);

        var coinsText = CreateText("CoinsText", panel.transform, "0", 42, TextAlignmentOptions.Center);
        AnchorStretch(coinsText.gameObject, new Vector2(40f, 130f), new Vector2(40f, 200f));

        var starsText = CreateText("StarsText", panel.transform, "0", 42, TextAlignmentOptions.Center);
        AnchorStretch(starsText.gameObject, new Vector2(40f, 210f), new Vector2(40f, 120f));

        var claimButton = CreateButton("ClaimButton", panel.transform, "Claim");
        AnchorBottom(claimButton.gameObject, 28f, 86f);

        var rewardPopup = root.AddComponent<RewardPopup>();
        SetObjectReference(rewardPopup, "root", root);
        SetObjectReference(rewardPopup, "titleText", titleText);
        SetObjectReference(rewardPopup, "coinsText", coinsText);
        SetObjectReference(rewardPopup, "starsText", starsText);
        SetObjectReference(rewardPopup, "claimButton", claimButton);
        root.SetActive(false);

        return rewardPopup;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        var buttonObject = CreateUiObject(name, parent);
        var image = AddImage(buttonObject, new Color(0.18f, 0.37f, 0.75f, 1f));
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        var labelText = CreateText("Text", buttonObject.transform, label, 28, TextAlignmentOptions.Center);
        StretchToParent(labelText.gameObject);
        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, int fontSize, TextAlignmentOptions alignment)
    {
        var textObject = CreateUiObject(name, parent);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Image AddImage(GameObject target, Color color)
    {
        var image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        image.color = color;
        return image;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void SetRect(GameObject target, Vector2 anchoredPosition, Vector2 pivot, Vector2 size)
    {
        var rect = target.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.pivot = pivot;
        rect.sizeDelta = size;
    }

    private static void StretchToParent(GameObject target)
    {
        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AnchorTop(GameObject target, float topOffset, float height)
    {
        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -topOffset - height);
        rect.offsetMax = new Vector2(0f, -topOffset);
    }

    private static void AnchorBottom(GameObject target, float bottomOffset, float height)
    {
        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(0f, bottomOffset);
        rect.offsetMax = new Vector2(0f, bottomOffset + height);
    }

    private static void AnchorStretch(GameObject target, Vector2 leftBottom, Vector2 rightTop)
    {
        var rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = leftBottom;
        rect.offsetMax = -rightTop;
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        var serializedObject = new SerializedObject(target);
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetObjectList<T>(Object target, string propertyName, IReadOnlyList<T> values)
        where T : Object
    {
        var serializedObject = new SerializedObject(target);
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = values.Count;
        for (var i = 0; i < values.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var slashIndex = path.LastIndexOf('/');
        var parent = path.Substring(0, slashIndex);
        var folderName = path.Substring(slashIndex + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
