# SevenCrowns — Boot Pipeline (Addressables Keys)

This folder contains the minimal boot pipeline to preload game assets using **Addressables** with a simple rule: **1 asset = weight 1**.  
The progress bar stays smooth because each asset’s `PercentComplete` is averaged while loading.

## Files in this folder

- `BootLoaderController.cs` — Orchestrates the boot flow (progress, status text, “Press any key”, SFX, next scene).
- `PreloadRegistry.cs` — Central registry that keeps loaded Addressables handles in memory and allows reuse later.
- `BasePreloadTask.cs` — Abstract `ScriptableObject` base for preload tasks (`Run()` coroutine + display name + weight).
- `IRuntimeWeightedTask.cs` — Optional interface for tasks that compute their weight at runtime (e.g., number of keys).
- `AddressablesLoadKeysTask.cs` — Loads a list of Addressables keys in parallel, averages progress, and registers handles.

> All comments/tooltips in code are **English**, naming follows **Unity C# conventions**, and code is written with **SOLID** in mind.

## Prerequisites

1. Install **Addressables** via Package Manager.
2. Mark assets you want to preload as **Addressable** and give them **keys** (e.g., `SFX/click`, `UI/AtlasMain`).
3. Build Addressables content (Window → Asset Management → Addressables → Build).

## How to add assets to preload

1. Create a task asset:
   - Right click in Project window → **Create → SevenCrowns → Boot → Addressables Load Keys Task**
2. In the task inspector, add the exact keys you want to preload (e.g., `SFX/click`, `UI/AtlasMain`).
3. Open the **Boot** scene and assign this task to the `BootLoaderController` list of tasks.
   - If you also use `AddressablesInitTask` in the future, put it **before** the keys task.

## How progress is computed

- Each asset (key) contributes **equally** (weight = 1).
- The task starts all loads in **parallel** and reports the **average** of all `PercentComplete` values.
- The `BootLoaderController` aggregates tasks using **runtime weights** when available (via `IRuntimeWeightedTask`).

## How to reuse preloaded assets

- The boot task registers each loaded handle into **`PreloadRegistry`**.
- Later (e.g., in your UI code), fetch by key in **O(1)**:

```csharp
if (SevenCrowns.Boot.PreloadRegistry.TryGet<AudioClip>("SFX/click", out var clip))
{
    audioSource.PlayOneShot(clip);
}
If TryGet returns false, the asset was not preloaded or the key is wrong.