# Level Progression Index

Рабочий индекс существующих уровней проекта. Файл нужен для генерации новых уровней, проверки прогрессии сложности и сравнения будущих уровней с уже созданными.

## Как использовать

- `Merge_Level_Design_Rules.md` хранит правила механик.
- `Level_Progression_Index.md` хранит краткое описание конкретных уровней.
- При генерации новых уровней достаточно прикладывать rules-документ и этот index. Полные `.asset` нужны только для прямой правки Unity-данных.

## Ограничения данных

- Item GUID уже сопоставлены с реальными Unity asset names в разделе `Item Mapping`. В карточках уровней старый формат `Item_01[guid]` пока сохранён как техническая ссылка, но при новом балансе нужно использовать `Short Name` или `Real Unity Name`.
- `objectiveType` оставлен числом, потому что точная enum-расшифровка не хранится в этих файлах.
- `Difficulty`, `Focus` и `Player Lesson` — дизайнерская интерпретация на основе board size, mask, moves, goals и количества ресурсов.

## Item Mapping

Этот раздел связывает старые технические алиасы `Item_01`, `Item_02` и т.д. с реальными Unity asset names.  
В новых балансных описаниях лучше использовать `Short Name`, а не `Item_XX`.

| Alias | GUID | Real Unity Name | Short Name | Type | Chain / Group | Tier |
|---|---|---|---|---|---|---:|
| `Item_01` | `50e17eb973d2a4f4887adf1dc8160eb9` | `Item_Crystal_Lvl_2` | Crystal L2 | Resource | Crystal | 2 |
| `Item_02` | `da200a52036e13f44b6d84a28c6de0e9` | `Item_Stone_Dark_Lvl_2` | Dark Stone L2 | Destructible Objective | Stone Dark | 2 |
| `Item_03` | `67b4da7ec2bd3f0409dd1296ee237360` | `Item_Crystal_Lvl_3` | Crystal L3 | Resource | Crystal | 3 |
| `Item_04` | `a33c1beba84795444a695fc2ce6cf7e1` | `Item_Emerland_Lvl_2` | Emerald L2 | Resource | Emerald | 2 |
| `Item_05` | `c5f8e9419e692b54ca62a99be1e5350e` | `Item_Emerland_Lvl_3` | Emerald L3 | Resource | Emerald | 3 |
| `Item_06` | `4a536f1015a7c2a4ea47c94d21d15ce3` | `Item_Dynamite_Lvl_1` | Dynamite L1 | Special Tool | Dynamite | 1 |
| `Item_07` | `0cc9490e02953a44b9eb0a4ce4e9a542` | `Item_Crystal_Lvl_4` | Crystal L4 | Resource | Crystal | 4 |
| `Item_08` | `c5824e6174fdfbe49b3d166fdd74fac0` | `Item_Gold_Lvl_2` | Gold L2 | Resource | Gold | 2 |
| `Item_09` | `6fdcf7b0fce727a439cf287b6c25ff6d` | `Item_Gold_Lvl_3` | Gold L3 | Resource | Gold | 3 |
| `Item_10` | `0636a8cab37768e49ae39148199c2359` | `Item_Gold_Lvl_4` | Gold L4 | Resource | Gold | 4 |
| `Item_11` | `8cfa7b3c4e357544d96e8c7553d758d1` | `Item_Ruby_Lvl_2` | Ruby L2 | Resource | Ruby | 2 |
| `Item_12` | `9fdd0c216bb5e4943a30262ba4314a1a` | `Item_Ruby_Lvl_3` | Ruby L3 | Resource | Ruby | 3 |
| `Item_13` | `4e93026ec1c472243a7daa62cb04c336` | `Item_Ruby_Lvl_4` | Ruby L4 | Resource | Ruby | 4 |
| `Item_14` | `63169e2b46fdeda4a99c2f6186d6658d` | `Item_Crystal_Lvl_5` | Crystal L5 | Resource | Crystal | 5 |
| `Item_15` | `aa8b132dd0e1a1d4e8f68ef0bc74faff` | `Item_Gold_Lvl_5` | Gold L5 | Resource | Gold | 5 |
| `Item_16` | `300364dda5aedd94aa6b010fcf21288c` | `Item_Emerland_Lvl_4` | Emerald L4 | Resource | Emerald | 4 |
| `Item_17` | `1f1ce725e39b4be9b5847f7b46a2258f` | `Item_Spider` | Spider | Dynamic Blocker | Spider | - |

### Naming notes

- В проекте сейчас используется написание `Emerland`. В дизайн-документах можно писать `Emerald`, но в Unity asset name пока оставлять как есть, чтобы не ломать ссылки.
- `Item_02 / Dark Stone L2` часто встречается как `ObjectiveType 1`. Это не collect-resource: цель должна читаться как `Destroy / Clear Dark Stone L2 xN`, а не `Collect Dark Stone L2 xN`.
- `Item_06 / Dynamite L1` встречается в spawn weights как special/tool item.
- `Item_17 / Spider` — динамический blocker и основная новая механика блока 21-30.

### Example readable goal format

Было:

```text
Goals: Type 0: Item_14[63169e2b] x1; Type 1: Item_02[da200a52] x5
```

Лучше писать:

```text
Goals:
- Collect Crystal L5 x1
- Destroy Dark Stone L2 x5
```

## Summary 1-30

