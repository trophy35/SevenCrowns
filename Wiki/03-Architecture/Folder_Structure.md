# 📁 Unity Project Structure Documentation (SevenCrowns)

This document defines the **official folder layout** for the project and what types of files each folder should contain.\
All team members and automation tools (e.g. AI code generation, CI) should respect this structure.

---

## Root: `Assets/_Project/`

Contains **all custom project content** (code, art, data, prefabs, scenes).\
External plugins live in `Assets/ThirdParty`.

---

### `_Settings/`

Global project-wide configuration and settings assets.

* `URP/` → Universal Render Pipeline assets (Pipeline.asset, Renderer2D.asset).
* `Input/` → Input System `.inputactions` assets.
* `Addressables/` → Addressable settings, profiles, groups.

---

### `_Shared/`

Content shared across gameplay features.

#### `Scripts/Core/`

* **Runtime/** → Core services (event bus, service locator, config, time, save manager base).
* **Editor/** → Editor utilities for Core (custom inspectors, menus).
* **Tests/** → Unit tests for Core logic.

#### `Scripts/Utilities/`

* **Runtime/** → General-purpose utilities (math helpers, extensions, pooling, FSM, pathfinding helpers).
* **Editor/** → Utility editor scripts (importers, tools).
* **Tests/** → Tests for utilities.

#### `Art/`

Shared art (fonts, icons, shared materials, shaders).

#### `Audio/`

Shared music/SFX not tied to a single gameplay feature.

#### `UI/`

* `Common/` → Shared UI prefabs/components (tooltip, button, frame).
* `Theme/` → Spritesheets, atlases, 9-slice frames.

#### `Prefabs/`

* `Common/` → Shared prefabs (generic markers, camera rigs, lights).

---

### `Gameplay/`

All game feature logic (map, battle, town, economy, AI, quests).

#### `Data/`

Static design data as **ScriptableObjects**.

* `Creatures/` → Creature definitions (stats, icons, prefabs).
* `Spells/` → Spell definitions (mana cost, targeting).
* `Artifacts/` → Artifact definitions.
* `Factions/` → Faction/hero class definitions.
* `Terrains/` → Terrain tile definitions (movement costs, visuals).
* `Buildings/` → Town building definitions.
* `Balance/` → Balance tables (growth rates, costs).

#### `Map/`

Overworld (adventure map).

* `Scripts/Runtime/` → Map grid, tilemap, movement, pathfinding, POIs.
* `Scripts/Editor/` → Map editing tools.
* `Scripts/Tests/` → Unit tests for map logic.
* `Prefabs/` → Map objects (resources, mines, heroes).
* `Tilemaps/` → Tilemap assets for the map.
* `Sprites/` → Map-specific sprites.

#### `Battle/`

Turn-based combat system.

* `Scripts/Runtime/` → Hex grid, initiative, actions, abilities.
* `Scripts/Editor/` → Combat dev tools.
* `Scripts/Tests/` → Battle system tests.
* `Prefabs/` → Combat prefabs (units, grid cells).
* `Sprites/` → Battle-specific art.

#### `Town/`

Town screen & buildings.

* `Scripts/Runtime/` → Town UI controller, building logic.
* `Scripts/Editor/` → Town dev tools.
* `Scripts/Tests/` → Town tests.
* `Prefabs/` → Town prefabs (UI layout, building slots).
* `Sprites/` → Town UI art.

#### `Economy/`

Economy system.

* `Scripts/Runtime/` → Resources, income per day/week.
* `Scripts/Editor/` → Debug economy editor tools.
* `Scripts/Tests/` → Economy tests.

#### `AI/`

Game AI.

* `Scripts/Runtime/` → Pathfinding, decision-making, heuristics.
* `Scripts/Editor/` → Debug AI tooling.
* `Scripts/Tests/` → AI unit tests.

#### `Quests/`

Quest and event system.

* `Scripts/Runtime/` → Quest definitions, triggers, rewards.
* `Scripts/Editor/` → Quest editing tools.
* `Scripts/Tests/` → Quest system tests.

---

### `UI/`

UI layer of the game.

#### `Screens/`

Each screen has its own folder:

* `MainMenu/` → Main Menu screen prefabs & scripts.
* `MapHUD/` → HUD during adventure map.
* `BattleHUD/` → Combat HUD.
* `TownHUD/` → Town screen HUD.
* `Popups/` → Shared popup windows (dialogs, message boxes).

#### `Widgets/`

Reusable UI components (tooltip, resource bar, minimap widget).

#### `Prefabs/`

UI prefabs (menus, windows).

#### `Sprites/`

UI spritesheets and atlases.

---

### `Systems/`

Cross-cutting systems (independent of gameplay).

#### `SceneFlow/`

Scene management & bootstrap.

* `Scripts/Runtime/` → SceneLoader, Bootstrapper.
* `Scripts/Editor/` → Editor helpers.
* `Scripts/Tests/` → Scene loading tests.

#### `SaveSystem/`

Save/load system.

* `Scripts/Runtime/` → Serialization logic.
* `Scripts/Editor/` → Save debugging.
* `Scripts/Tests/` → Save tests.

#### `AudioSystem/`

Central audio manager.

* `Scripts/Runtime/` → Music controller, SFX system.
* `Scripts/Editor/` → Audio debugging.
* `Scripts/Tests/` → Audio system tests.

#### `LocalizationSystem/`

Localization data.

* `Scripts/Runtime/` → Text lookup.
* `Scripts/Editor/` → Localization importers.
* `Scripts/Tests/` → Localization tests.

---

### `Scenes/`

* `Boot.unity` → First scene (loads MainMenu).
* `MainMenu.unity` → Main Menu.
* `Map.unity` → Adventure map.
* `Battle.unity` → Battle screen.
* `Town.unity` → Town screen.
* `_Dev/` → Temporary development/test scenes (never in build).

---

### `Tests/`

Top-level project tests.

* `PlayMode/` → PlayMode test suites.
* `EditMode/` → EditMode test suites.

---

### `ThirdParty/`

External assets/plugins (kept separate from `_Project`).\
Examples: Cinemachine, Odin Inspector, DOTween.