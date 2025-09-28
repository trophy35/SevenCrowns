SevenCrowns – Agent Guidelines

-------------------------------
Purpose

You are generating code for the SevenCrowns project - AAA game (Unity 6 LTS).
RULES (always apply before writing CODE):
1) REUSE SCAN — Search the repository for existing classes, services, or utilities that already solve or partially solve the problem. Output a table: {File, API/Class, What to reuse, Gaps}.
2) PLAN — If reuse is possible, integrate or extend existing code. If not, explain briefly why new code is needed.
3) CONTRACTS — List which existing APIs you call and any new public APIs you expose.
4) NO DUPLICATES — Never reimplement functions that already exist. If similar logic is needed, refactor into a shared utility/service instead.
UNITY/SEVENCROWNS SPECIFICS:
- Always use Unity Localization (String Tables) for UI text/labels. No hardcoded strings. Add FR/EN keys.
- Respect memory management and performance (low allocations, Addressables handles, batching).
- Follow SOLID principles (see below) and Unity’s official C# naming conventions.
- All comments must be in English.
- When it's possible Unity Tests unit are implemented to ensure there is no regressions when we implement new features.

OUTPUT FORMAT:
Sections in order: REUSE SCAN, PLAN, CONTRACTS, CODE.

---------------------------------