| Level | Difficulty | Focus | Board | Active | Moves | Mechanics | Player Lesson | Notes |
|---:|---|---|---|---:|---:|---|---|---|
| 1 | Simple | Simple Collect | 4x4 | 100% | 7 | Simple Collect | Базовый merge на маленьком открытом поле. | OK |
| 2 | Simple | Simple Collect | 4x4 | 100% | 8 | Simple Collect | Закрепление базового collect-goal. | OK |
| 3 | Simple+ | Multi Collect | 5x5 | 100% | 14 | Simple Collect | Первое расширение поля и две collect-цели. | OK |
| 4 | Simple+ | Board Shape | 5x5 | 84% | 19 | Board Shape, ObjectiveType 1, Multi-resource spawn | Первое знакомство с простой формой поля и дополнительной целью. | OK |
| 5 | Simple+ | Board Shape | 5x5 | 92% | 23 | Board Shape, ObjectiveType 1, Multi-resource spawn | Форма с лёгкими вырезами, больше целей и стартовых предметов. | OK |
| 6 | Hard | Board Shape | 6x6 | 89% | 25 | Board Shape, ObjectiveType 1, Multi-resource spawn | 6x6 с центральными вырезами; учит играть вокруг пустых клеток. | OK |
| 7 | Hard | Board Shape / Multi Collect | 6x6 | 89% | 28 | Board Shape, ObjectiveType 1, Multi-resource spawn | 6x6 cut corners, несколько целей и цепочек. | OK |
| 8 | Hard | Multi Collect | 6x6 | 83% | 31 | Board Shape, ObjectiveType 1, Multi-resource spawn | Много ресурсов/целей; требуется проверить boardMask, в файле 5 строк при boardRows=6. | Mask rows 5 != boardRows 6 |
| 9 | Hard | Board Shape / Multi Collect | 6x6 | 89% | 32 | Board Shape, ObjectiveType 1, Multi-resource spawn | Форма с центральным сужением, несколько ресурсных цепочек. | OK |
| 10 | Hard | Multi Collect | 6x6 | 83% | 36 | Board Shape, ObjectiveType 1, Multi-resource spawn | Усиленный collect-уровень с 4 целями; требуется проверить boardMask. | Mask rows 5 != boardRows 6 |
| 11 | Hard | Limited Space | 6x6 | 67% | 34 | Board Shape, ObjectiveType 1, Multi-resource spawn | Самое сильное ограничение пространства: 24 активные клетки из 36. | Low active cell % |
| 12 | Hard | High Tier / Multi Collect | 6x6 | 83% | 42 | Board Shape, ObjectiveType 1, Multi-resource spawn | Длинные цели и больше ходов; требуется проверить boardMask. | Mask rows 5 != boardRows 6 |
| 13 | Hard | Multi Collect | 5x6 | 100% | 39 | ObjectiveType 1, Multi-resource spawn | Открытое 5x6 поле с несколькими целями и 6 единицами ObjectiveType 1. | OK |
| 14 | Hard | High Tier / Multi Collect | 6x6 | 83% | 45 | Board Shape, ObjectiveType 1, Multi-resource spawn | Большой набор стартовых предметов и высокие цели; требуется проверить boardMask. | Mask rows 5 != boardRows 6 |
| 15 | Hard | Board Shape / Chapter Check | 6x6 | 89% | 47 | Board Shape, ObjectiveType 1, Multi-resource spawn | Форма с диагональными вырезами, много целей; хороший checkpoint раннего блока. | OK |
| 16 | Hard | Board Shape / Multi Collect | 6x6 | 89% | 42 | Board Shape, ObjectiveType 1 | Форма с боковыми вырезами и 3 цели; закрепляет игру на 6x6 с несколькими ресурсами. | OK |
| 17 | Hard | Stones / Blockers | 6x6 | 94% | 44 | Board Shape, Stones, ObjectiveType 1 | Первый сильный stone-блок: 4 камня в центре, игрок учится планировать merge рядом с blockers. | OK |
| 18 | Hard | Board Shape / Multi Collect | 6x6 | 89% | 49 | Board Shape, ObjectiveType 1 | Форма с двумя боковыми сужениями и повышенной длиной целей; тренирует управление пространством. | OK |
| 19 | Hard | Stones / Blockers | 6x6 | 100% | 51 | Stones, ObjectiveType 1 | Диагональная линия из 6 камней: уровень про расчистку и доступ к полю через merge рядом. | OK |
| 20 | Hard | Board Shape / Multi Collect | 6x6 | 89% | 56 | Board Shape, ObjectiveType 1 | Итоговый ранний multi-goal уровень: большое поле, много стартовых предметов и длинные цели. | OK |
| 21 | Hard- | New Special / Spider Block Intro | 6x6 | 100% | 42 | ObjectiveType 1, New special Item_17 | Начало нового блока: появляется новый специальный Item_17 в стартовой раскладке/цели; поле пока открытое. | OK |
| 22 | Hard | New Special / Spider Block | 6x6 | 100% | 44 | Stones, ObjectiveType 1, New special Item_17 | Новый специальный Item_17 сочетается с центральными камнями; аккуратный переход к spider/special-блоку. | OK |
| 23 | Hard | New Special / Spider Block | 6x6 | 89% | 49 | Board Shape, ObjectiveType 1, New special Item_17 | Новый специальный Item_17 + форма с боковыми вырезами; игрок планирует вокруг ограничений пространства. | OK |
| 24 | Hard | New Special / Spider Block | 6x6 | 100% | 51 | ObjectiveType 1, New special Item_17 | Открытое поле без boardMask: усиленный multi-goal уровень с Item_17 как дополнительной целью. | Empty boardMask -> assumed full rectangle fallback |
| 25 | Hard | New Special / Spider Block | 6x6 | 100% | 56 | ObjectiveType 1, New special Item_17 | Итоговый уровень блока 21–25: много целей, Item_17 x2 и высокий запас ходов. | Empty boardMask -> assumed full rectangle fallback |
| 26 | Hard | Two Spiders Intro | 6x6 | 100% | 50 | ObjectiveType 1, Spider x2, Multi-resource spawn | Игрок впервые явно работает с двумя пауками на открытом поле: давление выше, но пространство честное. | OK; 2 spiders from boardMask; Dark Stone L2 is destroy objective, not collect. |
| 27 | Hard | Two Spiders + Stones Soft | 6x6 | 100% | 54 | Stones, ObjectiveType 1, Spider x2, Multi-resource spawn | Два паука и несколько камней вместе ограничивают пространство, но форма поля остаётся простой. | OK; 2 spiders + 3 stones from boardMask; Dark Stone L2 is destroy objective. |
| 28 | Hard+ | Two Spiders + Limited Space | 6x6 | 78% | 52 | Board Shape, ObjectiveType 1, Spider x2, Multi-resource spawn | Два паука становятся опаснее из-за меньшего количества активных клеток и узких зон. | OK; 2 spiders; active cells 28/36, not 24/36. |
| 29 | Very Hard- | Spider Swarm | 6x6 | 100% | 33 | Spider Swarm, ObjectiveType 1, Multi-resource spawn | Специальный уровень: главная цель — очистить поле от большого количества пауков. | OK; Spider Swarm with 7 spiders; no collect goals, only remove/destroy objectives. |
| 30 | Very Hard- | Spider Chapter Finale | 6x6 | 89% | 55 | Board Shape, Stones, ObjectiveType 1, Spider x2, Multi-resource spawn | Финальная проверка spider-блока: два паука, камни, форма поля и high-tier цели одновременно. | OK; finale with 2 spiders + 2 stones; Dark Stone L2 is destroy objective. |

