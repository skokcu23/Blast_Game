# Architecture Decision Records
## Blast Game — Key Design Decisions

Each record documents a significant architectural choice: the context, the decision, alternatives we considered, and why we chose this path.

---

### ADR-1: Three-Layer Separation (Data / Logic / View)

**Context:**
Unity projects commonly put game logic inside MonoBehaviours, coupling rules to the rendering engine. This makes testing impossible without Play Mode and creates fragile code where a visual change can break game rules.

**Decision:**
Separate the codebase into three strict layers:
- **Data** — pure DTOs and enums with no behavior
- **Logic** — pure C# classes with no Unity dependency (except LevelParser for Resources.Load)
- **View** — MonoBehaviours that only animate and render

The Logic layer communicates to the View exclusively through `TurnResult`, a DTO containing an ordered list of animation steps.

**Alternatives considered:**
- *MVC within MonoBehaviours:* Simpler setup, but still untestable without Unity. Rejected because testability was a hard requirement for correctness.
- *Full ECS (Unity DOTS):* Overkill for a 2D puzzle game. Adds complexity without benefit at this scale.

**Consequences:**
- 220+ NUnit tests run in milliseconds without starting Unity
- Logic bugs are caught in the test suite, not during play testing
- View layer can be completely rewritten (e.g., for 3D) without touching any game rules
- Trade-off: some data duplication between layers (Board state exists in Logic, visual state in GridStateManager)

---

### ADR-2: TurnResult as the Single Logic→View Contract

**Context:**
The View needs to know what happened after a tap: which cubes were destroyed, whether a rocket was created, what gravity movements occurred, etc. The question was how to communicate this.

**Decision:**
`GameSession.ProcessTap()` returns a `TurnResult` containing an ordered `List<TurnStep>`. Each step has a type (Blast, RocketCreated, Gravity, Refill, etc.) and exactly one populated data field. The View iterates these steps sequentially, awaiting each animation before starting the next.

**Alternatives considered:**
- *Callback/event system:* Logic fires events (`OnCubeDestroyed`, `OnRocketCreated`) that the View subscribes to. Rejected because ordering becomes implicit and fragile — if two events fire in the wrong order, animations break. With TurnResult, ordering is explicit and deterministic.
- *Direct Board observation:* View diffs the Board state before/after a tap. Rejected because it loses intermediate states (e.g., rocket created then immediately exploded in the same tap — the diff only shows the final state).
- *Coroutine-driven back-and-forth:* Logic yields control to View after each sub-step. Rejected because it couples the two layers at the execution level, making Logic untestable.

**Consequences:**
- Logic is a pure function: `Board + Tap → TurnResult`. Fully deterministic, fully testable.
- View is a pure consumer: iterate steps, animate each, done.
- Trade-off: TurnResult can be verbose for complex turns (combo + chain + gravity + refill = many steps). Acceptable for this game's scale.

---

### ADR-3: Dual Processed Sets for Combo Damage

**Context:**
A rocket-rocket combo fires two independent explosions (horizontal and vertical) from the same origin. The 3×3 center area is where both directions overlap. Vases (2 HP) in this area should take 2 damage (one from each direction) and be destroyed. Cubes (1 HP) should die on the first hit; the second direction passes through empty cells.

**Decision:**
Each direction traces with its own `HashSet<Coordinate> processed`. The horizontal explosion damages cells first. The vertical explosion damages cells independently — it doesn't know what H already did. For a vase at (2,2): H hits it (2→1), adds to processedH. V has its own processedV, finds the vase still alive, hits it (1→0).

`TracePathCombo` starts AT the origin cell (not origin + direction) to ensure every cell in the 3×3 area is visited by both directions.

**Alternatives considered:**
- *Single shared processed set + double damage in 3×3:* A separate `ProcessCellDamage` function that applies `TryDamage` twice for obstacles in the 3×3 area. Rejected because it's a special case — the geometry of two independent explosions should handle it naturally.
- *Explicit 3×3 damage phase before directional tracing:* Process the 3×3 area first, then trace outward. Rejected because it creates two different damage paths (area vs. directional) that can conflict and are harder to reason about.

**Consequences:**
- No special-case code for the 3×3 area. The geometry does the work.
- Cubes die on first hit (H), V sees empty — 1 hit total. Correct.
- Vases take 1 hit from H, 1 from V — 2 hits total. Correct.
- Works for any obstacle HP value, not just the current 1 and 2.

---

### ADR-4: Stateless Hint System (Derived State, Not Managed State)

**Context:**
Rocket hints highlight cubes in groups of 4+. After each turn, gravity moves cubes to new positions. The old approach tracked which coordinates had hints and tried to diff old vs. new sets — but coordinates become stale after gravity, causing hints to stick on moved cubes.

**Decision:**
Hints are treated as derived state, not managed state. `GridStateManager.UpdateHints()` walks every active cell on every update and tells each one: "you're hinted" or "you're not." `CubeView.ApplyHint()` and `RemoveHint()` are idempotent — calling them when already in the target state is a no-op (no duplicate animations). No coordinate tracking. No diffing.

**Alternatives considered:**
- *Coordinate-based diffing with gravity compensation:* Track old hint coordinates, update them when cells move during gravity. Rejected because it requires the hint system to understand gravity — a coupling that shouldn't exist.
- *Clear all hints, then reapply:* Force every cell to remove its hint, then apply new ones. Rejected because it causes unnecessary animation on cells whose hint state didn't change.

**Consequences:**
- Immune to gravity — works on current dict keys, not remembered old coordinates
- Immune to refill — new cells start unhinted, UpdateHints sets them correctly
- O(n) per update where n = active cells. Acceptable for grids up to 10×10 = 100 cells.
- Idempotent ApplyHint/RemoveHint means no animation flicker even if called redundantly

---

### ADR-5: Object Pooling via CubePool

**Context:**
A 9×10 grid has 90 cubes. Each blast destroys some and refill creates new ones. Without pooling, this means `Instantiate()` and `Destroy()` every turn, causing GC spikes that produce frame drops on mobile devices.

**Decision:**
`CubePool` pre-creates CubeView instances at level start, deactivates them, and reuses them via `Get()` / `Return()`. Only `GridStateManager` calls Get/Return — no other class touches the pool. Auto-grows by 10 if the pool empties during gameplay.

**Alternatives considered:**
- *Unity's built-in ObjectPool<T>:* Available in Unity 2021+, but our CubeViews need custom reset logic (kill DOTween animations, clear sprite, reset hint state). A custom pool with a `Reset()` contract on CubeView is cleaner.
- *No pooling (Instantiate/Destroy):* Simpler code. Rejected because GC spikes are measurable on mobile and this is a mobile-first game.

**Consequences:**
- Zero GC allocations during gameplay for cube creation/destruction
- Pool prewarms at level start: `boardWidth * boardHeight + 20` instances
- `CubeView.Reset()` is the single cleanup contract — ensures no stale state leaks between reuses
- Trade-off: slightly higher memory footprint at level start (pre-allocated objects). Negligible for grids up to 100 cells.
