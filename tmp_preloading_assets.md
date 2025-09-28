# **Overview**

* Purpose: Preload key assets at boot, cache handles for instant access, show progress with accurate weighting, and transition cleanly to the next scene.
* Core pieces: preload task pipeline, weighted progress, Addressables-based loading, central cache, UI wiring, and scene flow.

# **Key Components**

* Base Task: Assets/Game/Scripts/Systems/BasePreloadTask.cs:12
  * Abstract ScriptableObject with Id, DisplayName, Weight, and Run(Action\<float\>).
* Runtime Weight: Assets/Game/Scripts/Systems/IRuntimeWeightedTask.cs:1
  * Optional interface to override weight at runtime.
* Registry Cache: Assets/Game/Scripts/Systems/PreloadRegistry.cs:1
  * Static key→handle store with Register, TryGet\<T\>, Release, ReleaseAll.
* Boot Orchestration: Assets/Game/Scripts/Systems/BootLoaderController.cs:19
  * Runs tasks in order, aggregates weighted progress, enforces minimum screen time, waits for input, plays SFX, then fades to next scene.
* Example Task (keys): Assets/Game/Scripts/Systems/AddressablesLoadKeysTask.cs
  * Loads a list of Addressables keys in parallel; progress = mean of handle PercentComplete.
* Localization Task: Assets/Game/Scripts/Systems/LocalizationPreloadTask.cs
  * Initializes LocalizationSettings, selects locale, preloads string/asset tables.

# **Workflow**

* Author ScriptableObject tasks (derived from BasePreloadTask).
* Add task assets to BootLoaderController.\_tasks in the Boot scene, ordered logically.
* Each task reports 0..1 progress via callback; Boot maps per-task progress to overall based on weight/runtime weight.
* Preloaded Addressables are registered into PreloadRegistry so they stay alive and can be looked up by key.
* After all tasks: show “Press any key”, play SFX from cache, fade to MainMenu via Scene Flow.

# **Adding A New Preload Task**

* Create a ScriptableObject derived from BasePreloadTask.
* Implement IEnumerator Run(Action\<float\> reportProgress):
  * Kick off async loads (Addressables or other).
  * Poll and compute progress; call reportProgress(p) regularly.
  * Always call reportProgress(1f) before finishing.
* If task size is known only at runtime, implement IRuntimeWeightedTask.GetRuntimeWeight() and return a meaningful weight (e.g., number of items).
* Save the asset under Assets/Game/Content/ScriptableObjects/Config/Boot/PreloadTasks/ and add it to BootLoaderController.\_tasks.

# **Addressables & Defines**

* Ensure Addressables package is installed and assets are marked Addressable with stable keys.
* Defines:
  * ADDRESSABLES is auto-added via asmdef version define in Game.Core (Addressables code compiled-in when the package exists).
  * UNITY_LOCALIZATION is defined when the Localization package exists.
* Play Mode (to see real progress):
  * Addressables Groups → Play Mode Script: Use Existing Build (or Packed).
  * Build Addressables via Build \> New Build.

# **Using The Cache (PreloadRegistry)**

* Register: after starting a load, call PreloadRegistry.Register(key, handle) once per key.
* Lookup: if (PreloadRegistry.TryGet\<AudioClip\>("SFX/click", out var clip)) \_audio.PlayOneShot(clip);
* Release:
  * Release by key: PreloadRegistry.Release(key)
  * Release all: PreloadRegistry.ReleaseAll() (e.g., at scene/lifetime changes if you don’t need preloads anymore).

# **Localization**

* Preload common tables (e.g., Boot, UI.Common) in LocalizationPreloadTask.
* After preload, fetch localized strings by table/key:
  * Async (safe): GetLocalizedStringAsync("UI.Common","PressAnyKey") and yield until done.
  * Synchronous (only if preloaded): StringDatabase.GetTable("UI.Common").GetEntry("PressAnyKey").LocalizedValue.
* Prefer component LocalizeStringEvent on static labels for auto-updates; use code for dynamic text.

# **Boot UI & Progress**

* UI component: Assets/Game/Scripts/UI/UiProgressBar.cs
  * SetSmooth(value) for tweened updates; SetImmediate(value) for snaps.
  * Optional numeric label and a debug override for manual testing.
* BootLoaderController sets \_statusText (task.DisplayName key) and updates \_progressBar during loads.

# **Scene Flow**

* Config + Fader + Controller implement a fade-out/load/fade-in transition.
* Files:
  * Config: Assets/Game/Scripts/Systems/SceneFlowConfig.cs
  * Fader: Assets/Game/Scripts/Systems/SceneTransitionFader.cs
  * Controller: Assets/Game/Scripts/Systems/SceneFlowController.cs
* Boot calls SceneFlowController.GoToBySceneName(\_nextSceneName) after input.

# **Memory & Release Strategy**

* Registry holds Addressables handles to prevent unload. Keep keys minimal and scoped.
* Release as you enter new lifetimes (e.g., world/combat) to reclaim memory:
  * Use LifetimeContentService for label-scoped loading/unloading if you need broader lifetime management.
* Avoid double-registering valid handles for the same key (registry keeps first valid).

# **Testing & Debug**

* UiProgressBar:
  * Toggle \_debugOverride and move \_debugValue during Play Mode to validate visuals.
* Simulated task:
  * Add a simple “Wait” preload task that increments progress across 2–3 seconds to test the bar independent of Addressables.
* Logging:
  * Print task start/end and key counts while integrating new tasks for clarity.

# **Common Issues**

* Progress stuck at 100%:
  * No tasks assigned; Addressables path compiled out; tiny loads; Image not assigned; Image not set to Filled/Horizontal.
* Localization not showing:
  * Wrong table/key; async not awaited; table not preloaded; package/define missing.
* SFX not playing:
  * Key mismatch; clip not addressable; registry didn’t register; \_pressAnyKeyVolume set to 0.

# **Conventions**

* Keys: use hierarchical strings like Audio/SFX/UI/click, UI/AtlasMain.
* Asset placement:
  * Scripts: Assets/Game/Scripts/Systems/…
  * Task SOs: Assets/Game/Content/ScriptableObjects/Config/Boot/PreloadTasks/
  * Localization tables: Assets/Game/Content/Localization/StringTables/…
* Keep weights roughly proportional to wall-clock time to give stable UX.

# **Quick Recipe: Add A New Cached Asset**

* Mark asset as Addressable with key X/Y/Z.
* Add key to an AddressablesLoadKeysTask asset, add asset to Boot tasks list.
* At runtime, call PreloadRegistry.TryGet\<T\>("X/Y/Z", out var obj) where needed.