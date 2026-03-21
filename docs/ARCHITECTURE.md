# Architecture Document

## Table of Contents

1. Layer Overview
2. Data Flow
3. Data Layer — Class Reference
4. Logic Layer — Class Reference
5. View Layer — Class Reference
6. Design Patterns
7. Key Architectural Decisions

---

## 1. Layer Overview

The game uses a strict three-layer architecture. Dependencies flow downward only — the View depends on Logic and Data, Logic depends on Data, Data depends on nothing.

```
┌─────────────────────────────────────────────────────────────┐
│                       DATA LAYER                            │
│                                                             │
│  Pure value types: structs, enums, DTOs                     │
│  No behavior. No dependencies. Shared by all layers.        │
│                                                             │
│  13 files: Coordinate, ItemIds, BlastResult, TurnResult,    │
│  RocketExplosionData, DamageResult, DamageSource,           │
│  GoalSnapshot, GameSessionState, TapAction, ItemMovement,   │
│  LevelData, RocketCreationData                              │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                       LOGIC LAYER                           │
│                                                             │
│  Pure C# classes. No MonoBehaviour, no UnityEngine          │
│  (except LevelParser which uses Resources.Load).            │
│  Fully testable with NUnit — 250+ tests, zero Unity deps.  │
│                                                             │
│  Entry point: GameSession.ProcessTap(coord) → TurnResult    │
│                                                             │
│  16 files: GameSession, Board, GridItem, ItemFactory,       │
│  ClassicMatchStrategy, DamageResolver, GravityProcessor,    │
│  RocketProcessor, TapResolver, HintCalculator,              │
│  ObstacleGoalTracker, LevelProgressionManager,              │
│  LevelParser, MatchStrategy, ILevelPersistence,             │
│  InMemoryLevelPersistence                                   │
└──────────────────────────┬──────────────────────────────────┘
                           │ TurnResult (ordered list of TurnSteps)
┌──────────────────────────▼──────────────────────────────────┐
│                       VIEW LAYER                            │
│                                                             │
│  Unity MonoBehaviours. DOTween animations. UI management.   │
│  Consumes TurnResult sequentially — never calls Logic       │
│  systems directly.                                          │
│                                                             │
│  Entry point: GameOrchestrator receives tap events,         │
│  calls GameSession, animates the result via BoardView.      │
│                                                             │
│  15 files: GameOrchestrator, BoardView, AnimationController,│
│  GridStateManager, CubePool, CubeView, ParticleEffect-     │
│  Controller, DOTweenHelper, InputManager, GameplayUI,       │
│  GoalItemUI, FailPopupUI, CelebrationUI,                   │
│  MainSceneController, PlayerPrefsLevelPersistence           │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Data Flow

### 2.1 One Complete Tap — From Input to Animation

```
Player taps screen
       │
       ▼
InputManager.Update()
  Raycasts → finds CubeView → reads GridCoordinate
  Fires OnCubeTapped(Coordinate)
       │
       ▼
GameOrchestrator.OnCellTapped(coord)
  Guards: not busy, state == Playing
       │
       ▼
GameSession.ProcessTap(coord)
  ├─ TapResolver.Resolve() → determines action type
  │    Priority: Combo > Rocket > Blast > None
  │
  ├─ Based on action:
  │    BlastGroup → ClassicMatchStrategy.Blast()
  │                 RocketProcessor.CreateRocket() (if 4+)
  │    ExplodeRocket → RocketProcessor.ExplodeRocket()
  │                    ProcessChainReactions() (queue loop)
  │    RocketCombo → RocketProcessor.ProcessCombo()
  │                  ProcessChainReactions() (for triggered)
  │
  ├─ AppendSettleSteps()
  │    GravityProcessor.ApplyGravity()
  │    GravityProcessor.FillEmptySpaces()
  │
  ├─ CheckGameState() → Won / Lost / Playing
  │
  └─ Returns TurnResult { Steps[], HintData, State, Moves }
       │
       ▼
GameOrchestrator.AnimateTurnSteps(turnResult)
  Iterates steps sequentially:
  ├─ Blast         → BoardView.AnimateBlast()
  ├─ BlastForRocket→ BoardView.AnimateRocketCreation()
  ├─ RocketCreated → BoardView.SpawnRocketVisual()
  ├─ RocketExplosion→ BoardView.AnimateRocketExplosion()
  ├─ ComboExplosion→ BoardView.AnimateComboExplosion()
  ├─ Gravity       → BoardView.AnimateGravity()
  └─ Refill        → BoardView.AnimateRefill()
       │
       ▼
