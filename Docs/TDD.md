# Technical Design Document (TDD) — Phase 1

## Architecture
- **Bootstrap:** RuntimeInitializeOnLoadMethod creates managers, UI canvas, time controls, graph, and initial spawns.
- **Managers:**
  - EcosystemManager: singleton, global simulation speed, per-species counts.
  - EnvironmentManager: temperature/rainfall curves; exposes environment multipliers.
- **Entities:**
  - Producer: biomass growth from environment; `Consume(amount)` API returns energy.
  - Herbivore: simple target-seek for nearest producer; energy drains, reproduces/dies by thresholds.
  - Carnivore: hunts nearest herbivore; gains energy on kill.
- **UI:**
  - PopulationGraph: RawImage-based line plot (ring buffer), series = Producers/Herbivores/Carnivores.
  - TimeControls: 3 buttons wired to EcosystemManager.SimulationSpeed.
- **Utils:**
  - SpriteFactory: programmatic sprites for colored discs/squares.
  - RNG helpers / clamps.

## Update Order
1. EnvironmentManager updates scalars.
2. Entities step using `dt = Time.deltaTime * EcosystemManager.SimSpeed`.
3. Graph samples counts on an interval using unscaled time.

## Data
- No ScriptableObjects required for Phase 1; constants live in each class (documented). Hook points are provided for replacing with configs later.

## Error Handling
- All `FindObjectOfType` calls guarded.
- No reliance on scene references; everything created at runtime.
- Counts updated in OnEnable/OnDisable; clamped to non-negative.

## Performance Notes
- Naive nearest-target scans throttled (every 0.25–0.5s) and with small populations are fine.
- For 1k+ entities, migrate seeking into a spatial hash (stub provided).