Global Context
- SevenCrowns is a Unity project (C#) combining world exploration and RPG systems. Entry flow starts at the Boot scene, preloads content (Addressables/Localization), then transitions to gameplay/menu scenes.

Architecture
- Assemblies: `Assets/Game/Scripts/Systems/Game.Core.asmdef` (systems, scene flow, boot), `Assets/Game/Scripts/UI/Game.UI.asmdef` (UI components), tests under `Assets/Game/Scripts/Tests/EditMode`.
- Boot flow: `Assets/Game/Content/Scenes/Boot.unity`, `Assets/Game/Scripts/Systems/BootLoaderController.cs` orchestrates preload tasks and progress UI.
- Preload tasks: derive from `BasePreloadTask` and optionally `IRuntimeWeightedTask` for runtime weights.
- Addressables: use `PreloadRegistry` or `LifetimeContentService` to manage handles and lifetimes.
- Scene transitions: `SceneFlowController` with optional fader.

Key Docs
- Main rules: `Assets/AI_Context/Main_rules.md`
- Folders: `Assets/AI_Context/Folders_structure.md`
- Preloading: `Assets/AI_Context/Preloading_assets.md`
- Design: `Assets/AI_Context/GameDesign.md`

Conventions
- Unity lifecycle: prefer `OnEnable`/`OnDisable` for event wiring; avoid reflection per frame.
- C#: consistent naming, null checks, guard clauses; no one-letter vars.
- Keep PRs/changes surgical; avoid unrelated refactors; no license headers.
- Logging: be sparing; prefer warnings/errors only when actionable.
- Addressables: keep handles alive via registry/service; release on scene transitions as appropriate.

UI/UX
- Localization: when available, use tables (e.g., `UI.Common`) and preload via tasks.

Testing
- Unit tests live in `Assets/Game/Scripts/Tests/EditMode`; prefer focused tests near the changed code.
- Always create unit tests when it's possible with nominal cases and edge cases
- Don’t add a new test framework; mirror existing patterns.

Definition of Done
- Project compiles; boot flow loads without errors; tests for changed areas pass.
- No new warnings/errors in Console related to changes; scene flow remains intact.

Don’ts
- Don’t introduce new third-party deps without explicit approval.
- Don’t rename/move assets unless necessary and coordinated.
- Don’t change scene wiring unless requested; if you must, document it clearly.

How to Work Here
- Fix root causes, not symptoms. Keep changes minimal and consistent with current style.
- Update inline documentation or this file if guidance changes.
- When ambiguous, ask for clarification and propose a small plan before large edits.

Pointers & Entry Points
- Boot: `Assets/Game/Content/Scenes/Boot.unity`
- Progress bar: `Assets/Game/Scripts/UI/UiProgressBar.cs`
- Boot controller: `Assets/Game/Scripts/Systems/BootLoaderController.cs`
- Addressables tasks: `Assets/Game/Scripts/Systems/AddressablesLoadKeysTask.cs`
- Localization tasks: `Assets/Game/Scripts/Systems/LocalizationPreloadTask.cs`

---------------------------------------------
SevenCrows respects strictly SOLID Principles
---------------------------------------------

The SOLID design principles guide how we structure agent code and behaviors for clarity, maintainability, and extensibility.

1. Single Responsibility Principle (SRP)

Definition: An agent or component should have one clearly defined responsibility.
Why: Limits complexity, improves readability, and reduces unintended side effects.

Example:
✅ A DialogueManager handles conversation flow.
❌ A DialogueManager that also fetches APIs, manages state, and logs telemetry.

2. Open/Closed Principle (OCP)

Definition: Agents should be open for extension, but closed for modification.
Why: Adding new features should not require rewriting existing code.

Example:
✅ Add new output formats via plugins implementing an IFormatter interface.
❌ Modify the core ResponseEngine class every time a new format is introduced.

3. Liskov Substitution Principle (LSP)

Definition: Subtypes must be replaceable for their base types without breaking correctness.
Why: Guarantees safe polymorphism, avoids fragile inheritance.

Example:
✅ Any AgentTask subclass can replace BaseTask in the scheduler.
❌ A subclass throws exceptions when used in place of the base type.

4. Interface Segregation Principle (ISP)

Definition: Prefer many small, specific interfaces over one large, general interface.
Why: Prevents agents from depending on methods they don’t need.

Example:
✅ Separate ILocalizationProvider and ILoggingProvider.
❌ One ISystemProvider with 20 unrelated methods.

5. Dependency Inversion Principle (DIP)

Definition: Depend on abstractions, not concretions. High-level modules shouldn’t rely on low-level details.
Why: Enables inversion of control, testability, and flexible architecture.

Example:
✅ AgentPlanner depends on an IDataStore interface injected at runtime.
❌ AgentPlanner directly instantiates a FileDatabase.

Practical Guidance for Agents
Keep each agent and helper module focused on one job.
Add new behaviors via extension, not by hacking the old.
Ensure subtypes behave like their parents without surprises.
Break large interfaces down to what each agent actually needs.
Use injection and abstractions so agents can evolve without rewiring everything.
👉 Following SOLID keeps our agent codebase scalable, testable, and maintainable as the system grows.

------------------------------------------------------------------
Unity Unit Tests — Best Practices
------------------------------------------------------------------
- Test Types & When to Use

Edit Mode tests (fast, no player loop): default for pure logic, data models, utility classes, domain rules, serializers, services with mocked dependencies.
Play Mode tests (with player loop): only when behavior needs MonoBehaviour lifecycle, coroutines, physics, animation, scene loading, or frame-driven systems.
Integration tests: limited scope; verify composition (small graphs of components/services). Keep under 1–2 seconds each.

- Naming, Structure, Style

File/class naming: {SUT}Tests.cs or {SUT}_{Concern}Tests.cs.
Test naming: MethodOrScenario_Should_ExpectedOutcome_[Condition].
AAA pattern: Arrange – Act – Assert. No hidden work in asserts.
One logical assertion per test (multiple low-level asserts OK if they validate a single behavior).
Keep tests <100ms (Edit Mode) and <500ms (Play Mode) whenever possible.

- Determinism & Isolation

No reliance on global state, time, randomness, or frame timing without control.
Use seeded RNG; wrap time in an abstraction (ITimeProvider) and inject a fixed clock in tests.
No network, file IO, or real Addressables in unit tests—mock them.
Each test creates and disposes its own objects/scene state. Use [SetUp]/[TearDown] or [OneTimeSetUp] for shared fixtures.

- Assertions & Test API

Prefer constraint-based asserts (Assert.That(actual, Is.EqualTo(expected))) for clarity.
Use [TestCase], [TestCaseSource] for data-driven coverage.
For coroutines or frame-driven checks, use [UnityTest] and yield return null (keep frame counts minimal).
When asserting floats/physics, use tolerances: Is.EqualTo(expected).Within(1e-4f).

- Scenes, GameObjects, and Lifecycles

Instantiate minimal hierarchies; avoid loading full game scenes in unit tests.
In Play Mode, prefer additive mini-scenes created by the test, released after the test.
Use GameObject + required components only; avoid FindObjectOfType/singletons—inject references.

- Depend on interfaces/abstractions (DIP).

Use lightweight fakes or hand-rolled stubs for clarity and zero allocations; introduce a mock framework only if it reduces code.
Verify interactions that matter (calls, parameters) but focus primarily on observable behavior.

- Concurrency, Coroutines, Async

Keep coroutine tests short; prefer testing resulting state rather than internal yields.
For async APIs (e.g., UniTask/Task), expose awaitable methods that can be awaited in Edit Mode tests when possible (no engine API usage).
If the code touches Unity APIs, test with [UnityTest] and control time/frames deterministically.

- Performance & Allocations (Unity-aware)

Guard hot paths with allocation-free assertions when feasible (e.g., verify no boxing/log spam in tight loops).
In Play Mode tests, set physics/animation to minimal usage and avoid long waits/sleeps.
Never assert on exact frame counts unless inherent to the contract; assert on state within a bounded window.

- Flakiness Kill-Switches

No WaitForSeconds in unit tests. If timing must be tested, use manual ticking of your own clock or small frame advances with robust conditions.
Avoid relying on Editor settings or PlayerPrefs; if unavoidable, back them up and restore.

-----------------------------------------
Tile Occupancy & Path Blocking — Reuse Patterns
-----------------------------------------

Rule
- A tile cannot be occupied by two heroes at the same time. When a hero is on a tile, that tile is blocked for other heroes; pathfinding must skirt it or fail if no route exists.

Files
- Occupancy service: `Assets/Game/Scripts/Map/GridOccupancyService.cs` (implements `IGridOccupancyProvider`)
- Occupancy overlay provider: `Assets/Game/Scripts/Map/BlockingOverlayTileDataProvider.cs` (`ITileDataProvider` decorator)
- Hero component: `Assets/Game/Scripts/Map/HeroAgentComponent.cs` (injects dynamic blocking; reports moves)
- Click-to-move: `Assets/Game/Scripts/Map/ClickToMoveController.cs` (uses blocking overlay for A*)
- Pathfinding: `Assets/Game/Scripts/Map/AStarPathfinder.cs` (unchanged; consumes provider)
- Movement agent: `Assets/Game/Scripts/Map/HeroMapAgent.cs` (optional `isBlocked` predicate)

Architecture
- Discoverability-first: UI and Map systems auto-discover services if not assigned in Inspector (keeps scenes flexible).
- Dynamic blocking is applied at two levels:
  - Pathfinding-time via `BlockingOverlayTileDataProvider` to avoid planning through occupied tiles.
  - Movement-time via `HeroMapAgent` injected predicate to stop if a tile becomes occupied between plan and commit.
- The current hero is excluded when wrapping the provider so their own start cell is not treated as blocked.

Scene Wiring
- Place exactly one `GridOccupancyService` in the world map scene (Core object is recommended).
- `ClickToMoveController`:
  - Optional `MonoBehaviour _occupancyBehaviour` field; if left null, it discovers an `IGridOccupancyProvider` in scene.
  - Internally wraps `TilemapTileDataProvider` with `BlockingOverlayTileDataProvider` and sets excluded hero on selection change.
- `HeroAgentComponent`:
  - Automatically discovers `IGridOccupancyProvider` and injects `isBlocked = c => occupancy.IsOccupiedByOther(c, self)` into `HeroMapAgent`.
  - On position changes, calls `GridOccupancyService.UpdateHeroPosition(self, from, to)` to keep occupancy in sync.

Contracts
- Map provides: `ITileDataProvider` from `TilemapTileDataProvider`.
- Core provides: `IGridOccupancyProvider` via `GridOccupancyService`.
- Pathfinding consumes: `ITileDataProvider` (the blocking overlay decorator is transparent to A*).
- Movement consumes: `isBlocked(GridCoord)` predicate (optional) to guard last‑moment conflicts.

Extending
- New movement systems should depend on `IGridOccupancyProvider` (or a predicate) rather than duplicating occupancy logic.
- For custom path queries, wrap your `ITileDataProvider` with `BlockingOverlayTileDataProvider` and call `SetExcluded(heroIdentity)` when applicable.
- Runtime spawns/despawns: after adding/removing heroes, call `GridOccupancyService.Refresh()` and `SelectedHeroService.RefreshHeroes()` to rescan scene state.

Testing
- Keep Edit Mode tests focused and engine‑free. Examples:
  - Pathfinding avoids occupied tiles and fails when the goal is occupied.
  - Movement stops with `BlockedByTerrain` when the next tile is occupied.

Pitfalls
- Lambda captures in event wiring: when subscribing inside loops, capture the current `HeroIdentity` in a local variable.
- Provider types: the blocking overlay returns a shared impassable `TileData` for occupied cells; avoid per‑call allocations.
- Multiple occupancy services: ensure there is only one active `GridOccupancyService` per scene.

-----------------------------------------
— World Map Radial Menu: Reuse Patterns —
------------------------------------------

Files

UI controller: Assets/Game/Scripts/UI/WorldMapRadialMenuController.cs
Asset provider: Assets/Game/Scripts/Systems/PreloadRegistryAssetProvider.cs (implements Game.UI IUiAssetProvider)
Turn handler: Assets/Game/Scripts/Systems/WorldMapTurnController.cs
World input: Assets/Game/Scripts/Map/ClickToMoveController.cs
Add A New Button (minimal steps)

Localization: add EN/FR keys to String Table (e.g., UI.Common/MyAction, UI.Common/MyActionDesc).
Addressables:
Icon: prefer Sprite sub-asset address (child row), e.g., UI/Icons/MyAction[MyAction_0].
SFX: AudioClip key, e.g., SFX/UI/MyAction.
Optional: add keys to AddressablesLoadKeysTask for preload.
In a UI controller (extend WorldMapRadialMenuController or sibling), build info + register:
Create UltimateRadialButtonInfo { name, description, icon, key/id }.
_menu.RegisterButton(OnClick, info, -1); then _menu.UpdatePositioning().
Keep selectButtonOnInteract = false if you want hover feedback after clicking.
Hook behavior: expose a UnityEvent or call a Core service in OnClick; reuse TryPlaySfx() pattern.
Input & UX Defaults (reliable one-click)

EventSystem: exactly one UltimateRadialMenuInputManager with:
invokeAction = OnButtonDown
enableMenuSetting = Manual
Radial menu object:
useButtonIcon = true
minRange ≈ 0.0–0.25, infiniteMaxRange = false (finite ring)
Avoid sticky selection if hover desired: selectButtonOnInteract = false
Assets & Decoupling

UI never references Core types directly; it uses Game.UI IUiAssetProvider.
Core provides PreloadRegistryAssetProvider in the scene to resolve Addressables.
Prefer Sprite sub-asset keys. If a key resolves to Texture2D:
Fix the Addressables entry, or temporarily enable “Convert Texture To Sprite” on PreloadRegistryAssetProvider.
Diagnostics: PreloadRegistry.TryGetRaw(key, out object raw) to see actual loaded type.
SFX First-Click Polishing

Warm up AudioClip on scene start (WorldMapRadialMenuController) and cache it.
Fall back to a local AudioClip if fetch isn’t ready so the first click has sound.
End-Turn Flow (example)

UI: WorldMapRadialMenuController.OnEndTurnRequested → wire to WorldMapTurnController.OnEndTurnRequested.
Core: WorldMapTurnController resets all heroes’ MP (IMapMovementService.ResetDaily()) and clears paths (IHeroMapAgent.ClearPath()).
Visual update: ClickToMoveController listens to Movement.Changed and re-renders the path preview (PathPreviewRenderer.Show).
World Input vs UI

ClickToMoveController ignores clicks when the pointer is over UI using a robust EventSystem raycast (works with Standalone/InputSystem UI modules).
Ensure Canvas has a GraphicRaycaster and radial button Images are raycast targets.
Localization

UI strings live in String Table UI.Common; ensure LocalizationPreloadTask includes UI.Common.
Add EN/FR entries for every new button: Name + Description keys.
Common Pitfalls

Sprite not showing: your key is likely a Texture2D (parent). Use the Sprite sub-asset address (child row) or enable conversion temporarily.
Double-click feel / no hover after click: check invokeAction (use OnButtonDown) and set selectButtonOnInteract = false.
World clicks triggering while over menu: ensure the input ignore-over-UI guard is active in ClickToMoveController.

-------------------------------------
Hero Portraits: Reuse Patterns
-------------------------------------

Files

- UI view: Assets/Game/Scripts/UI/HeroPortraitView.cs
- UI contract: Assets/Game/Scripts/UI/ICurrentHeroPortraitKeyProvider.cs
- Core service: Assets/Game/Scripts/Systems/CurrentHeroService.cs
- Asset provider: Assets/Game/Scripts/Systems/PreloadRegistryAssetProvider.cs (implements Game.UI IUiAssetProvider)
- Optional preload: Assets/Game/Scripts/Systems/AddressablesLoadKeysTask.cs

Goal

Display the current hero portrait (Addressables Sprite) in UI, decoupled from Core.

Minimal Setup (steps)

1) Scene services:
   - Ensure one PreloadRegistryAssetProvider exists in the scene (used by all UI to resolve Addressables).
   - Add CurrentHeroService to a Core object and configure entries: { heroId, portraitKey }.
     - Use Sprite sub-asset addresses for portraitKey, e.g., UI/Heroes/Knight01[Knight01_0].
     - Optionally set Default Hero Id to select at scene start.