GameOrchestrator post-animation:
  ├─ ReconcileWithBoard() (safety net)
  ├─ UpdateRocketHints()
  ├─ UpdateUI (moves, goals via GoalSnapshot)
  └─ HandleGameState() → Win/Lose UI
```

### 2.2 BoardView Internal Delegation

```
BoardView (facade)
  │
  ├─ AnimateBlast(BlastResult)
  │    └─ AnimationController.PlayBlast()
  │         ├─ CrackDamagedVases() → CubeView.ApplyDamageVisual()
  │         ├─ EmitDestructionParticles() → ParticleEffectController
  │         ├─ CreatePopTween() → DOTween scale animation
  │         └─ GridStateManager.RemoveCell() → CubePool.Return()
  │
  ├─ AnimateGravity(movements)
  │    └─ AnimationController.PlayGravity()
  │         ├─ GridStateManager.MoveCell() (dict key update)
  │         └─ DOTween.DOMove() (visual slide)
  │
  ├─ AnimateRefill(newItems)
  │    └─ AnimationController.PlayRefill()
  │         ├─ GridStateManager.PlaceCell() → CubePool.Get()
  │         └─ DOTween.DOMove() (drop from above)
  │
  └─ UpdateRocketHints(hints)
       └─ GridStateManager.UpdateHints()
            └─ For each cell: CubeView.ApplyHint() or RemoveHint()
```

### 2.3 Combo Explosion — Dual Direction

```
ProcessCombo(origin, adjacentRockets)
  │
  ├─ Remove all participant rockets from board
  │
  ├─ HORIZONTAL (own processedH set)
  │    ├─ Row y-1: TracePathCombo left + right
  │    ├─ Row y  : TracePathCombo left + right
  │    └─ Row y+1: TracePathCombo left + right
  │
  ├─ VERTICAL (own processedV set)
  │    ├─ Col x-1: TracePathCombo down + up
  │    ├─ Col x  : TracePathCombo down + up
  │    └─ Col x+1: TracePathCombo down + up
  │
  ├─ 3×3 center: hit by BOTH directions
  │    Cube (1HP): H destroys → V sees empty
  │    Vase (2HP): H damages → V destroys
  │
  └─ ComboAreaCleared: board-verified empty cells in 3×3