## Detailed level cards
### Level 001

```text
File: Level_001(8).asset
Difficulty: Simple
Focus: Simple Collect
Board Size: 4x4
Board Shape: Full rectangle
Active Cells: 16 / 16 (100%)
Moves: 7
Main Mechanics: Simple Collect
Goals: Type 0: Item_03[67b4da7e] x2
Initial Items: Item_01[50e17eb9] x3, Item_02[da200a52] x3
Spawn Weights: Item_01[50e17eb9]:1
Player Lesson: Базовый merge на маленьком открытом поле.
Notes / Validation: OK
Board Mask:
1111
1111
1111
1111
```

### Level 002

```text
File: Level_002(7).asset
Difficulty: Simple
Focus: Simple Collect
Board Size: 4x4
Board Shape: Full rectangle
Active Cells: 16 / 16 (100%)
Moves: 8
Main Mechanics: Simple Collect
Goals: Type 0: Item_03[67b4da7e] x3
Initial Items: Item_01[50e17eb9] x2, Item_02[da200a52] x4
Spawn Weights: Item_01[50e17eb9]:1
Player Lesson: Закрепление базового collect-goal.
Notes / Validation: OK
Board Mask:
1111
1111
1111
1111
```

### Level 003

```text
File: Level_003(7).asset
Difficulty: Simple+
Focus: Multi Collect
Board Size: 5x5
Board Shape: Full rectangle
Active Cells: 25 / 25 (100%)
Moves: 14
Main Mechanics: Simple Collect
Goals: Type 0: Item_03[67b4da7e] x2; Type 0: Item_05[c5f8e941] x2
Initial Items: Item_01[50e17eb9] x5, Item_04[a33c1beb] x4
Spawn Weights: Item_01[50e17eb9]:11, Item_04[a33c1beb]:9
Player Lesson: Первое расширение поля и две collect-цели.
Notes / Validation: OK
Board Mask:
11111
11111
11111
11111
11111
```

### Level 004

```text
File: Level_004(7).asset
Difficulty: Simple+
Focus: Board Shape
Board Size: 5x5
Board Shape: Cut corners / shaped field
Active Cells: 21 / 25 (84%)
Moves: 19
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_07[0cc9490e] x1; Type 0: Item_05[c5f8e941] x1; Type 1: Item_02[da200a52] x1
Initial Items: Item_01[50e17eb9] x6, Item_03[67b4da7e] x1, Item_04[a33c1beb] x4, Item_02[da200a52] x1
Spawn Weights: Item_01[50e17eb9]:11, Item_03[67b4da7e]:1, Item_04[a33c1beb]:6, Item_02[da200a52]:1, Item_06[4a536f10]:1
Player Lesson: Первое знакомство с простой формой поля и дополнительной целью.
Notes / Validation: OK
Board Mask:
01110
11111
11111
11111
01110
```

### Level 005

```text
File: Level_005(7).asset
Difficulty: Simple+
Focus: Board Shape
Board Size: 5x5
Board Shape: Cut corners / shaped field
Active Cells: 23 / 25 (92%)
Moves: 23
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_07[0cc9490e] x1; Type 0: Item_05[c5f8e941] x2; Type 1: Item_02[da200a52] x3
Initial Items: Item_01[50e17eb9] x6, Item_03[67b4da7e] x1, Item_04[a33c1beb] x5, Item_02[da200a52] x3
Spawn Weights: Item_01[50e17eb9]:10, Item_04[a33c1beb]:5, Item_03[67b4da7e]:1, Item_05[c5f8e941]:1, Item_06[4a536f10]:1, Item_02[da200a52]:2
Player Lesson: Форма с лёгкими вырезами, больше целей и стартовых предметов.
Notes / Validation: OK
Board Mask:
11011
11111
11111
11111
11011
```

### Level 006

```text
File: Level_006(6).asset
Difficulty: Hard
Focus: Board Shape
Board Size: 6x6
Board Shape: Cutouts / tunnels
Active Cells: 32 / 36 (89%)
Moves: 25
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_07[0cc9490e] x1; Type 0: Item_09[6fdcf7b0] x3; Type 1: Item_02[da200a52] x3
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x1, Item_04[a33c1beb] x3, Item_08[c5824e61] x4, Item_02[da200a52] x3
Spawn Weights: Item_01[50e17eb9]:8, Item_08[c5824e61]:6, Item_04[a33c1beb]:3, Item_03[67b4da7e]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: 6x6 с центральными вырезами; учит играть вокруг пустых клеток.
Notes / Validation: OK
Board Mask:
111111
111111
110011
110011
111111
111111
```

### Level 007

```text
File: Level_007(6).asset
Difficulty: Hard
Focus: Board Shape / Multi Collect
Board Size: 6x6
Board Shape: Cut corners / shaped field
Active Cells: 32 / 36 (89%)
Moves: 28
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_10[0636a8ca] x1; Type 0: Item_05[c5f8e941] x2; Type 1: Item_02[da200a52] x4
Initial Items: Item_08[c5824e61] x6, Item_09[6fdcf7b0] x1, Item_04[a33c1beb] x4, Item_01[50e17eb9] x3, Item_02[da200a52] x4
Spawn Weights: Item_08[c5824e61]:9, Item_04[a33c1beb]:5, Item_01[50e17eb9]:3, Item_09[6fdcf7b0]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: 6x6 cut corners, несколько целей и цепочек.
Notes / Validation: OK
Board Mask:
011110
111111
111111
111111
111111
011110
```

### Level 008