2) UI binding:
   - Add HeroPortraitView to the target uGUI Image in the HUD.
   - Optionally assign a fallback Sprite for immediate visuals while Addressables warms up.
3) Switching hero:
   - From selection code, call CurrentHeroService.SetCurrentHeroById(heroId) or SetCurrentHeroDirect(id, portraitKey).
   - HeroPortraitView listens to ICurrentHeroPortraitKeyProvider.CurrentHeroChanged and updates automatically.
4) Preload (recommended):
   - Add all portrait keys to AddressablesLoadKeysTask so first-use has no hitch.

Contracts

- UI consumes:
  - Game.UI IUiAssetProvider.TryGetSprite(key, out Sprite) via PreloadRegistryAssetProvider.
  - Game.UI ICurrentHeroPortraitKeyProvider (CurrentHeroService implements it in Core).
- Core exposes:
  - CurrentHeroService API: SetCurrentHeroById(string id), SetCurrentHeroDirect(string id, string portraitKey).

Guidelines

- Decoupling: UI never references Core types directly; it searches for interfaces in the scene (IUiAssetProvider, ICurrentHeroPortraitKeyProvider).
- Addressables: Prefer Sprite sub-asset addresses. If a key resolves to Texture2D, fix Addressables or temporarily enable conversion on PreloadRegistryAssetProvider.
- Localization: Portraits are images (no strings). If you display hero names/labels, add EN/FR keys to UI.Common and preload Localization.
- Performance: Keep handles alive via PreloadRegistry; rely on late-binding retries in HeroPortraitView to avoid blocking the main thread.
- Memory: Release portrait handles on scene transitions using PreloadRegistry (bulk) if needed.

