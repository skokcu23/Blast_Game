# Requirements Traceability Document
## Dream Games Blast Game — Case Study Implementation

---

## 1. Grid & Board

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 1.1 | Rectangular grid, 6–10 cells width and height | Done | `LevelData.IsValid()` enforces 6–10 range. `Board(width, height)` constructor. | `LevelDataTests` |
| 1.2 | 4 types of colored cubes | Done | `ItemIds`: Red (r), Green (g), Blue (b), Yellow (y). `ItemFactory.CreateItem()` | `ItemIdsTests`, `ItemFactoryTests` |
| 1.3 | Grid initialized from JSON level files | Done | `LevelParser.Parse()` → `Board.Initialize(LevelData)`. Grid stored bottom-left to top-right: `index = y * width + x` | `LevelDataTests`, `BoardTests` |
| 1.4 | 10 levels loaded from JSON | Done | Files at `Resources/Levels/level_01.json` through `level_10.json`. `LevelParser.LoadLevel(int)` | Manual: all 10 levels load and play |

---

## 2. Cube Matching & Blasting

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 2.1 | At least 2 adjacent same-color cubes can be blasted by tapping | Done | `ClassicMatchStrategy.FindMatches()` flood fill, minimum group size = 2 | `ClassicMatchStrategyTests` |
| 2.2 | Adjacent = horizontal or vertical neighbors only | Done | 4-directional neighbor check in flood fill (no diagonals) | `ClassicMatchStrategyTests` |
| 2.3 | Each valid tap costs one move | Done | `GameSession.ProcessTap()` decrements `_moveCount` only on valid TapAction | `GameSessionTests.ProcessTap_InvalidTap_DoesNotDecrementMoves` |
| 2.4 | Tapping single cube or empty cell does nothing | Done | `TapResolver.Resolve()` returns `TapAction.None` for groups < 2, empty cells, obstacles | `TapResolverTests` |

---

## 3. Gravity & Refill

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 3.1 | Falling objects fall down to empty cells only vertically | Done | `GravityProcessor.ApplyGravity()` scans columns bottom-to-top, counts empty cells, shifts items down | `GravityProcessorTests` |
| 3.2 | New random cubes spawn from the top | Done | `GravityProcessor.FillEmptySpaces()` creates random cubes with `StartPos.y = board.Height` (above board) | `GravityProcessorTests` |
| 3.3 | Fall mechanics are code-driven, not physics-based | Done | Pure C# `GravityProcessor` produces `List<ItemMovement>`. No Rigidbody or Physics. View animates with DOTween. | — |
| 3.4 | Cubes cannot fall through other cubes, rockets, or obstacles | Done | `GravityProcessor`: non-movable items (Box, Stone) reset `emptyCount`. Movable items (cubes, rockets, vases) fall as a unit. | `GravityProcessorTests.Gravity_BoxBlocksFalling` |

---

## 4. Rockets

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 4.1 | Groups of 4+ same-color cubes form a Rocket at the tapped cell | Done | `RocketProcessor.QualifiesForRocket(count >= 4)`. `CreateRocket()` places rocket at tapped coordinate. | `RocketProcessorTests`, `GameSessionTests.ProcessTap_GroupOf4_CreatesRocket` |
| 4.2 | Rocket direction is randomly horizontal or vertical | Done | `Random.Next(0, 2)` in `RocketProcessor.CreateRocket()` | `RocketProcessorTests` |
| 4.3 | Rockets can fall to empty cells below them | Done | `ItemFactory.CreateItem("vro"/"hro")` sets `IsMovable = true`. `GravityProcessor` moves them. | `GravityProcessorTests` |
| 4.4 | Tapping a rocket triggers explosion (splits into 2 moving parts) | Done | `TapResolver` returns `TapAction.ExplodeRocket`. `RocketProcessor.ExplodeRocket()` traces PathA/PathB. | `RocketExplosionTests`, `GameSessionTests.ProcessTap_TapRocket_ExplodesIt` |
| 4.5 | Rocket parts damage cells as they travel across the grid | Done | `TracePath()` iterates cells, calls `ApplyObstacleDamage()` per cell. Cubes destroyed, obstacles take damage. | `RocketExplosionTests` |
| 4.6 | Being hit by another rocket triggers chain explosion | Done | `TracePath()` adds rockets to `TriggeredRockets`. `GameSession.ProcessChainReactions()` uses queue-based processing with `MaxChainDepth = 200`. | `GameSessionTests.ProcessTap_RocketChain_BothExplode`, `_ABC` |
| 4.7 | Groups of 4+ display a rocket indicator (hint) | Done | `HintCalculator.FindRocketHints()` flood fills for groups >= 4, returns `Dict<Coordinate, colorId>`. View swaps sprite to RocketState version. | `HintCalculatorTests`, `GameSessionTests.GetHints_ReturnsCorrectData` |
| 4.8 | Hint system: stateless, idempotent, survives gravity | Done | `GridStateManager.UpdateHints()` walks all cells every update. `CubeView.ApplyHint()/RemoveHint()` are no-ops if already in target state. No coordinate tracking. | Manual: hints correct after gravity |

