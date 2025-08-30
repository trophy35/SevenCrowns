# Assembly Definitions Strategy

This document explains how we structure **Assembly Definition Files (.asmdef)** in SevenCrowns to keep the codebase **modular, maintainable, and scalable** (SOLID-friendly), and to speed up compilation and testing.

## Goals

* **Clear boundaries** between domains (Boot, UI, Systems, Gameplay, etc.).
* **Stable dependencies** (no cycles, DIP applied).
* **Faster iteration** (smaller assemblies = faster recompilation).
* **Testability** (separate EditMode/PlayMode test assemblies).
* **Editor-only isolation** (Editor code never ships to runtime builds).

## Target Assemblies

> Names map 1:1 with folders and namespaces.

* **SevenCrowns.Core**\
  Cross-cutting helpers & primitives (extensions, small services, abstractions).\
  **Depends on:** _(none)_
* **SevenCrowns.Config**\
  ScriptableObjects, data models, static configs, (localization tables if not Addressables).\
  **Depends on:** Core
* **SevenCrowns.Systems**\
  Technical systems: Audio, Save/Load, Input abstraction, Localization runtime, SceneFlow.\
  **Depends on:** Core\
  **Optional:** `ADDRESSABLES` (via Version Define) if a system uses Addressables.
* **SevenCrowns.UI**\
  Reusable UI components (e.g., `UiProgressBar`, `BlinkCanvasGroup`, common widgets).\
  **Depends on:** Core, **Unity.TextMeshPro**
* **SevenCrowns.Boot**\
  Boot flow, preload tasks (`AddressablesLoadKeysTask`, `PreloadRegistry`, `BasePreloadTask`), “Press any key”.\
  **Depends on:** Core, Systems, UI, Config, **Unity.Addressables**, **Unity.ResourceManager**, **Unity.TextMeshPro**\
  **Version Define:** `ADDRESSABLES` enabled if package `com.unity.addressables` is present.
* **SevenCrowns.Gameplay**\
  Game logic (hero, map, battle, economy…).\
  **Depends on:** Core, Systems, Config\
  **Does NOT depend on:** UI, Boot
* **SevenCrowns.Editor** _(Editor only)_\
  Custom inspectors, menus, build scripts.\
  **Depends on:** the runtime assemblies it customizes (Core/UI/Systems/Config/Gameplay)\
  **Include Platforms:** Editor
* **SevenCrowns.Tests.EditMode** / **SevenCrowns.Tests.PlayMode**\
  Test assemblies that reference only what they test.\
  **OptionalUnityReferences:** TestAssemblies

## Folder Mapping (recommended)

`Assets/_Project/Scripts/
  Core/        -> SevenCrowns.Core.asmdef
  Config/      -> SevenCrowns.Config.asmdef
  Systems/     -> SevenCrowns.Systems.asmdef
  UI/          -> SevenCrowns.UI.asmdef
  Boot/        -> SevenCrowns.Boot.asmdef
  Gameplay/    -> SevenCrowns.Gameplay.asmdef
  Editor/      -> SevenCrowns.Editor.asmdef   (Editor-only)
Tests/
  EditMode/    -> SevenCrowns.Tests.EditMode.asmdef
  PlayMode/    -> SevenCrowns.Tests.PlayMode.asmdef
`

Namespaces mirror directories (e.g., `SevenCrowns.Boot`, `SevenCrowns.UI`).

## Dependency Graph (summary)

`Core     <- Config, Systems, UI, Boot, Gameplay
Config   <- Systems, Boot, Gameplay
Systems  <- Boot, Gameplay
UI       <- Boot
Boot     <- Core, Systems, UI, Config, Addressables, ResourceManager, TextMeshPro
Gameplay <- Core, Systems, Config
Editor   <- Core/UI/Systems/Config/Gameplay (Editor-only)
`

**No cycles.** Gameplay must not depend on UI or Boot.

## Addressables Setup

* In **SevenCrowns.Boot.asmdef** (and Systems if needed), add a **Version Define**:
  * Name: `Addressables`
  * Expression: `com.unity.addressables`
  * Define: `ADDRESSABLES`
* In code, guard usages with `#if ADDRESSABLES`.
* This avoids global Scripting Define Symbols and auto-enables if the package is installed.

## Input System

* If project uses **Input System (New)**, add reference **Unity.InputSystem** where needed and wrap with:

  `#if ENABLE_INPUT_SYSTEM
  using UnityEngine.InputSystem;
  #endif
  `
* Prefer **“Both”** only as a temporary fallback.

## Editor-Only Code

* All Editor scripts live under **SevenCrowns.Editor** with **Include Platforms = Editor**.
* Never reference `UnityEditor` from runtime assemblies.

## Adding a New Module

1. Create folder `Assets/_Project/Scripts/<ModuleName>/`.
2. Add `<ModuleName>.asmdef` named `SevenCrowns.<ModuleName>`.
3. Wire references **only** to the assemblies it truly needs.
4. Keep UI-free by default; if it needs UI, add a **view** in `SevenCrowns.UI` and interface-based access from your module (DIP).

## Common Pitfalls & Fixes

* **TMPro type not found** → add `Unity.TextMeshPro` reference in the assembly that directly uses TMP types.
* **Addressables code is grayed out** → ensure Version Define is set OR add `ADDRESSABLES` in Player Settings; build Addressables.
* **Circular dependency** → extract interfaces to **Core** or **Systems**; invert dependency.
* **Editor classes in runtime assembly** → move to `SevenCrowns.Editor`.

## Testing

* Put **unit tests** in `Tests/EditMode` (fast, pure C#).
* Put **integration/gameplay tests** in `Tests/PlayMode`.
* Each test asmdef references only the assemblies it tests.

## Style & Documentation

* **C# style**: Unity official naming guidelines.
* **Comments & tooltips**: **English only**.
* **SOLID**: single responsibility, interface segregation, dependency inversion everywhere feasible.

---

## FAQ

**Q: Can I keep creating tiny asmdefs per feature (e.g., per HUD screen)?**\
A: Prefer **module-level** assemblies (UI, Gameplay, Systems). Too many tiny assemblies add maintenance overhead and risk complex dependency graphs.

**Q: Where do I place ScriptableObject assets for Boot preload?**\
A: `Assets/_Project/_Settings/Boot/Preload/` (configuration assets), while the **code** lives in `Assets/_Project/Scripts/Boot/`.