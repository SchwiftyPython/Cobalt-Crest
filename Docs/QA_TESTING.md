# QA & Testing — Phase 1

## Scenarios
1. **Startup:** Open MainScene → Play. Expect producers, herbivores, carnivores; no Console errors.
2. **Dynamics:** Let run for 2–3 minutes. Populations should oscillate without total collapse (given defaults).
3. **Time Controls:** Pause/Play/Fast modifies simulation visibly.
4. **Graph:** Lines follow populations; auto-scale adapts to spikes.
5. **Stress:** Increase initial counts in `Spawner` (constants). Verify 60 FPS on dev hardware.

## Troubleshooting
- **Nothing spawns:** Ensure scripts compiled; try menu *Ecosphere → Setup Phase 1* (Editor script).
- **Graph not drawing:** Check that EcosystemManager exists; graph will early-exit if null/config missing.
- **Performance dips:** Reduce counts; increase seek interval; enable simplified behaviors.