```text
File: Level_008(6).asset
Difficulty: Hard
Focus: Multi Collect
Board Size: 6x6
Board Shape: Cut corners / shaped field
Active Cells: 30 / 36 (83%)
Moves: 31
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_07[0cc9490e] x1; Type 0: Item_10[0636a8ca] x1; Type 1: Item_02[da200a52] x4
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x1, Item_08[c5824e61] x6, Item_09[6fdcf7b0] x1, Item_04[a33c1beb] x3, Item_02[da200a52] x4
Spawn Weights: Item_01[50e17eb9]:7, Item_08[c5824e61]:7, Item_04[a33c1beb]:2, Item_03[67b4da7e]:1, Item_09[6fdcf7b0]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: Много ресурсов/целей; требуется проверить boardMask, в файле 5 строк при boardRows=6.
Notes / Validation: Mask rows 5 != boardRows 6
Board Mask:
111111
111111
111111
111111
111111
```

### Level 009

```text
File: Level_009(6).asset
Difficulty: Hard
Focus: Board Shape / Multi Collect
Board Size: 6x6
Board Shape: Cut corners / shaped field
Active Cells: 32 / 36 (89%)
Moves: 32
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_10[0636a8ca] x1; Type 0: Item_12[9fdd0c21] x1; Type 1: Item_02[da200a52] x4
Initial Items: Item_08[c5824e61] x5, Item_09[6fdcf7b0] x1, Item_11[8cfa7b3c] x4, Item_01[50e17eb9] x3, Item_04[a33c1beb] x2, Item_02[da200a52] x4
Spawn Weights: Item_08[c5824e61]:7, Item_11[8cfa7b3c]:6, Item_01[50e17eb9]:2, Item_04[a33c1beb]:1, Item_09[6fdcf7b0]:1, Item_12[9fdd0c21]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: Форма с центральным сужением, несколько ресурсных цепочек.
Notes / Validation: OK
Board Mask:
111111
111111
011110
011110
111111
111111
```

### Level 010

```text
File: Level_010(6).asset
Difficulty: Hard
Focus: Multi Collect
Board Size: 6x6
Board Shape: Cut corners / shaped field
Active Cells: 30 / 36 (83%)
Moves: 36
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_07[0cc9490e] x1; Type 0: Item_10[0636a8ca] x1; Type 0: Item_12[9fdd0c21] x2; Type 1: Item_02[da200a52] x5
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x1, Item_08[c5824e61] x5, Item_09[6fdcf7b0] x1, Item_11[8cfa7b3c] x4, Item_02[da200a52] x4
Spawn Weights: Item_01[50e17eb9]:7, Item_08[c5824e61]:7, Item_11[8cfa7b3c]:5, Item_04[a33c1beb]:1, Item_03[67b4da7e]:1, Item_09[6fdcf7b0]:1, Item_12[9fdd0c21]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: Усиленный collect-уровень с 4 целями; требуется проверить boardMask.
Notes / Validation: Mask rows 5 != boardRows 6
Board Mask:
111111
111111
111111
111111
111111
```

### Level 011

```text
File: Level_011(5).asset
Difficulty: Hard
Focus: Limited Space
Board Size: 6x6
Board Shape: Limited space / diamond
Active Cells: 24 / 36 (67%)
Moves: 34
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_13[4e93026e] x1; Type 0: Item_10[0636a8ca] x1; Type 1: Item_02[da200a52] x5
Initial Items: Item_11[8cfa7b3c] x6, Item_12[9fdd0c21] x1, Item_08[c5824e61] x5, Item_09[6fdcf7b0] x1, Item_01[50e17eb9] x2, Item_04[a33c1beb] x1, Item_02[da200a52] x3
Spawn Weights: Item_11[8cfa7b3c]:8, Item_08[c5824e61]:7, Item_01[50e17eb9]:2, Item_04[a33c1beb]:1, Item_12[9fdd0c21]:1, Item_09[6fdcf7b0]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: Самое сильное ограничение пространства: 24 активные клетки из 36.
Notes / Validation: Low active cell %
Board Mask:
001100
011110
111111
111111
011110
001100
```

### Level 012

```text
File: Level_012(5).asset
Difficulty: Hard
Focus: High Tier / Multi Collect
Board Size: 6x6
Board Shape: Cut corners / shaped field
Active Cells: 30 / 36 (83%)
Moves: 42
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_14[63169e2b] x1; Type 0: Item_05[c5f8e941] x2; Type 1: Item_02[da200a52] x4
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x2, Item_07[0cc9490e] x1, Item_04[a33c1beb] x5, Item_08[c5824e61] x2, Item_02[da200a52] x5
Spawn Weights: Item_01[50e17eb9]:9, Item_04[a33c1beb]:5, Item_08[c5824e61]:2, Item_03[67b4da7e]:2, Item_05[c5f8e941]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: Длинные цели и больше ходов; требуется проверить boardMask.
Notes / Validation: Mask rows 5 != boardRows 6
Board Mask:
111111
111111
111111
111111
111111
```

### Level 013

```text
File: Level_013(5).asset
Difficulty: Hard
Focus: Multi Collect
Board Size: 5x6
Board Shape: Full rectangle
Active Cells: 30 / 30 (100%)
Moves: 39
Main Mechanics: ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_10[0636a8ca] x1; Type 0: Item_13[4e93026e] x1; Type 0: Item_03[67b4da7e] x2; Type 1: Item_02[da200a52] x6
Initial Items: Item_08[c5824e61] x5, Item_09[6fdcf7b0] x1, Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_01[50e17eb9] x4, Item_04[a33c1beb] x1, Item_02[da200a52] x5
Spawn Weights: Item_08[c5824e61]:7, Item_11[8cfa7b3c]:7, Item_01[50e17eb9]:4, Item_04[a33c1beb]:1, Item_09[6fdcf7b0]:1, Item_12[9fdd0c21]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: Открытое 5x6 поле с несколькими целями и 6 единицами ObjectiveType 1.
Notes / Validation: OK
Board Mask:
11111
11111
11111
11111
11111
11111
```

### Level 014

