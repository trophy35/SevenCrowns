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
- Follow SOLID principles and Unity’s official C# naming conventions.
- All comments must be in English.

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
