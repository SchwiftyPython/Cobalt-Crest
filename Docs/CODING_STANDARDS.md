# Coding Standards & Conventions

- **C# version:** Unity default for chosen LTS.
- **Style:** PascalCase for public props/types, camelCase for privates (prefixed `_`), UPPER_SNAKE for consts.
- **Comments:** XML summaries for public classes/members; inline comments for tricky logic.
- **Null checks:** Guard all external references (UI, singletons, lists).
- **Time:** Multiply gameplay by `EcosystemManager.SimSpeed` (not by Time.timeScale).
- **Determinism:** Random seeded per entity at spawn (TODO in Phase 2); acceptable non-determinism in Phase 1.
- **Folders:** Assets/Scripts/{Managers,Entities,UI,Player,Utils,Bootstrap,Data}, Assets/Scenes.