```text
File: Level_014(5).asset
Difficulty: Hard
Focus: High Tier / Multi Collect
Board Size: 6x6
Board Shape: Cut corners / shaped field
Active Cells: 30 / 36 (83%)
Moves: 45
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_15[aa8b132d] x1; Type 0: Item_13[4e93026e] x1; Type 1: Item_02[da200a52] x6
Initial Items: Item_08[c5824e61] x6, Item_09[6fdcf7b0] x2, Item_10[0636a8ca] x1, Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_01[50e17eb9] x2, Item_02[da200a52] x5
Spawn Weights: Item_08[c5824e61]:9, Item_11[8cfa7b3c]:6, Item_01[50e17eb9]:2, Item_04[a33c1beb]:1, Item_09[6fdcf7b0]:2, Item_12[9fdd0c21]:1, Item_06[4a536f10]:1, Item_02[da200a52]:1
Player Lesson: Большой набор стартовых предметов и высокие цели; требуется проверить boardMask.
Notes / Validation: Mask rows 5 != boardRows 6
Board Mask:
111111
111111
111111
111111
111111
```

### Level 015

```text
File: Level_015(5).asset
Difficulty: Hard
Focus: Board Shape / Chapter Check
Board Size: 6x6
Board Shape: Cut corners / shaped field
Active Cells: 32 / 36 (89%)
Moves: 47
Main Mechanics: Board Shape, ObjectiveType 1, Multi-resource spawn
Goals: Type 0: Item_14[63169e2b] x1; Type 0: Item_13[4e93026e] x1; Type 0: Item_09[6fdcf7b0] x2; Type 1: Item_02[da200a52] x6
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x2, Item_07[0cc9490e] x1, Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_08[c5824e61] x4, Item_02[da200a52] x6
Spawn Weights: Item_01[50e17eb9]:8, Item_11[8cfa7b3c]:6, Item_08[c5824e61]:4, Item_04[a33c1beb]:1, Item_03[67b4da7e]:2, Item_12[9fdd0c21]:1, Item_09[6fdcf7b0]:1, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Форма с диагональными вырезами, много целей; хороший checkpoint раннего блока.
Notes / Validation: OK
Board Mask:
111110
111110
111111
111111
011111
011111
```

## Progression notes

- Уровни 1-3 хорошо работают как базовое обучение core merge и collect goals.
- Уровни 4-7 постепенно вводят формы поля и пустые клетки без добавления паука/жил/паутины.
- Уровни 8-15 усиливают количество целей, ресурсов и ходов; это уже ближе к сложным ранним уровням.
- Перед генерацией 16-20 стоит решить, являются ли `objectiveType 1` blocker/removal goals или другим типом цели.
- Нужно проверить маски уровней 8, 10, 12 и 14: в файлах 5 строк маски при `boardRows = 6`. Если это не задумано, лучше добавить шестую строку.

### Level 016

```text
File: Level_016(4).asset
Difficulty: Hard
Focus: Board Shape / Multi Collect
Board Size: 6x6
Board Shape: Board shape / cutouts
Active Cells: 32 / 36 (89%)
Moves: 42
Main Mechanics: Board Shape, ObjectiveType 1
Goals: Type 0: Item_14[63169e2b] x1; Type 0: Item_10[0636a8ca] x1; Type 1: Item_02[da200a52] x5
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x1, Item_08[c5824e61] x5, Item_09[6fdcf7b0] x1, Item_11[8cfa7b3c] x3, Item_04[a33c1beb] x2, Item_02[da200a52] x4
Spawn Weights: Item_01[50e17eb9]:7, Item_08[c5824e61]:7, Item_11[8cfa7b3c]:3, Item_04[a33c1beb]:2, Item_03[67b4da7e]:1, Item_09[6fdcf7b0]:1, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Форма с боковыми вырезами и 3 цели; закрепляет игру на 6x6 с несколькими ресурсами.
Notes / Validation: OK
Board Mask:
111111
100111
111111
111111
111001
111111
```

### Level 017

```text
File: Level_017(4).asset
Difficulty: Hard
Focus: Stones / Blockers
Board Size: 6x6
Board Shape: Stone pattern / blockers in mask
Active Cells: 34 / 36 (94%)
Moves: 44
Main Mechanics: Board Shape, Stones, ObjectiveType 1
Goals: Type 0: Item_13[4e93026e] x1; Type 0: Item_16[300364dd] x1; Type 1: Item_02[da200a52] x4
Initial Items: Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_04[a33c1beb] x5, Item_05[c5f8e941] x1, Item_01[50e17eb9] x3, Item_08[c5824e61] x2
Spawn Weights: Item_11[8cfa7b3c]:7, Item_04[a33c1beb]:7, Item_01[50e17eb9]:3, Item_08[c5824e61]:2, Item_12[9fdd0c21]:1, Item_05[c5f8e941]:1, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Первый сильный stone-блок: 4 камня в центре, игрок учится планировать merge рядом с blockers.
Notes / Validation: OK
Board Mask:
011111
111111
11SS11
11SS11
111111
111110
```

### Level 018

```text
File: Level_018(4).asset
Difficulty: Hard
Focus: Board Shape / Multi Collect
Board Size: 6x6
Board Shape: Board shape / cutouts
Active Cells: 32 / 36 (89%)
Moves: 49
Main Mechanics: Board Shape, ObjectiveType 1
Goals: Type 0: Item_15[aa8b132d] x1; Type 0: Item_07[0cc9490e] x1; Type 1: Item_02[da200a52] x2
Initial Items: Item_08[c5824e61] x6, Item_09[6fdcf7b0] x2, Item_10[0636a8ca] x1, Item_01[50e17eb9] x5, Item_03[67b4da7e] x1, Item_11[8cfa7b3c] x2, Item_02[da200a52] x2
Spawn Weights: Item_08[c5824e61]:9, Item_01[50e17eb9]:6, Item_11[8cfa7b3c]:2, Item_04[a33c1beb]:1, Item_03[67b4da7e]:1, Item_09[6fdcf7b0]:2, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Форма с двумя боковыми сужениями и повышенной длиной целей; тренирует управление пространством.
Notes / Validation: OK
Board Mask:
111111
110011
111111
111111
110011
111111
```

### Level 019

