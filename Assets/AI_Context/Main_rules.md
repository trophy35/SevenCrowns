# Unity 6 Best Practices — SOLID, Memory & Performance (SevenCrowns)

> Scope: Unity 6 LTS (6000.0.39f1). Applies to SevenCrowns’ architecture and coding standards. All comments in English. Follow Unity’s C# style guide (PascalCase for types/methods, camelCase for fields with \_prefix for private, ALL_CAPS for constants).

---

## 1) Architectural Principles (SOLID-first)

**Single Responsibility:**

* Each MonoBehaviour should do one thing well: input, presentation, domain logic, or data access — not several.
* Extract non-Unity logic into plain C# services/POCOs to enable tests and reuse.

**Open/Closed:**

* Prefer composition + interfaces over switch/case or enums branching. Add behavior via new classes, not edits.
* Drive feature toggles with ScriptableObject configs instead of hardcoded flags.

**Liskov Substitution:**

* Favor small, capability-focused interfaces (e.g., `IDamageable`, `IHealable`).
* Avoid inheritance chains on MonoBehaviours; use components and data to extend behavior safely.

**Interface Segregation:**

* Split fat interfaces into narrow ones. Never force consumers to depend on methods they don’t use.

**Dependency Inversion:**

* Depend on abstractions (interfaces) and inject concrete implementations at composition roots (Bootstrap/Installers).
* Keep Unity dependencies at the edges; core domain should be engine-agnostic.

**Assemblies/Modules:**

* Use asmdefs to create feature- and layer-based modules.
* Enforce minimal references; no circular deps. Treat modules as packages.

---

## 4) Memory Optimization

**General:**

* Treat memory as a budget. Track peak RAM/VRAM and GC spikes per scene and per feature.
* Avoid per-frame allocations. Watch for: LINQ in Update, string concatenation, boxing (structs in `object`), hidden `foreach` allocations on non-arrays.

**Assets:**

* Prefer **Sprite Atlases** and **Texture compression**; trim import sizes; limit readable CPU copies.
* Audio: stream long music from disk; compress voice/SFX appropriately.

**Objects & Pools:**

* Pool transient GameObjects/FX/projectiles/UI elements. Avoid `Instantiate/Destroy` bursts.
* Use object lifetime owners: the spawner or the scope controller releases pooled items.

**ScriptableObjects:**

* Use for configs and shared read-only data (no runtime mutation unless explicitly designed). Keeps scenes slim.

**Serialization:**

* Keep serialized fields minimal and explicit. Prefer IDs/references over deep object graphs.

**GC Control:**

* Cache component lookups; avoid `GetComponent` in hot paths.
* Reuse `List<>`, `StringBuilder`, and temp buffers; clear, don’t reallocate.

---

## 5) Performance Guidelines

**Profiling Workflow:**

* Use **Profiler**, **Profile Analyzer**, **Memory Profiler**, **Frame Debugger** early and often; capture on target hardware.
* Record baselines per feature; regressions over thresholds fail code review.

**Update Loop:**

* Minimize `Update` count: consolidate via tick services or event-driven patterns.
* Use `FixedUpdate` only for physics; keep it lightweight. Time-scale all movement with `deltaTime`.
* Prefer `OnEnable`/`OnDisable` for event wiring; avoid reflection per frame.

**Rendering:**

* Enable **SRP Batcher**; group materials; minimize material variants.
* Reduce draw calls: GPU instancing, dynamic/static batching, mesh merging where appropriate.
* Use LODs, culling (frustum/occlusion), and lightmapping. Limit real-time lights and shadow casters.
* VFX: shared particle materials, pooled systems, truncated lifetimes.

**Physics:**

* Keep collision layers tight; avoid mesh colliders for moving objects.
* Use continuous collision only where needed; limit rigidbody count and joint complexity.

**UI:**

* Split Canvases to limit rebuild scope; avoid per-frame layout changes and animated LayoutGroups.
* Preload localized tables; defer heavy `TMP` operations off hot paths.

**Async/Tasks:**

* Use coroutines or async patterns for I/O and loading; never block main thread.
* Sequence Addressables loads to avoid spikes; stagger chunk loads.

---

## 6) Testing, Observability & Safety Nets

* Unit-test domain services (no Unity deps) and add PlayMode tests for integration.
* Guardrails: assertions in dev builds, clear error logs (no silent failures), feature flags via ScriptableObjects.
* Telemetry hooks (frame time, spikes, memory) for long sessions; surface warnings in debug HUD.

---

## 7) Code Style & Reviews

* Follow Unity’s official C# style guide strictly.
* Keep methods short; prefer early returns; document non-obvious logic.
* Reviews must check: SOLID adherence, allocations, Addressables release paths, scene/asset lifetime, localization usage, asmdef deps.

---

## 8) Practical Checklists

**Feature PR Checklist (excerpt):**

* SOLID: single responsibility, interfaces over inheritance.
  * [ ] No per-frame allocations; Profiler snapshot attached.
  * [ ] Addressables: loaded by label; released on scope exit.
  * [ ] Pools for transient objects.
  * [ ] Localization: FR/EN keys; `LocalizedString` in UI.
  * [ ] Rendering: materials batched, LODs/culling configured.
  * [ ] Physics: layers filtered; no unnecessary mesh colliders.
  * [ ] Tests: unit/PlayMode where applicable.
  * [ ] Docs: update `SceneFlow.md`/`Folders.md` references if impacted.

**Performance Budget Hints:**

* CPU frame: keep headroom (e.g., \< 12 ms on target), no \>2 ms spikes in steady state.
* Draw calls/material sets bounded per scene type (agree per-vertical).
* Peak RAM/VRAM documented per scene; deltas tracked per PR.

---

## 9) Anti-Patterns to Avoid

* God-classes MonoBehaviours, deep inheritance on components.
* Feature flags hardcoded in code; magic numbers.
* `Resources.Load` for runtime content (use Addressables).
* Instantiating UI/FX per event without pooling.
* LINQ/allocations inside `Update`/`LateUpdate`/`FixedUpdate`.
* Forgetting to **Release** Addressables handles/labels.
* Hardcoded UI strings; skipping FR/EN keys.