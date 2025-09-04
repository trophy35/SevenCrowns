# 📁SevenCrowns Unity Project Folder Structure

`Assets/
  Game/
    Scripts/                      ← All C# source code
      Systems/                    ← Boot, preload, save, addressables, scene flow, localization
      Map/                        ← Grid, pathfinding, fog of war, movement, terrain
      City/                       ← City model, buildings, recruitment, defenses
      Combat/                     ← Battlefield grid, initiative, unit actions, damage resolution
      Hero/                       ← Hero stats, talents, skills, inventory, spellbook
      Magic/                      ← Schools, spells, mastery, casting
      Quests/                     ← Quest system, objectives, rewards
      Meta/                       ← Meta profile, Ether currency, unlocks
      AI/                         ← Strategic AI, combat AI, decision-making
      UI/                         ← UI logic scripts, view controllers, widgets
      Audio/                      ← Audio manager, mixer control, music crossfade logic
      Editor/                     ← Editor-only utilities, custom inspectors, validators
      Tests/                      ← Unit tests and play mode tests
      Game.Runtime.asmdef
      Game.Editor.asmdef
      Game.Tests.asmdef

    Content/                      ← All authored assets (data, prefabs, sprites, sounds…)
      ScriptableObjects/
        Resources/                ← Gold, Wood, Iron, Coal, Diamonds, Ether, Sulfur definitions
        Units/                    ← Peasant … Angel, tier I/II/III unit data
        Buildings/                ← Farms, Market, Barracks, Towers, Walls…
        Spells/                   ← Schools + spell definitions
        Artifacts/                ← Artifact definitions
        Quests/                   ← Primary/secondary quests
        AI/                       ← Neutral guardian presets, AI tuning data
        Config/                   ← Constants, settings (movement cost, initiative bands…)
        Balance/                  ← XP curves, unit/building balance tables

      Prefabs/
        Units/                    ← Prefabs for unit stacks
        Heroes/                   ← Hero prefab(s)
        UI/                       ← UI prefabs (tooltip, progress bar, HUD panels…)
        Systems/                  ← System prefabs (AudioManager, SceneFlowFader…)
        Environment/              ← Map props (trees, rocks…)
        VFX/                      ← VFX prefabs (explosions, spell effects)

      UI/
        Screens/
          Boot/                   ← Loader screen assets (backgrounds, progress bar layout…)
          MainMenu/
          WorldMapHUD/
          CityScreen/
          CombatHUD/
          Results/
        Common/                   ← Shared UI widgets (buttons, tooltip, inventory slots…)
        Fonts/                    ← TMP font assets
        Styles/                   ← UI themes, colors, TMP style sheets

      Art/
        Sprites/
          Environment/            ← In-game environment sprites (tiles, resources piles, terrain)
          Characters/             ← Unit, hero, monster spritesheets
          Icons/                  ← Resource icons, skill icons, artifact icons
        Textures/                 ← High-res textures for 3D or UI
        Materials/                ← Materials for sprites, terrain, effects
        Shaders/                  ← ShaderGraph / custom shaders
        Animations/
          Controllers/            ← Animator controllers
          Clips/                  ← Animation clips

      Audio/
        Music/                    ← World, city, combat tracks
        SFX/
          UI/                     ← Clicks, error buzz, hover ticks
          World/                  ← Ambient, footsteps, pickup
          City/                   ← Construction, marketplace, tavern
          Combat/                 ← Attacks, spells, unit deaths, victory/defeat

      VFX/
        Graphs/                   ← Shader Graphs or Visual Effect Graphs
        Textures/                 ← VFX textures
        Prefabs/                  ← Reusable particle effect prefabs

      Localization/
        StringTables/
          UI/                     ← Translations for UI keys
          Gameplay/                ← Translations for gameplay-related texts

      Scenes/
        Boot.unity
        MainMenu.unity
        WorldMap.unity
        City.unity
        Combat.unity
        Results.unity
        _Dev/                     ← Sandbox/dev-only scenes (excluded from build)

      Addressables/               ← Optional entry assets for grouping Addressables

    Settings/                     ← Project settings assets (URP, Input System, Audio Mixer, Physics)
  External/
    Plugins/                      ← Third-party packages, SDKs
`

---

✅ **Key rules to remember**

* **All C# code → `Scripts/`** (structured by domain).
* **All data (ScriptableObjects) → `Content/ScriptableObjects/`**.
* **All in-game sprites → `Content/Art/Sprites/…`**.
* **All UI sprites (icons, backgrounds) → `Content/UI/…`**.
* **All prefabs → `Content/Prefabs/…`**.
* **All audio → `Content/Audio/…`**.
* **All effects → `Content/VFX/…`**.
* **All localization → `Content/Localization/…`**.
* **All scenes → `Content/Scenes/`**.