```text
File: Level_019(4).asset
Difficulty: Hard
Focus: Stones / Blockers
Board Size: 6x6
Board Shape: Stone pattern / blockers in mask
Active Cells: 36 / 36 (100%)
Moves: 51
Main Mechanics: Stones, ObjectiveType 1
Goals: Type 0: Item_14[63169e2b] x1; Type 0: Item_13[4e93026e] x1; Type 1: Item_02[da200a52] x6
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x2, Item_07[0cc9490e] x1, Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_08[c5824e61] x3
Spawn Weights: Item_01[50e17eb9]:8, Item_11[8cfa7b3c]:6, Item_04[a33c1beb]:3, Item_08[c5824e61]:1, Item_03[67b4da7e]:2, Item_12[9fdd0c21]:1, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Диагональная линия из 6 камней: уровень про расчистку и доступ к полю через merge рядом.
Notes / Validation: OK
Board Mask:
S11111
1S1111
11S111
111S11
1111S1
11111S
```

### Level 020

```text
File: Level_020(4).asset
Difficulty: Hard
Focus: Board Shape / Multi Collect
Board Size: 6x6
Board Shape: Board shape / cutouts
Active Cells: 32 / 36 (89%)
Moves: 56
Main Mechanics: Board Shape, ObjectiveType 1
Goals: Type 0: Item_15[aa8b132d] x1; Type 0: Item_13[4e93026e] x1; Type 0: Item_07[0cc9490e] x1; Type 1: Item_02[da200a52] x5
Initial Items: Item_08[c5824e61] x6, Item_09[6fdcf7b0] x2, Item_10[0636a8ca] x1, Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_01[50e17eb9] x4, Item_03[67b4da7e] x1, Item_02[da200a52] x5
Spawn Weights: Item_08[c5824e61]:8, Item_11[8cfa7b3c]:6, Item_01[50e17eb9]:5, Item_04[a33c1beb]:1, Item_09[6fdcf7b0]:2, Item_12[9fdd0c21]:1, Item_03[67b4da7e]:1, Item_06[4a536f10]:3, Item_02[da200a52]:1
Player Lesson: Итоговый ранний multi-goal уровень: большое поле, много стартовых предметов и длинные цели.
Notes / Validation: OK
Board Mask:
111111
101101
111111
111111
101101
111111
```

### Level 021

```text
File: Level_021.asset
Difficulty: Hard-
Focus: New Special / Spider Block Intro
Board Size: 6x6
Board Shape: Full rectangle
Active Cells: 36 / 36 (100%)
Moves: 42
Main Mechanics: ObjectiveType 1, New special Item_17
Goals: Type 0: Item_14[63169e2b] x1; Type 0: Item_10[0636a8ca] x1; Type 1: Item_02[da200a52] x5
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x1, Item_08[c5824e61] x5, Item_09[6fdcf7b0] x1, Item_11[8cfa7b3c] x3, Item_04[a33c1beb] x2, Item_02[da200a52] x4, Item_17[1f1ce725] x1
Spawn Weights: Item_01[50e17eb9]:7, Item_08[c5824e61]:7, Item_11[8cfa7b3c]:3, Item_04[a33c1beb]:2, Item_03[67b4da7e]:1, Item_09[6fdcf7b0]:1, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Начало нового блока: появляется новый специальный Item_17 в стартовой раскладке/цели; поле пока открытое.
Notes / Validation: OK
Board Mask:
111111
111111
111111
111111
111111
111111
```

### Level 022

```text
File: Level_022.asset
Difficulty: Hard
Focus: New Special / Spider Block
Board Size: 6x6
Board Shape: Stone pattern / blockers in mask
Active Cells: 36 / 36 (100%)
Moves: 44
Main Mechanics: Stones, ObjectiveType 1, New special Item_17
Goals: Type 0: Item_13[4e93026e] x1; Type 0: Item_16[300364dd] x1; Type 1: Item_02[da200a52] x4; Type 1: Item_17[1f1ce725] x1
Initial Items: Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_04[a33c1beb] x5, Item_05[c5f8e941] x1, Item_01[50e17eb9] x3, Item_08[c5824e61] x2, Item_17[1f1ce725] x1
Spawn Weights: Item_11[8cfa7b3c]:7, Item_04[a33c1beb]:7, Item_01[50e17eb9]:3, Item_08[c5824e61]:2, Item_12[9fdd0c21]:1, Item_05[c5f8e941]:1, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Новый специальный Item_17 сочетается с центральными камнями; аккуратный переход к spider/special-блоку.
Notes / Validation: OK
Board Mask:
111111
111111
11SS11
11SS11
111111
111111
```

### Level 023

```text
File: Level_023.asset
Difficulty: Hard
Focus: New Special / Spider Block
Board Size: 6x6
Board Shape: Board shape / cutouts
Active Cells: 32 / 36 (89%)
Moves: 49
Main Mechanics: Board Shape, ObjectiveType 1, New special Item_17
Goals: Type 0: Item_15[aa8b132d] x1; Type 0: Item_07[0cc9490e] x1; Type 1: Item_17[1f1ce725] x1; Type 1: Item_02[da200a52] x2
Initial Items: Item_08[c5824e61] x6, Item_09[6fdcf7b0] x2, Item_10[0636a8ca] x1, Item_01[50e17eb9] x5, Item_03[67b4da7e] x1, Item_11[8cfa7b3c] x2, Item_02[da200a52] x2, Item_17[1f1ce725] x1
Spawn Weights: Item_08[c5824e61]:9, Item_01[50e17eb9]:6, Item_11[8cfa7b3c]:2, Item_04[a33c1beb]:1, Item_03[67b4da7e]:1, Item_09[6fdcf7b0]:2, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Новый специальный Item_17 + форма с боковыми вырезами; игрок планирует вокруг ограничений пространства.
Notes / Validation: OK
Board Mask:
111111
110011
111111
111111
110011
111111
```

### Level 024

```text
File: Level_024.asset
Difficulty: Hard
Focus: New Special / Spider Block
Board Size: 6x6
Board Shape: Full rectangle / empty mask fallback
Active Cells: 36 / 36 (100%)
Moves: 51
Main Mechanics: ObjectiveType 1, New special Item_17
Goals: Type 0: Item_14[63169e2b] x1; Type 0: Item_13[4e93026e] x1; Type 1: Item_17[1f1ce725] x1; Type 1: Item_02[da200a52] x6
Initial Items: Item_01[50e17eb9] x5, Item_03[67b4da7e] x2, Item_07[0cc9490e] x1, Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_08[c5824e61] x3, Item_02[da200a52] x6, Item_17[1f1ce725] x2
Spawn Weights: Item_01[50e17eb9]:8, Item_11[8cfa7b3c]:6, Item_04[a33c1beb]:3, Item_08[c5824e61]:1, Item_03[67b4da7e]:2, Item_12[9fdd0c21]:1, Item_06[4a536f10]:2, Item_02[da200a52]:1
Player Lesson: Открытое поле без boardMask: усиленный multi-goal уровень с Item_17 как дополнительной целью.
Notes / Validation: Empty boardMask -> assumed full rectangle fallback
Board Mask:
111111
111111
111111
111111
111111
111111
```