---

## 5. Rocket-Rocket Combo

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 5.1 | Adjacent rockets combine into a combo | Done | `TapResolver` detects adjacent rockets → `TapAction.RocketCombo`. `RocketProcessor.ProcessCombo()` fires both directions. | `TapResolverTests.Resolve_TwoAdjacentRockets_ReturnsCombo` |
| 5.2 | Combo creates 3×3 explosion pattern in both directions | Done | `ProcessCombo()` traces 3 parallel paths per direction (dy/dx = -1, 0, +1). 3×3 center is the natural intersection. | `ComboDamageTests` (24 tests) |
| 5.3 | Combo: vases in 3×3 take 2 damage (destroyed) | Done | Each direction has its own `processed` set. H damages vase (2→1), V destroys vase (1→0). Geometry handles it — no special-case code. | `ComboDamageTests.ProcessCombo_VaseIn3x3_Destroyed` |
| 5.4 | Combo: vases outside 3×3 take 1 damage (survive) | Done | Only one direction reaches cells outside 3×3. Single `TryDamage` call. | `ComboDamageTests.ProcessCombo_VaseOnHorizontalPathOnly_TakesOneDamage` |
| 5.5 | Combo: chain reactions from triggered rockets | Done | Shared `triggeredSet` across both directions prevents duplicate queueing. `GameSession` processes triggered rockets via queue after combo. | `ComboDamageTests.ProcessCombo_TriggeredRocket_NotDuplicated` |

---

## 6. Obstacles

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 6.1 | Box: damaged by blast + rocket, 1 HP, does not fall | Done | `ItemFactory`: `IsObstacle=true, IsMovable=false, Health=1`. `DamageResolver.CanDamage()`: Blast ✓, Rocket ✓, Combo ✓ | `DamageResolverTests`, `GameSessionTests.ProcessTap_BlastAdjacentToBox_DestroysBox` |
| 6.2 | Stone: damaged by rocket only, 1 HP, does not fall | Done | `DamageResolver.CanDamage()`: Blast ✗, Rocket ✓, Combo ✓. `IsMovable=false`. | `DamageResolverTests`, `GameSessionTests.ProcessTap_BlastAdjacentToStone_StoneImmune` |
| 6.3 | Vase: damaged by blast + rocket, 2 HP, max 1 damage per blast group, falls like cubes | Done | `Health=2, IsMovable=true`. `DamageResolver.ProcessAdjacentDamage()` uses `HashSet<Coordinate> alreadyDamaged` to enforce 1-per-blast cap. | `DamageResolverTests`, `GameSessionTests.ProcessTap_BlastAdjacentToVase_OneDamage` |
| 6.4 | Vase visual: shows cracked state at 1 HP | Done | `AnimationController.PlayDamagedSprites()`: squash animation + sprite swap mid-squash via `GridStateManager.UpdateSprite()`. `BoardView.GetVaseSprite(health)` returns damaged sprite. | Manual: vase cracks visually |

---

## 7. Win / Lose Conditions

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 7.1 | Win: all obstacles cleared within move limit | Done | `GameSession.CheckGameState()`: `Board.AreAllObstaclesCleared()` → `GameSessionState.Won` | `GameSessionTests.ProcessTap_ClearAllObstacles_StateWon` |
| 7.2 | Lose: no moves remaining with obstacles still on board | Done | `_moveCount <= 0 && !AreAllObstaclesCleared()` → `GameSessionState.Lost` | `GameSessionTests.ProcessTap_LastMoveObstaclesRemain_StateLost` |
| 7.3 | After win/lose, no more taps accepted | Done | `ProcessTap()` checks `_state != Playing` before processing | `GameSessionTests.ProcessTap_AfterGameWon_ReturnsInvalid` |

---