Common Pitfalls

- Wrong asset type: Sprite not showing if the key points to Texture2D (parent). Use child Sprite address.
- No provider in scene: Ensure exactly one PreloadRegistryAssetProvider is present before UI binds.
- Not preloaded: If not preloaded, first bind shows fallback; HeroPortraitView will late-bind within a short timeout.

-------------------------------------
Hero Selection – Reuse
-------------------------------------

Architecture

Contract lives in Game.Map: ISelectedHeroAgentProvider (prevents Game.Map ↔ Game.Core cycles).
Core implements selection via SelectedHeroService and mirrors to CurrentHeroService so UI (HeroPortraitView) updates automatically.
ClickToMoveController depends only on ISelectedHeroAgentProvider.
Scene Wiring Checklist

Per hero GameObject:
Add HeroIdentity and set HeroId (e.g., hero.harry).
Ensure HeroAgentComponent exists.
Add a Collider2D sized to the sprite for picking. Optional: put on “Heroes” layer.
Core services:
One CurrentHeroService with entries { heroId, portraitKey } and optional _defaultHeroId.
One SelectedHeroService with _portraitService assigned.
ClickToMoveController:
Assign _selectionBehaviour to SelectedHeroService (or rely on auto‑discovery).
Assign _grid, _provider, _preview, _camera.
_heroLayer can stay 0 to search all layers (more forgiving), or set to “Heroes” layer.
Keep _ignoreClicksOverUI = true to avoid fighting UI input.
Lifecycle