### Level 025

```text
File: Level_025(1).asset
Difficulty: Hard
Focus: New Special / Spider Block
Board Size: 6x6
Board Shape: Full rectangle / empty mask fallback
Active Cells: 36 / 36 (100%)
Moves: 56
Main Mechanics: ObjectiveType 1, New special Item_17
Goals: Type 0: Item_15[aa8b132d] x1; Type 0: Item_13[4e93026e] x1; Type 0: Item_07[0cc9490e] x1; Type 1: Item_02[da200a52] x5; Type 1: Item_17[1f1ce725] x2
Initial Items: Item_08[c5824e61] x6, Item_09[6fdcf7b0] x2, Item_10[0636a8ca] x1, Item_11[8cfa7b3c] x5, Item_12[9fdd0c21] x1, Item_01[50e17eb9] x4, Item_03[67b4da7e] x1, Item_02[da200a52] x5, Item_17[1f1ce725] x2
Spawn Weights: Item_08[c5824e61]:8, Item_11[8cfa7b3c]:6, Item_01[50e17eb9]:5, Item_04[a33c1beb]:1, Item_09[6fdcf7b0]:2, Item_12[9fdd0c21]:1, Item_03[67b4da7e]:1, Item_06[4a536f10]:3, Item_02[da200a52]:1
Player Lesson: Итоговый уровень блока 21–25: много целей, Item_17 x2 и высокий запас ходов.
Notes / Validation: Empty boardMask -> assumed full rectangle fallback
Board Mask:
111111
111111
111111
111111
111111
111111
```

## Progression notes 26-30

- Уровни 26-30 закрывают spider-блок после первого знакомства с пауком на уровнях 21-25.
- С 26 уровня используется 2 паука как нормальная сложность блока.
- Уровень 29 — отдельный `Spider Swarm`: 7 пауков, цель только на удаление/уничтожение пауков и Dark Stone L2.
- `Dark Stone L2` в этих уровнях не собирается как ресурс. В целях он трактуется как `Destroy / Clear Dark Stone L2`.
- 31+ можно использовать как начало нового блока Web / Mud, чтобы не смешивать новую механику с финалом spider-блока.

### Level 026

```text
File: Level_026.asset
Difficulty: Hard
Focus: Two Spiders Intro
Board Size: 6x6
Board Shape: Full rectangle
Active Cells: 36 / 36 (100%)
Moves: 50
Main Mechanics: ObjectiveType 1, Spider x2, Multi-resource spawn
Board Mask Objects: Spider x2
Goals: Type 0: Collect Ruby L4 (Item_13[4e93026e]) x1; Type 0: Collect Gold L4 (Item_10[0636a8ca]) x1; Type 1: Destroy Dark Stone L2 (Item_02[da200a52]) x5; Type 1: Clear / Remove Spider (Item_17[1f1ce725]) x2
Initial Items: Crystal L2 (Item_01[50e17eb9]) x3, Gold L2 (Item_08[c5824e61]) x5, Gold L3 (Item_09[6fdcf7b0]) x1, Ruby L2 (Item_11[8cfa7b3c]) x5, Ruby L3 (Item_12[9fdd0c21]) x1, Dark Stone L2 (Item_02[da200a52]) x4, Emerald L2 (Item_04[a33c1beb]) x2
Spawn Weights: Crystal L2 (Item_01[50e17eb9]):3, Gold L2 (Item_08[c5824e61]):8, Ruby L2 (Item_11[8cfa7b3c]):7, Emerald L2 (Item_04[a33c1beb]):2, Ruby L3 (Item_12[9fdd0c21]):1, Gold L3 (Item_09[6fdcf7b0]):1, Dynamite L1 (Item_06[4a536f10]):1, Dark Stone L2 (Item_02[da200a52]):1
Spawnable Items: Unknown Spawnable (Unknown_01[76195626]), Crystal L2 (Item_01[50e17eb9])
Player Lesson: Игрок впервые явно работает с двумя пауками на открытом поле: давление выше, но пространство честное.
Notes / Validation: OK; 2 spiders from boardMask; Dark Stone L2 is destroy objective, not collect.
Board Mask:
111111
111111
11P111
111111
111P11
111111
```

### Level 027

```text
File: Level_027.asset
Difficulty: Hard
Focus: Two Spiders + Stones Soft
Board Size: 6x6
Board Shape: Full rectangle + stones
Active Cells: 36 / 36 (100%)
Moves: 54
Main Mechanics: Stones, ObjectiveType 1, Spider x2, Multi-resource spawn
Board Mask Objects: Spider x2, Stone S x3
Goals: Type 0: Collect Crystal L5 (Item_14[63169e2b]) x1; Type 0: Collect Emerald L4 (Item_16[300364dd]) x1; Type 1: Destroy Dark Stone L2 (Item_02[da200a52]) x5; Type 1: Clear / Remove Spider (Item_17[1f1ce725]) x2
Initial Items: Crystal L2 (Item_01[50e17eb9]) x5, Crystal L3 (Item_03[67b4da7e]) x2, Crystal L4 (Item_07[0cc9490e]) x1, Emerald L2 (Item_04[a33c1beb]) x5, Emerald L3 (Item_05[c5f8e941]) x1, Gold L2 (Item_08[c5824e61]) x3, Ruby L2 (Item_11[8cfa7b3c]) x2
Spawn Weights: Crystal L2 (Item_01[50e17eb9]):8, Emerald L2 (Item_04[a33c1beb]):7, Gold L2 (Item_08[c5824e61]):3, Ruby L2 (Item_11[8cfa7b3c]):2, Crystal L3 (Item_03[67b4da7e]):2, Emerald L3 (Item_05[c5f8e941]):1, Dynamite L1 (Item_06[4a536f10]):1, Dark Stone L2 (Item_02[da200a52]):1
Spawnable Items: Unknown Spawnable (Unknown_01[76195626]), Crystal L2 (Item_01[50e17eb9])
Player Lesson: Два паука и несколько камней вместе ограничивают пространство, но форма поля остаётся простой.
Notes / Validation: OK; 2 spiders + 3 stones from boardMask; Dark Stone L2 is destroy objective.
Board Mask:
111111
111S11
11S111
1P1111
111S11
1111P1
```

