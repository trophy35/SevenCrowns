SevenCrowns – Agent Guidelines

-------------------------------
Purpose

You are generating code for the SevenCrowns project (Unity 6 LTS).
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