## 8. UI & Scene Flow

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 8.1 | MainScene with LevelButton displaying current level | Done | `MainSceneController` reads from `LevelProgressionManager.GetLevelButtonText()` → "Level X" or "Finished" | `LevelProgressionManagerTests.GetLevelButtonText_*` |
| 8.2 | LevelScene loaded according to current level | Done | `GameOrchestrator.Start()` → `LevelProgressionManager.GetLevelToPlay()` → `LevelParser.LoadLevel(n)` | Manual |
| 8.3 | Win: celebration particles and animation shown, then MainScene loaded | Done | `CelebrationUI.PlayCelebration()`: dim fade → popup scale-in → 3 stars pop sequentially → Continue button fades in → tap loads MainScene | Manual |
| 8.4 | Lose: popup with close button (→ MainScene) and try again button (→ reload level) | Done | `FailPopupUI`: dim fade + popup scale-in (DOTween). Close → `SceneManager.LoadScene("MainScene")`. Try Again → `GameOrchestrator.ReloadCurrentLevel()` | Manual |
| 8.5 | Top UI bar showing Goal icons + Move counter | Done | `GameplayUI` spawns `GoalItemUI` prefabs from `ObstacleGoalTracker`. Updates counts and checkmarks after each turn via `GoalSnapshot` DTO. | Manual |

---

## 9. Level Persistence

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 9.1 | Level number persisted across Unity restarts | Done | `PlayerPrefsLevelPersistence` implements `ILevelPersistence`. Saves/loads via `PlayerPrefs.GetInt("CurrentLevel")`. | `LevelProgressionManagerTests` (uses `InMemoryLevelPersistence`) |
| 9.2 | Editor menu item to set level number | Done | `LevelEditorMenu` under `Tools → Set Level Number`. Calls `PlayerPrefs.SetInt()` directly. | Manual |
| 9.3 | All levels finished → display "Finished" | Done | `LevelProgressionManager.AllLevelsComplete` → `GetLevelButtonText()` returns "Finished". Button disabled or shows text. | `LevelProgressionManagerTests.GetLevelButtonText_ShowsFinished_WhenComplete` |

---

## 10. Technical Requirements

| # | Requirement | Status | Implementation | Test Coverage |
|---|------------|--------|----------------|---------------|
| 10.1 | Portrait 9:16 orientation | Done | Canvas Scaler: Scale With Screen Size, Reference Resolution 1080×1920, Match 0.5. `BoardView.FitCameraToBoard()` adjusts camera. | Manual |
| 10.2 | OOP principles | Done | Three-layer architecture (Data/Logic/View). Strategy pattern (MatchStrategy). Factory pattern (ItemFactory). Observer pattern (events). Interface segregation (ILevelPersistence). Single responsibility throughout. | — |
| 10.3 | No Zenject / DI framework | Done | Manual dependency injection via constructors (GameSession, LevelProgressionManager) and Unity SerializedFields (BoardView, GameOrchestrator). | — |
| 10.4 | Object pooling for cubes | Done | `CubePool`: prewarm, Get/Return pattern, auto-grow. Only `GridStateManager` calls Get/Return. | — |


---

## 12. Test Summary

| Test Suite | Count | Coverage |
|------------|-------|----------|
| BoardTests | 14 | Board construction, initialization, item access, obstacle tracking |
| ClassicMatchStrategyTests | ~15 | Flood fill, blast, adjacent damage, vase cap |
| CoordinateTests | ~8 | Equality, hash code, dictionary key behavior |
| DamageResolverTests | ~12 | Per-obstacle immunity rules, damage application, destruction |
| GameSessionTests | ~30 | Full tap→result flow, win/lose, moves, hints, step ordering |
| GravityProcessorTests | 13 | Gravity, refill, obstacle blocking, vase falling |
| GridItemTests | ~8 | Properties, damage, health |
| HintCalculatorTests | ~6 | Rocket hints for groups ≥ 4 |
| ItemFactoryTests | ~10 | All item types, properties, random generation |
| ItemIdsTests | ~6 | Type checks, constants |
| LevelDataTests | ~6 | Validation, coordinate access |
| LevelProgressionManagerTests | 22 | Advancement, persistence, clamping, button text |
| ObstacleGoalTrackerTests | ~8 | Goal counting, decrement, type tracking |
| RocketExplosionTests | ~15 | Single rocket paths, destruction, triggered rockets |
| RocketProcessorTests | ~10 | Creation, qualification, direction |
| ComboDamageTests | 24 | Dual-direction damage, vase 2-hit kill, parallel paths, ComboAreaCleared |
| TapResolverTests | ~15 | All tap types, priority, edge cases |
| **Total** | **~220+** | |
