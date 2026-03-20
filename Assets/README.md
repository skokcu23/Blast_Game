# Blast Game

A Match-2 blast puzzle game built in Unity 6 as a software engineering case study for Dream Games.

## Overview

Tap groups of 2+ same-color cubes to blast them. Groups of 4+ create rockets that clear entire rows or columns. Combine two rockets for a devastating 3-wide cross explosion. Clear all obstacles to win each level.

## How to Run

### Requirements
- Unity 6 (6000.0.59f2 or later)
- DOTween (included in Assets/Plugins/)

### Quick Start
1. Open the project in Unity
2. Open `Assets/Scenes/MainScene`
3. Press Play
4. Tap the Level button to start playing

### Build Settings
Scenes must be added in this order:
- `MainScene` — Build Index 0 (loads first)
- `LevelScene` — Build Index 1

## Gameplay

### Controls
- Tap a group of 2+ same-color cubes to blast them
- Tap a rocket to fire it
- Tap a rocket adjacent to another rocket to trigger a combo

### Items
| Item | Description |
|------|-------------|
| Cubes | 4 colors (red, green, blue, yellow). Match 2+ to blast. |
| Rockets | Created from groups of 4+. Horizontal clears a row, vertical clears a column. |
| Box | 1 HP. Damaged by blast and rockets. Fixed in place. |
| Stone | 1 HP. Damaged by rockets only (immune to blast). Fixed in place. |
| Vase | 2 HP. Damaged by blast and rockets. Falls with gravity. Max 1 damage per blast. |

### Win Condition
Clear all obstacles (boxes, stones, vases) within the move limit.

### Rocket Hints
Cube groups of 4+ display a rocket icon, indicating they will create a rocket when tapped.

### Rocket-Rocket Combo
Tapping a rocket adjacent to another rocket triggers a combo: 3-wide explosion in both horizontal and vertical directions. The 3×3 intersection area hits obstacles twice — enough to destroy a 2 HP vase.

## Project Structure

```
Assets/
├── Editor/                     Editor-only tools
│   ├── LevelEditorMenu.cs     Tools → Set Level Number
│   └── SpriteMirrorTool.cs    Tools → Mirror Sprite Horizontally
│
├── Prefabs/
│   ├── Cube_Prefab             SpriteRenderer + BoxCollider2D + CubeView
│   └── GoalItem                GoalItemUI prefab for top bar
│
├── Resources/
│   └── Levels/                 Level JSON files (level_01 through level_10)
│
├── Scenes/
│   ├── MainScene               Main menu with level button
│   └── LevelScene              Gameplay scene
│
├── Scripts/
│   ├── Data/                   Pure DTOs and enums (13 files)
│   ├── Logic/                  Game rules, pure C# (16 files)
│   └── View/                   Unity MonoBehaviours + animation (15 files)
│
├── Sprites/                    All visual assets
│
└── Tests/
    └── EditMode/               NUnit tests (18 test files, 250+ test cases)
```

## Architecture

Three-layer separation with strict dependency direction:

```
┌─────────────────────────────┐
│         DATA LAYER          │  Pure DTOs, enums, structs
│  Coordinate, BlastResult,   │  No behavior, no dependencies
│  TurnResult, ItemIds, ...   │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│         LOGIC LAYER         │  Pure C# game rules
│  GameSession, Board,        │  No Unity dependency (except LevelParser)
│  RocketProcessor,           │  Fully NUnit-testable
│  DamageResolver, ...        │
└──────────────┬──────────────┘
               │ TurnResult (the only contract)
┌──────────────▼──────────────┐
│         VIEW LAYER          │  Unity MonoBehaviours
│  GameOrchestrator,          │  DOTween animations
│  BoardView, CubeView,       │  Object pooling
│  AnimationController, ...   │  UI management
└─────────────────────────────┘
```

The Logic layer produces a `TurnResult` containing ordered animation steps. The View layer consumes it sequentially. They never share mutable state.

## Key Design Patterns

| Pattern | Where | Why |
|---------|-------|-----|
| Strategy | MatchStrategy / ClassicMatchStrategy | Swap match rules without touching other systems |
| Factory | ItemFactory | Single place to create GridItems from string IDs |
| Object Pool | CubePool, ParticleEffectController | Zero GC during gameplay |
| Facade | BoardView | Thin entry point delegating to 4 subsystems |
| Mediator | GameOrchestrator | Bridges Logic and View without coupling them |
| Repository | ILevelPersistence | Logic layer doesn't depend on PlayerPrefs |

## Testing

250+ NUnit tests cover the entire Logic layer.

### Run Tests
1. Window → General → Test Runner
2. Select EditMode tab
3. Click Run All

### Test Coverage

| Test File | What It Tests |
|-----------|---------------|
| BoardTests | Grid operations, obstacle counting, win detection |
| ClassicMatchStrategyTests | Flood-fill matching, blast execution |
| ComboDamageTests | Dual-direction combo damage, vase 2-hit kills |
| DamageResolverTests | Per-obstacle immunity rules, vase cap |
| GameSessionTests | Full tap-to-result integration |
| GravityProcessorTests | Falling, non-movable blocking, refill |
| HintCalculatorTests | Group detection, rocket eligibility |
| RocketExplosionTests | Single rocket path tracing, chain reactions |
| RocketProcessorTests | Creation, explosion, triggered rockets |
| TapResolverTests | Priority: combo > rocket > blast > none |

## Level Format

Levels are JSON files in `Resources/Levels/`:

```json
{
  "level_number": 1,
  "grid_width": 9,
  "grid_height": 10,
  "move_count": 20,
  "grid": ["r", "g", "b", "y", "rand", "bo", "s", "v", ...]
}
```

Grid array starts from bottom-left, fills horizontally row by row. Item codes: `r` red, `g` green, `b` blue, `y` yellow, `rand` random, `bo` box, `s` stone, `v` vase.

Grid dimensions must be between 6 and 10 cells.

## Editor Tools

### Set Level Number
`Tools → Set Level Number` — Jump to any level for testing. Persists via PlayerPrefs.

## Third-Party Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| DOTween | Latest | Tween animations (blast, gravity, UI transitions) |
| TextMeshPro | Built-in | UI text rendering |

## Scene Flow

```
MainScene                          LevelScene
┌──────────┐    Tap Button    ┌─────────────────┐
│ Level    │ ───────────────► │ Gameplay        │
│ Button   │                  │                 │
│ "Level X"│                  │ Win ──► Stars   │──► MainScene
│          │                  │        Continue │
│          │ ◄─── Close ──── │ Lose ──► Popup  │
│          │                  │        TryAgain │──► Reload
└──────────┘                  └─────────────────┘
```

## Technical Notes

- Portrait orientation, 9:16 aspect ratio, reference resolution 1080×1920
- Object pooling for cubes and particles — zero Instantiate/Destroy during gameplay
- Sorting order derived from Y coordinate for 3D stacked cube illusion
- Board frame uses 9-slice SpriteRenderer (drawMode = Sliced)
- Hint system is stateless — walks all cells every update, CubeView owns its hint flag
- Chain reactions processed via queue with 200-depth safety cap
- Level progress persisted via PlayerPrefs