```

---

## 3. Data Layer — Class Reference

| Class | Type | Purpose |
|-------|------|---------|
| `Coordinate` | struct | Immutable 2D grid position. Value equality for Dictionary keys. |
| `ItemIds` | static class | String constants matching level JSON format (r, g, b, y, bo, s, v, etc.). |
| `GameSessionState` | enum | Playing, Won, Lost. |
| `DamageSource` | enum | Blast, Rocket, Combo. Determines immunity rules. |
| `DamageResult` | struct | Output of a damage attempt: applied? destroyed? remaining HP? |
| `TapAction` | enum | None, BlastGroup, ExplodeRocket, RocketCombo. |
| `ItemMovement` | struct | From/to coordinate + item ID for gravity and refill. |
| `BlastResult` | class | Complete blast output: blasted coords, damaged/destroyed obstacles, rocket flag. |
| `RocketCreationData` | class | Spawn position + rocket ID + source coordinates. |
| `RocketExplosionData` | class | Paths, damage lists, triggered rockets. Single and combo modes. |
| `GoalSnapshot` | class | Read-only snapshot of goal progress for UI layer. |
| `LevelData` | class | JSON-deserialized level file: grid dimensions, move count, grid array. |
| `TurnResult` | class | Complete tap result: ordered TurnSteps, game state, moves, hints. |
| `TurnStep` | class | One animation event within a turn. Type + associated data. |
| `TurnStepType` | enum | Blast, BlastForRocket, RocketCreated, RocketExplosion, ComboExplosion, Gravity, Refill. |
| `DestroyedObstacleInfo` | struct | Position + obstacle type, captured before board clear for goal tracking. |

---

## 4. Logic Layer — Class Reference

### 4.1 Core Game State

| Class | Responsibility |
|-------|----------------|
| `Board` | 2D grid of GridItems. Get/Set/Validate coordinates. Win condition check (AreAllObstaclesCleared). |
| `GridItem` | Single cell contents: ID, obstacle flag, movable flag, health. Created by ItemFactory only. |
| `ItemFactory` | Translates string IDs to configured GridItems. Enforces property rules (Box=fixed, Vase=movable+2HP). |
| `GameSession` | State machine for one level. Single entry point: ProcessTap() → TurnResult. Coordinates all systems. |

### 4.2 Tap Resolution

| Class | Responsibility |
|-------|----------------|
| `TapResolver` | Determines what a tap means. Priority: combo > rocket > blast > none. Returns TapResult with pre-computed data. |
| `TapResult` | Action type + matched coordinates or adjacent rockets. |

### 4.3 Match & Blast

| Class | Responsibility |
|-------|----------------|
| `MatchStrategy` | Interface: FindMatches() + Blast(). Strategy pattern for swappable rules. |
| `ClassicMatchStrategy` | Match-2 flood-fill implementation. Delegates adjacent damage to DamageResolver. |
| `DamageResolver` | Single source of truth for ALL damage rules. Immunity table (Stone vs Blast). Vase cap enforcement. |

### 4.4 Rockets

| Class | Responsibility |
|-------|----------------|
| `RocketProcessor` | Creation (group ≥ 4), single explosion (2 paths), combo explosion (6 parallel paths). Chain reaction invariant: triggered rockets stay on board until explicitly exploded. |

### 4.5 Board Settling

| Class | Responsibility |
|-------|----------------|
| `GravityProcessor` | Drop movable items into empty cells. Non-movable items (Box, Stone) block falling. Refill from top with random cubes. Sealed cells below obstacles don't refill. |
| `HintCalculator` | Flood-fill scan for groups ≥ 4. Returns coordinate → color map for sprite swaps. |

### 4.6 Goal & Progression

| Class | Responsibility |
|-------|----------------|
| `ObstacleGoalTracker` | Counts remaining obstacles per type. Initialized from board, decremented on destruction. |
| `LevelProgressionManager` | Tracks current level, advances on win, detects "all complete". Uses ILevelPersistence. |
| `ILevelPersistence` | Interface: LoadCurrentLevel() / SaveCurrentLevel(). |
| `InMemoryLevelPersistence` | Test implementation. Field storage, no Unity dependency. |

### 4.7 Level Loading

| Class | Responsibility |
|-------|----------------|
| `LevelParser` | Loads JSON from Resources, deserializes via JsonUtility, validates constraints. |

---

## 5. View Layer — Class Reference

### 5.1 Orchestration

| Class | Responsibility |
|-------|----------------|
| `GameOrchestrator` | Mediator. Creates GameSession, forwards taps, iterates TurnSteps, builds GoalSnapshots for UI, handles win/lose transitions. |
| `InputManager` | Converts mouse/touch to grid coordinates via Physics2D.Raycast on CubeView colliders. |

### 5.2 Board Visualization

| Class | Responsibility |
|-------|----------------|
| `BoardView` | Thin facade. Holds sprite references (30+ SerializedFields). Delegates to 4 subsystems. Camera and board frame fitting. |
| `GridStateManager` | Single owner of the visual cell dictionary. PlaceCell/RemoveCell/MoveCell. Stateless hint updates. SyncWithBoard safety net. |
| `CubePool` | Object pool for CubeViews. Prewarm, Get/Return, auto-grow. Zero GC during gameplay. |
| `CubeView` | MonoBehaviour on each cell prefab. Stores ItemId, coordinate, default sprite. Idempotent ApplyHint/RemoveHint. Instant ApplyDamageVisual. Y-based sorting order. |

### 5.3 Animation

| Class | Responsibility |
|-------|----------------|
| `AnimationController` | All DOTween animations: pop, merge, gravity bounce, refill drop, projectile paths with fly-off, combo parallel projectiles. Emits particles. Cracks vases via derived state. |
| `ParticleEffectController` | Pooled sprite particles: cube burst, obstacle burst, rocket burst with smoke+star, smoke trail along projectile paths. |
| `DOTweenHelper` | Extension methods: Tween.ToTask() and Sequence.ToTask(). Bridges DOTween with async/await. |

### 5.4 UI

| Class | Responsibility |
|-------|----------------|
| `GameplayUI` | Top bar: move counter + goal icons. Receives GoalSnapshot DTO (never touches ObstacleGoalTracker). |
| `GoalItemUI` | Single goal icon: obstacle sprite, count text, checkmark overlay when cleared. |
| `FailPopupUI` | DOTween scale-in popup. Close → MainScene, Try Again → reload level. |
| `CelebrationUI` | DOTween popup with 3-star sequential animation + Continue button. |
| `MainSceneController` | Main menu: reads level from persistence, shows "Level X" or "Finished", loads LevelScene. |

### 5.5 Persistence

| Class | Responsibility |
|-------|----------------|
| `PlayerPrefsLevelPersistence` | Implements ILevelPersistence using Unity's PlayerPrefs. |

---

## 6. Design Patterns

### 6.1 Strategy Pattern — MatchStrategy

```
GameSession uses MatchStrategy interface
       │
       ▼