### Level 028

```text
File: Level_028.asset
Difficulty: Hard+
Focus: Two Spiders + Limited Space
Board Size: 6x6
Board Shape: Mine passage / limited space
Active Cells: 28 / 36 (78%)
Moves: 52
Main Mechanics: Board Shape, ObjectiveType 1, Spider x2, Multi-resource spawn
Board Mask Objects: Spider x2
Goals: Type 0: Collect Ruby L4 (Item_13[4e93026e]) x1; Type 0: Collect Gold L4 (Item_10[0636a8ca]) x1; Type 1: Clear / Remove Spider (Item_17[1f1ce725]) x2; Type 1: Destroy Dark Stone L2 (Item_02[da200a52]) x4
Initial Items: Ruby L2 (Item_11[8cfa7b3c]) x5, Ruby L3 (Item_12[9fdd0c21]) x1, Gold L2 (Item_08[c5824e61]) x5, Gold L3 (Item_09[6fdcf7b0]) x1, Crystal L2 (Item_01[50e17eb9]) x2, Emerald L2 (Item_04[a33c1beb]) x2, Dark Stone L2 (Item_02[da200a52]) x3
Spawn Weights: Ruby L2 (Item_11[8cfa7b3c]):8, Gold L2 (Item_08[c5824e61]):7, Crystal L2 (Item_01[50e17eb9]):2, Emerald L2 (Item_04[a33c1beb]):1, Ruby L3 (Item_12[9fdd0c21]):1, Gold L3 (Item_09[6fdcf7b0]):1, Dynamite L1 (Item_06[4a536f10]):1, Dark Stone L2 (Item_02[da200a52]):1
Spawnable Items: Unknown Spawnable (Unknown_01[76195626]), Crystal L2 (Item_01[50e17eb9])
Player Lesson: Два паука становятся опаснее из-за меньшего количества активных клеток и узких зон.
Notes / Validation: OK; 2 spiders; active cells 28/36, not 24/36.
Board Mask:
011110
011110
11P111
111111
011P10
011110
```

### Level 029

```text
File: Level_029.asset
Difficulty: Very Hard-
Focus: Spider Swarm
Board Size: 6x6
Board Shape: Full rectangle / swarm layout
Active Cells: 36 / 36 (100%)
Moves: 33
Main Mechanics: Spider Swarm, ObjectiveType 1, Multi-resource spawn
Board Mask Objects: Spider x7
Goals: Type 1: Clear / Remove Spider (Item_17[1f1ce725]) x7; Type 1: Destroy Dark Stone L2 (Item_02[da200a52]) x3
Initial Items: Crystal L2 (Item_01[50e17eb9]) x4, Gold L2 (Item_08[c5824e61]) x4, Ruby L2 (Item_11[8cfa7b3c]) x7, Dark Stone L2 (Item_02[da200a52]) x2
Spawn Weights: Crystal L2 (Item_01[50e17eb9]):4, Gold L2 (Item_08[c5824e61]):4, Ruby L2 (Item_11[8cfa7b3c]):4, Emerald L2 (Item_04[a33c1beb]):3, Dynamite L1 (Item_06[4a536f10]):1, Dark Stone L2 (Item_02[da200a52]):1
Spawnable Items: Unknown Spawnable (Unknown_01[76195626]), Crystal L2 (Item_01[50e17eb9])
Player Lesson: Специальный уровень: главная цель — очистить поле от большого количества пауков.
Notes / Validation: OK; Spider Swarm with 7 spiders; no collect goals, only remove/destroy objectives.
Board Mask:
111111
1P11P1
1111P1
11PP11
1P11P1
111111
```

### Level 030

```text
File: Level_030.asset
Difficulty: Very Hard-
Focus: Spider Chapter Finale
Board Size: 6x6
Board Shape: Cut corners + stones
Active Cells: 32 / 36 (89%)
Moves: 55
Main Mechanics: Board Shape, Stones, ObjectiveType 1, Spider x2, Multi-resource spawn
Board Mask Objects: Spider x2, Stone S x2
Goals: Type 0: Collect Gold L5 (Item_15[aa8b132d]) x1; Type 0: Collect Crystal L5 (Item_14[63169e2b]) x1; Type 1: Destroy Dark Stone L2 (Item_02[da200a52]) x4; Type 1: Clear / Remove Spider (Item_17[1f1ce725]) x2
Initial Items: Gold L2 (Item_08[c5824e61]) x6, Gold L3 (Item_09[6fdcf7b0]) x2, Gold L4 (Item_10[0636a8ca]) x1, Crystal L2 (Item_01[50e17eb9]) x5, Crystal L3 (Item_03[67b4da7e]) x2, Crystal L4 (Item_07[0cc9490e]) x1, Ruby L2 (Item_11[8cfa7b3c]) x1, Emerald L2 (Item_04[a33c1beb]) x1
Spawn Weights: Gold L2 (Item_08[c5824e61]):8, Crystal L2 (Item_01[50e17eb9]):8, Ruby L2 (Item_11[8cfa7b3c]):3, Emerald L2 (Item_04[a33c1beb]):2, Gold L3 (Item_09[6fdcf7b0]):2, Crystal L3 (Item_03[67b4da7e]):2, Dynamite L1 (Item_06[4a536f10]):2, Dark Stone L2 (Item_02[da200a52]):1
Spawnable Items: Unknown Spawnable (Unknown_01[76195626]), Crystal L2 (Item_01[50e17eb9])
Player Lesson: Финальная проверка spider-блока: два паука, камни, форма поля и high-tier цели одновременно.
Notes / Validation: OK; finale with 2 spiders + 2 stones; Dark Stone L2 is destroy objective.
Board Mask:
011110
111111
11S1P1
111111
1P1S11
011110
```

