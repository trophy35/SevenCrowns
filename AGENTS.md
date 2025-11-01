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

Note: When implementing features that alter persistent world state (heroes, ownership, resources, fog, time, camera, selection, population, etc.), remember to add the corresponding save/restore capture to the WorldMap save system (snapshot + reader apply) and extend tests to cover round‑trip behavior.

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
- Don't add a new test framework; mirror existing patterns.

Unit Test Requirement for New Work
- When adding new public APIs, features, or changing behavior, add Edit Mode unit tests in the same PR.
- Cover nominal paths and at least one edge/failure path; follow AAA and naming conventions.
- Co-locate tests under the relevant domain folder (e.g., Systems/Save, Map/*, UI/*) using `{SUT}Tests.cs` naming.
- For engine-heavy behavior (MonoBehaviour visuals), prefer lightweight fakes or authoring stubs; use Play Mode tests only if lifecycle/frame timing is required.
- Tests are part of the Definition of Done for new/changed functionality.

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

-------------------------------------
Save/Load — World Map
-------------------------------------

Files
- Contracts/DTOs: `Assets/Game/Scripts/Systems/Save/WorldMapSnapshot.cs`, `IWorldMapStateReader.cs`, `IWorldMapSaveService.cs`, `IWorldMapPersistence.cs`
- Serializer: `Assets/Game/Scripts/Systems/Save/JsonWorldMapSerializer.cs`
- Persistence: `Assets/Game/Scripts/Systems/Save/FileWorldMapPersistence.cs` (runtime), `InMemoryWorldMapPersistence.cs` (tests)
- Orchestrator: `Assets/Game/Scripts/Systems/Save/WorldMapSaveService.cs`
- Scene bridge: `Assets/Game/Scripts/Systems/Save/SaveGameServiceBehaviour.cs`
- UI hook: `Assets/Game/Scripts/UI/MainMenu/MainMenuController.cs` (Save button UnityEvent)
- State reader: `Assets/Game/Scripts/Systems/Save/WorldMapStateReader.cs`

What It Saves
- Heroes: id, level, grid position
  - Reads: `HeroIdentity` + `HeroAgentComponent.GridPosition`
  - Applies: `HeroIdentity.SetLevel(level)`, `HeroAgentComponent.TeleportTo(coord)` (clears path, updates occupancy)
- Resource Wallet: all resource amounts
  - Reads via `IResourceWalletSnapshotProvider` implemented by `ResourceWalletService`
  - Applies by reconciling deltas with `IResourceWallet.Add` / `TrySpend`
- Ownership: Cities, Mines, Farms
  - Reads `*NodeService.Nodes` into `CityOwnershipSnapshot`, `MineOwnershipSnapshot`, `FarmOwnershipSnapshot`
  - Applies by rebuilding descriptors (preserving world position/entry coord) and calling `RegisterOrUpdate`
- Resource Nodes (collection state)
  - Snapshot stores either `collectedResourceNodeIds` (explicitly removed) or `remainingResourceNodeIds` (present at save time)
  - Applies by calling `ResourceNodeAuthoring.TryGetNode(id)?.Collect()` or `ResourceNodeService.Unregister(id)` when authoring not found
- World Time
  - Reads from first `WorldTimeService.CurrentDate`
  - Applies with `WorldTimeService.ResetTo(WorldDate)`
 - Population
   - Reads from `IPopulationService.GetAvailable()` (weekly pool available)
   - Applies with `IPopulationService.ResetTo(populationAvailable)`

Persistence
- File backend writes to: `Application.persistentDataPath/Saves/{slotId}.json`
- Helper: `FileWorldMapPersistence.ResolvePath(slotId)` returns absolute file path (used for logging)

UI Wiring
- Add `SaveGameServiceBehaviour` to a Core object in the world scene; adjust Default Slot if needed
- In the Main Menu Canvas, assign `MainMenuController._saveButton` and wire `_onSaveRequested` → `SaveGameServiceBehaviour.Save()`
- `MainMenuController` hides itself after save is requested; `SaveGameServiceBehaviour` logs the saved file path

Extending
- Add new domains to `WorldMapStateReader` only; keep DTOs simple and version-tolerant
- Prefer capturing “what exists” (remaining ids) over “what was removed” unless you can guarantee a baseline
- For additional systems (e.g., fog, quests), expose small reader interfaces rather than using reflection

Testing
- Unit tests (Edit Mode):
  - `Assets/Game/Scripts/Tests/EditMode/Systems/Save/WorldMapSaveSkeletonTests.cs` (serializer/persistence/service + wallet)
  - `Assets/Game/Scripts/Tests/EditMode/Systems/Save/WorldMapOwnershipTests.cs` (cities/mines/farms ownership round-trip)
  - `Assets/Game/Scripts/Tests/EditMode/Systems/Save/WorldMapResourcesAndTimeTests.cs` (resource nodes remaining set, world time round‑trip)
- Prefer `InMemoryWorldMapPersistence` in tests; avoid file IO

Pitfalls
- Ensure only one active `WorldTimeService` in scene; the reader uses the first it finds
- Resource node collection: authoring objects may be destroyed at runtime; apply code unregisters when authoring is missing
- Keep resource ids normalized (e.g., `resource.gold`); wallet snapshot provider uses raw dictionary keys
- When spawning/despawning heroes or nodes at runtime, ensure the related services are in sync before calling Save

-------------------------------------
Save/Load — Advanced Domains
-------------------------------------

Overview
- Advanced domains extend the base snapshot to fully restore the player view and exploration state.
- Implementations use small provider interfaces to keep dependencies inverted and avoid reflection.

Fog of War
- Provider: `Assets/Game/Scripts/Map/FogOfWar/IFogOfWarSnapshotProvider.cs`
- Implementation: `FogOfWarService` implements capture/apply of per-cell states (Unknown/Explored/Visible).
- Snapshot fields: `fogWidth`, `fogHeight`, `fogStates` (row-major byte[]).
- Apply order: apply fog before camera/selection dependent visuals to avoid flicker.
- Bounds mismatch: loader clamps to min(width,height) when target map is smaller/larger.

Camera
- Provider: `Assets/Game/Scripts/Map/ICameraSnapshotProvider.cs`
- Implementation: `MapCameraController` implements get/apply; state clamped to tilemap bounds.
- Snapshot fields: `camX`, `camY`, `camZ`, `camSize`.
- Apply order: after fog and hero positions so clamping can account for bounds.

Hero Movement Points (MP)
- Snapshot: `HeroSnapshot` extended with `mpCurrent`, `mpMax`.
- Apply: `SetMax(mpMax, refill:false)` then adjust to `mpCurrent` using `Refund` or `SpendUpTo` (no hard reset calls).
- Notes: if `mpCurrent` is 0, no refund occurs; service remains at its current unless explicitly adjusted later.

Selection (Current Hero)
- Service: `CurrentHeroService` (mirrors to `SelectedHeroService`).
- Snapshot field: `selectedHeroId`.
- Apply: `CurrentHeroService.SetCurrentHeroById(selectedHeroId)`; selection propagates automatically.

Population
- Provider: `IPopulationService` (Systems/Population/PopulationService)
- Snapshot fields: `populationHasSnapshot`, `populationAvailable` (guards older saves)
- Apply: `IPopulationService.ResetTo(populationAvailable)` to restore mid‑week value exactly

Reader Integration & Order
- Capture (in `WorldMapStateReader`): heroes (pos/level/mp) → wallet → nodes → ownership → world time → fog → camera → selection.
- Apply (in `WorldMapStateReader`): heroes (teleport/mp) → fog (grid) → resources/nodes/ownership → world time → camera → selection.

Tests
- `Assets/Game/Scripts/Tests/EditMode/Systems/Save/WorldMapCameraSelectionFogTests.cs` validates camera/selection/fog capture/apply.

Gotchas
- World time and production: City/Mine/Farm production services process on `DateChanged` (and may tick once in Update). After load, ensure world time is applied before resuming gameplay to avoid double-awards.
- Multiple fog services/cameras: the reader targets the first provider found; keep a single authoritative instance per scene.

-------------------------------------
Main Menu — Reuse & Wiring
-------------------------------------

Files
- Controller: `Assets/Game/Scripts/UI/MainMenu/MainMenuController.cs` (Game.UI)
- Input Actions: `Assets/InputSystem_Actions.inputactions` (uses standard `Cancel` binding)
- Localization: `Assets/Game/Content/Localization/StringTables/UI/UI.Common_*` (reuses `Popup.Cancel`)

Goal
- Display the Main Menu Canvas when the player presses ESC/Cancel during gameplay.
- Hide the menu when the "Cancel" button is clicked (TextMeshPro button using Unity UI `Button`).

Behavior
- ESC toggles the menu: show if hidden, hide if visible.
- Clicking the Cancel button hides the menu.
- Supports both Unity Input System (keyboard ESC, gamepad East/B) and legacy `Input.GetKeyDown(KeyCode.Escape)` fallback.

Scene Wiring
1) Place the Main Menu UI (Canvas or top-level panel) in the scene. Keep it hidden by default.
2) Add `MainMenuController` to either the Canvas root or a parent controller object.
3) Assign fields on `MainMenuController`:
   - `_root`: the menu Canvas or top-level panel GameObject (hidden by default).
   - `_cancelButton`: the Cancel button’s `Button` component (TextMeshPro button has a standard `Button`).
   - `_startHidden`: true (default) to start the scene with the menu hidden.
   - `_listenForCancel`: true to let ESC toggle visibility automatically.
4) Localization: the Cancel button label should use `UI.Common/Popup.Cancel` (EN/FR already provided). Ensure `LocalizationPreloadTask` includes `UI.Common`.

Extending & Integration
- External systems can call `Show()`, `Hide()`, or `Toggle()` on `MainMenuController` (e.g., from a Pause/Flow service) instead of duplicating input logic.
- If you introduce a dedicated Pause service later, keep `MainMenuController` UI-only and have the service call its public API (SRP & DIP).

Input System Notes
- With the Input System enabled, ESC and gamepad East/B are supported via direct device checks. The project also defines a `Cancel` action; you can optionally wire that action to call `Toggle()` if centralizing input.
- Legacy Input Manager fallback uses `Input.GetKeyDown(KeyCode.Escape)`.

Testing
- Edit Mode tests live at `Assets/Game/Scripts/Tests/EditMode/UI/MainMenuControllerTests.cs`:
  - `ShowHideToggle_ControlsRootActiveState` verifies visibility changes.
  - `CancelButton_Click_HidesMenu` verifies the Cancel button hides the menu.
- Keep tests focused, engine-light, and avoid scene loads; invoke `Show/Hide/Toggle` programmatically.

Best Practices
- One menu instance per scene; avoid multiple active menus.
- Do not hardcode UI strings; reuse `UI.Common` entries. Add new keys (EN/FR) as needed for future buttons.
- Keep the controller free of gameplay logic (UI only). Route gameplay effects to core services via events or injected interfaces.

Pitfalls
- Ensure the Cancel button has a `Button` component (TextMeshPro provides text; you must still add `Button`).
- If references are assigned dynamically (e.g., via code), make sure the controller is enabled or call `Show()` once to ensure listeners are wired.

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
Audio SFX - Reuse
-------------------------------------

Pattern
- WorldMapRadialMenuController (UI) and ResourceWalletService (Core) share a two-stage approach for Addressable SFX.

How it works
1) Discover IUiAssetProvider in-scene (or via serialized reference).
2) Ensure an AudioSource (2D) exists on the component; do not create transient sources per play.
3) Warm up the Addressable clip with a coroutine (poll provider or PreloadRegistry) so the first click is instant.
4) Cache the AudioClip once resolved, then play with AudioSource.PlayOneShot.
5) Keep volume configurable and default spatialBlend = 0.

When adding new SFX
- Reuse the helper logic (EnsureAudioSource, ResolveAssetProvider, warmup routine).
- Trigger PreloadRegistry.TryGet<AudioClip> before fallback to provider.
- Only spin up new coroutines if the clip is missing; stop them on OnDisable.

-------------------------------------

UI Tabs — Reuse Patterns
-------------------------------------

Files
- Controller: `Assets/Game/Scripts/UI/Tabs/VerticalTabsController.cs` (Game.UI)
- Item View: `Assets/Game/Scripts/UI/Tabs/VerticalTabItemView.cs` (Game.UI)
- Tests: `Assets/Game/Scripts/Tests/EditMode/UI/CityTabsControllerTests.cs`

Goal
- Reusable vertical (top-to-bottom) tabs for City and other scenes.
- Each tab: left focus line when selected, icon (`Image`), label (`TextMeshProUGUI`).
- Colors: Focused RGB(246,225,156), Unfocused RGB(190,181,182). Focus line enabled on selected only.
- Use separator `Image` between items for lines (visual only).

Contracts
- `VerticalTabsController`
  - `string SelectedTabId { get; }`
  - `UnityEvent<string> OnSelectionChanged` (raised on selection change)
  - `void SelectById(string id)`, `void SelectByIndex(int index)`
  - Auto-discovers child `VerticalTabItemView` when list is empty; selects `_defaultTabId` or first (when `_selectFirstOnStart`).
- `VerticalTabItemView`
  - `string TabId { get; }`
  - `Button Button`, `Image Icon`, `TextMeshProUGUI Label`
  - `void SetFocused(bool focused, Color32 focusedColor, Color32 unfocusedColor)` applied by controller
  - Localization via `LocalizedString`; Edit Mode tests bypass runtime localization.

Localization
- Add EN/FR keys in `UI.Common`:
  - `CityTabs.Building` → EN "Building" / FR "Bâtiments"
  - `CityTabs.Recruit` → EN "Recruit" / FR "Recruter"
  - `CityTabs.Magic` → EN "Magic" / FR "Magie"
  - `CityTabs.Research` → EN "Research" / FR "Recherche"
  - `CityTabs.Defense` → EN "Defense" / FR "Défense"
- Ensure `LocalizationPreloadTask` includes `UI.Common` (default).

Scene Wiring (City)
1) Create a panel with `VerticalLayoutGroup`; add `VerticalTabsController`.
2) For each tab: create a child with `Button`; add children `Image` (icon), `TextMeshProUGUI` (label), left focus line `Image`.
3) Add `VerticalTabItemView` to each tab root; assign Button/Icon/Label/FocusLine; set `Tab Id` and label entry (`UI.Common/CityTabs.*`).
4) In `VerticalTabsController.OnSelectionChanged`, wire to City logic to show/hide panels.

Edit Mode & Tests
- No per-frame work; selection is event-driven.
- Edit Mode sets label text directly (no LocalizationSettings dependency) to keep tests deterministic.
- Unit tests: see `CityTabsControllerTests` for selection and color assertions.

Performance & Extensibility
- Low allocations (cached refs); no coroutines.
- Layout-agnostic: can be used in horizontal setups; controller logic is independent of layout direction.

-------------------------------------

- World Map Radial Menu: Reuse Patterns -
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

-------------------------------------
Generic Popup System — Reuse
-------------------------------------

Files
- Contracts & requests: Assets/Game/Scripts/UI/Popups/IPopupService.cs, PopupRequest.cs, PopupOptionDefinition.cs, PopupResult.cs, PopupOptionIds.cs
- View layer: Assets/Game/Scripts/UI/Popups/PopupView.cs, PopupButtonView.cs
- Service orchestrator: Assets/Game/Scripts/UI/Popups/PopupService.cs
- Example usage: Assets/Game/Scripts/Systems/WorldMapTurnController.cs (end-turn confirmation)

Scene Wiring
1) Place a PopupService MonoBehaviour on a UI canvas/root. Assign its PopupView prefab and optional parent transform (leave _instantiateOnAwake true for reuse).
2) PopupView prefab: CanvasGroup + TMP title/message + button container populated with PopupButtonView prefabs. Assign this prefab to PopupService.
3) Ensure exactly one PopupService is active in the scene so callers resolving IPopupService succeed (drag-drop reference or rely on auto-discovery).

Usage Pattern
- Build popup data with PopupRequest; use CreateConfirmation(tableId, titleKey, bodyKey, confirmKey, cancelKey, args) for standard dialogs.
- Each PopupOptionDefinition needs localized strings (EN/FR) in the UI string tables; pass additional arguments through PopupRequest.CreateLocalized where needed.
- Call IPopupService.RequestPopup(request, result => { ... }); inspect result.OptionId against PopupOptionIds constants (Confirm/Cancel/Ok).
- PopupService queues requests; only one popup is shown at a time, later requests wait until the active one completes.

Localization & Assets
- Add string entries to UI.Common (and preload via LocalizationPreloadTask) for all popup text.
- If popups need icons or SFX, resolve them via IUiAssetProvider in PopupView/PopupService instead of direct Addressables lookups.

Extending
- Customize popup styling via alternate PopupView prefabs or wrapper services that assemble PopupRequest instances.
- Keep logic in Game.UI and depend on IPopupService to avoid assembly cycles or duplicated UI logic.

Testing
- PopupRequestTests (Assets/Game/Scripts/Tests/EditMode/UI) cover helper factories. Add similar focused tests when extending options/result handling.

Pitfalls
- Do not duplicate popup logic; reuse PopupService/IPopupService so behavior stays consistent.
- Ensure the string table holds EN/FR entries before shipping; missing keys surface as empty labels.
- Trigger audio or gameplay effects only after the callback confirms the chosen option (e.g., when OptionId == PopupOptionIds.Confirm).

-------------------------------------
World Time - Reuse
-------------------------------------

Files
- Service: Assets/Game/Scripts/Systems/WorldTimeService.cs (Game.Core) manages day/week/month rollover via WorldTimeCounter.
- Contracts: Assets/Game/Scripts/UI/WorldTime/IWorldTimeService.cs and WorldDate.cs (Game.UI) expose the shared API.
- UI binding: Assets/Game/Scripts/UI/WorldTimeHudView.cs updates TextMeshPro values and localized labels.
- Tests: Assets/Game/Scripts/Tests/EditMode/Systems/WorldTimeCounterTests.cs covers rollover rules.

Goal

Track the in-game calendar (day 1/week 1/month 1 start), incrementing each End Turn: 7 days per week, 4 weeks per month. Surface current values in UI and let gameplay systems react to DateChanged events.

Scene Wiring

1) Place exactly one WorldTimeService in the world map scene. Configure DaysPerWeek, WeeksPerMonth, and starting date.
2) WorldMapTurnController auto-discovers WorldTimeService (or wire _timeServiceBehaviour) and calls AdvanceDay() after hero cleanup.
3) Add WorldTimeHudView to the calendar HUD, assign TextMeshPro fields for day/week/month numbers and labels, and reference the same WorldTimeService (optional; auto-discovery works when a single implementation exists).
4) Ensure localization keys WorldTime.DayLabel / WeekLabel / MonthLabel exist in UI.Common with EN/FR entries.

Extending / Reuse

- Any system needing time progression should depend on IWorldTimeService (event DateChanged, property CurrentDate) rather than the concrete MonoBehaviour.
- To skip ahead or reset (e.g., time travel, debugging), call ResetTo(WorldDate) or repeated AdvanceDay().
- For save/load, serialize WorldDate (Day/Week/Month) and reapply via ResetTo on load before enabling listeners.
-------------------------------------
Resource Nodes — Reuse
-------------------------------------

Files
- Definitions: Assets/Game/Scripts/Map/Resources/ResourceDefinition.cs (ScriptableObject with localized name/description, sprite variants, random selection metadata)
- Runtime registry: Assets/Game/Scripts/Map/Resources/ResourceNodeService.cs (MonoBehaviour implementing IResourceNodeProvider)
- Scene authoring: Assets/Game/Scripts/Map/ResourceNodeAuthoring.cs (component on each resource GameObject, handles variant selection, grid snapping, registration)
- Tests: Assets/Game/Scripts/Tests/EditMode/Systems/Resources/ResourceNodeServiceTests.cs (register/update/unregister coverage)

Authoring Flow
1) Data: create a ResourceDefinition asset under Assets/Game/Content/ScriptableObjects/Resources, set Resource Id (e.g., resource.gold), wire LocalizedString entries (Gameplay.Resources table) with EN/FR text, and configure Map Visual variants with Sprite sub-asset keys + shared local offsets.
2) Scene services: ensure exactly one ResourceNodeService exists in the world scene; keep _logCoordinateConflicts enabled while authoring. Reference the same Grid/TilemapTileDataProvider used by movement so grid coords align.
3) Placement: drop a GameObject with SpriteRenderer + ResourceNodeAuthoring, assign Resource Definition, Base Yield, Grid, TilemapTileDataProvider. Leave Node Id empty for GUID auto-generation or set a stable id when needed. Choose Variant mode: Specific (provide Variant Id) or Random (optional Random Seed for deterministic picks). Manual Offset provides per-instance tweaks on top of the definition’s LocalOffset.
4) Prefab: when consistent, convert configured nodes into prefabs (Assets/Game/Content/Prefabs/WorldMap/Resources/) so designers drag-and-drop stacks without reassigning fields.

Runtime Usage
- ResourceNodeAuthoring registers with ResourceNodeService on enable, providing NodeId, GridCoord (if snapped), BaseYield, and chosen variant. Consumers (economy, UI) should depend on IResourceNodeProvider to query Nodes or subscribe to NodeRegistered/Updated/Unregistered.
- Keep Addressables handles warm by adding portrait/icon sprite keys to AddressablesLoadKeysTask when nodes must display instantly.
- Localization preloads: ensure LocalizationPreloadTask includes Gameplay.Resources to avoid first-use string fetch delays.

Best Practices
- Store shared offsets in ResourceDefinition variants; use Manual Offset only for unique scene adjustments.
- Avoid duplicate NodeIds; conflicts log warnings when _logCoordinateConflicts is true. If hand-authoring ids, prefer a faction/region prefix (e.g., gold.surface.01).
- When runtime spawning/despawning nodes, call ResourceNodeService.RegisterOrUpdate / Unregister and consider pairing with PreloadRegistry for asset lifetime.
- Extend tests alongside new service behaviors (e.g., coordinate collision handling, filtering APIs) to keep regression coverage tight.

Pitfalls
- Missing Grid or TilemapTileDataProvider references prevent snapping; the node still registers but lacks GridCoord (pathfinding blockers won’t see it).
- Sprite Addressable keys should target Sprite sub-assets; pointing to Texture2D main assets breaks SpriteRenderer assignment.
- Forgetting to preload Localization table causes one-frame empty labels when UI first queries resource names.