SelectedHeroService auto‑scans on Awake and Start (handles ordering). Call RefreshHeroes() after runtime spawns/despawns, then SelectById(id) if you need to focus.
CurrentHeroService holds the only maintained list of heroes (id → portrait key). Do not duplicate hero lists elsewhere.
Input & UX Flow

Click a hero: ISelectedHeroAgentProvider.SelectById(heroId) fired; portrait updates via CurrentHeroService; ClickToMoveController binds to the new hero.
Click on map:
First click: path preview to target respecting movement points.
Second click on same target: hero moves along the path.
Right‑click: cancels current order and clears preview.
World clicks are ignored when pointer is over UI (robust EventSystem raycast).
Diagnostics

Enable _debugLogs on SelectedHeroService and ClickToMoveController.
Key logs:
“[SelectedHeroService] Refreshed heroes. Count=X mapCount=X”
“Selected hero id=..., agentGO=...”
“[ClickToMove] Selected hero changed. HasHero=True”
“Pathfinder built with bounds …”
“Click ignored: pointer over UI.” (UI blocking)
“No hero detected under cursor.” (no collider/layer mismatch)
If preview doesn’t show: verify HeroIdentity present, Collider2D exists, TilemapTileDataProvider baked, and selection provider bound.
Contracts Summary

Map provides: HeroIdentity, ISelectedHeroAgentProvider
Core implements: SelectedHeroService (RefreshHeroes, event SelectedHeroChanged), CurrentHeroService (CurrentHeroChanged)
UI consumes: ICurrentHeroPortraitKeyProvider from CurrentHeroService; ClickToMoveController consumes ISelectedHeroAgentProvider
Pitfalls

Missing HeroIdentity or Collider2D → clicks won’t select.
Duplicating hero lists in multiple services → maintenance burden. Keep list only in CurrentHeroService.
Assembly cycles → keep ISelectedHeroAgentProvider in Game.Map; SelectedHeroService in Game.Core implements it.
UI intercepting clicks → enable _ignoreClicksOverUI and ensure Canvas has a GraphicRaycaster.