ClassicMatchStrategy (current implementation)
  - Match-2 flood-fill
  - Delegates damage to DamageResolver

Future: Could swap to Match-3, line-match, or shape-match
without changing GameSession, TapResolver, or the View.
```

### 6.2 Factory Pattern — ItemFactory

All GridItem creation goes through ItemFactory. This ensures property rules are enforced in one place: Box is fixed, Vase has 2HP, Stone is immovable. No raw GridItem constructors in game code.

### 6.3 Object Pool — CubePool + ParticleEffectController

Both use the same pattern: pre-warm queue, Get() activates and dequeues, Return() resets and re-enqueues. Auto-grow if empty. ReturnAll() on level load. Result: zero Instantiate/Destroy during gameplay, zero GC spikes.

### 6.4 Facade — BoardView

BoardView has 30+ SerializedFields and delegates to 4 subsystems. The Orchestrator calls one method (e.g., AnimateBlast). Internally, AnimationController plays tweens, GridStateManager updates state, ParticleEffectController emits particles, CubePool manages instances.

### 6.5 Mediator — GameOrchestrator

The Orchestrator is the only class that touches both Logic and View. It builds GoalSnapshot DTOs so the UI never accesses ObstacleGoalTracker. It calls ReconcileWithBoard as a safety net. No other View class imports any Logic class.

### 6.6 Repository — ILevelPersistence

The Logic layer defines ILevelPersistence (Load/Save interface). The View layer provides PlayerPrefsLevelPersistence. Tests use InMemoryLevelPersistence. LevelProgressionManager works with either — no Unity dependency.

---

## 7. Key Architectural Decisions

### 7.1 TurnResult as the Single Contract

The Logic layer produces a TurnResult containing an ordered list of TurnSteps. The View layer consumes them sequentially. No shared mutable state crosses the boundary. This means the entire Logic layer can be tested without Unity, and the View can be rewritten without touching game rules.

### 7.2 Derived State for Damage Visuals

Vase cracking is not a TurnStep. Instead, AnimationController reads DamagedObstacles from each BlastResult or RocketExplosionData and applies the visual immediately before the animation plays. This eliminated timing bugs where vases cracked at inconsistent times relative to gravity.

### 7.3 Stateless Hint System

The hint system has no memory. Every update walks all active cells in the dictionary and tells each CubeView "you're hinted" or "you're not." CubeView's ApplyHint/RemoveHint are idempotent — calling them when already in the target state is a no-op. This makes the system immune to gravity-induced coordinate changes.

### 7.4 Dual-Direction Combo Damage

Each combo direction (H and V) uses its own processed set. The 3×3 center is the natural intersection where both directions visit every cell. Vases (2HP) die because they get hit twice. No special-case 3×3 code exists — the geometry does the work.

### 7.5 Y-Based Sorting Order

Cubes use sortingOrder = coord.y so higher rows render in front. Combined with scale > 1.0 (cubes overlap), this creates the 3D stacked look where each cube's bottom bevel overlaps the cube below. Sorting order updates automatically in CubeView.UpdateCoordinate() during gravity.

### 7.6 9-Slice Board Frame

The board frame uses SpriteRenderer.drawMode = Sliced with Unity's 9-slice system. The golden border corners stay fixed while the center stretches to any board size. Sized via code in FitBoardFrame() after each level load.
