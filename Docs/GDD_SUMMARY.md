# GDD Summary (Phase 1 scope)

**Project:** Ecosphere — 2D sandbox ecosystem sim (top-down)  
**USP:** Emergent ecology, simple rules → complex behavior, evolutionary traits, living shelters (Phase 2+)  
**Phase 1 gameplay:** producers → herbivores → carnivores energy loop, environment drivers, time controls, population graph.

Source design highlights:
- Producers grow with rainfall/temperature; animals consume energy to move/reproduce (see full GDD).  
- Hybrid ECS/MB architecture planned; Phase 1 uses MonoBehaviours with clean seams for ECS later.  
- UI requires population graphs and time controls.

This package implements:
- Producers/Herbivores/Carnivores with simple AI and energy economics.
- Environment driver (temperature + rainfall curves) influencing producer growth.
- Time controls (Pause/1x/5x) and **Population graph per species**.
- Self-bootstrapping scene creation (no prefabs required).

Out-of-scope (Phase 2+):
- Shelter construction (grid/soft-body hybrid) with pressure breathing
- Advanced evolution, LOS/pathfinding through shelters
- URP 2D lighting pass
