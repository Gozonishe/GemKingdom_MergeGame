# MergeGameScene setup

Use the automatic builder from Unity:

1. Open Unity.
2. Run `Tools > Merge-2 Puzzle > Build Merge Game Scene`.
3. Open `Assets/_Project/Scenes/MergeGameScene.unity`.
4. Create `MergeItemData` assets in `Assets/_Project/ScriptableObjects/Items`.
5. Assign low-level item data, usually level 1 and 2, to `BoardRoot > BoardManager > Spawnable Items`.
6. Fill `OrdersPanel > OrderManager > Active Orders`.
7. Assign real item sprites to each `MergeItemData.icon`.

Generated scene hierarchy:

```text
Canvas
  SafeArea
    TopBar
      CoinsText
      EnergyText
      StarsText
    OrdersPanel
      OrderView_1
      OrderView_2
      OrderView_3
    BoardRoot
      GridLayoutGroup
      BoardCell prefabs will be spawned here
    BottomPanel
      AddEnergyButton
      ShuffleBoardButton
      CompleteRandomOrderButton
      RefillBoardButton
      ResetBoardButton
    RewardPopup
EventSystem
MainCamera
```

Manual setup checklist:

- `BoardRoot` must be a `RectTransform`.
- `BoardRoot` should have `GridLayoutGroup` and `BoardManager`.
- `BoardCell.prefab` must contain `RectTransform`, `Image` background and `BoardCell`.
- `MergeItem.prefab` must contain `RectTransform`, `Image`, `CanvasGroup`, `MergeItem` and `ItemDragHandler`.
- `TopBar` should have `CurrencyManager`, `EnergyManager`, `UICurrencyView` and `UIEnergyView`.
- `OrdersPanel` should have `OrderManager`, three `OrderView` objects and references to `BoardManager`, `CurrencyManager`, `RewardPopup`.
- `BottomPanel` should have `DebugPanel` and five test buttons.
- `RewardPopup` should have `RewardPopup`, title/coins/stars TMP texts and claim button.

Runtime references that still need manual game data:

- `BoardManager.spawnableItems`
- `OrderManager.activeOrders`
- `MergeItemData.icon`
- `MergeItemData.nextLevelItem`
