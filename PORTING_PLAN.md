# Porting Plan: Godot ➜ Unity (3D Isometric Tactical RPG)

## Context and Vision
This repository contains an early, abandoned Godot project for a turn-based isometric RPG.
The goal is to port the game to Unity while evolving it into a **true 3D isometric tactical game** with vertical gameplay.

This is not only an engine migration. It is also a gameplay and architecture redesign:
- Preserve the original game ideas and logic intent.
- Rebuild the codebase with a cleaner, maintainable structure.
- Prepare for a long-term MMO direction (without implementing MMO now).

---

## Confirmed Product Direction

### 1) Engine Migration
- Source reference: Godot project.
- Target implementation: Unity project.

### 2) World and Combat Dimension Upgrade
Move from 2-axis logic to 3-axis logic:
- Add verticality (`X/Y/Z` or grid equivalent).
- Support high-ground mechanics (height advantage).
- Support reach/line-of-sight constraints from elevation.
- Allow unreachable targets due to cliffs/floors/obstacles/jump limits.

### 3) Architecture Strategy
- Keep design intent.
- Do **not** do a 1:1 script translation from Godot to Unity.
- Refactor into cleaner gameplay systems and class boundaries.

---

## Repository/Branch Working Model
To ease migration and comparison, keep both source and target accessible together:
- `godot/` (reference/original project)
- `unity/` (new implementation)

This enables side-by-side checks for:
- scene/level structure
- gameplay behavior parity
- data and naming conventions

> Note: if current branch folders are named differently (e.g. `One Piece Tactics/` and `PES_Unity/`), keep an explicit mapping in docs/scripts to avoid confusion.

---

## Technical Principles for the Unity Port

### Core Principle: Separate Rules from Presentation
Keep gameplay simulation independent from visuals/UI where possible.

Recommended module boundaries:
- `Core/TurnSystem`
- `Core/Grid3D`
- `Combat/Abilities`
- `Units/Stats`
- `AI/Decision`
- `Presentation/VFX/UI`

### Action-Driven Simulation
Use explicit gameplay commands/actions (e.g. `Move`, `Attack`, `CastSkill`) rather than direct ad-hoc state mutation.

### Determinism and Reproducibility
- Deterministic rule resolution where feasible.
- Centralized RNG service with explicit seeding.
- Clear execution flow: input ➜ rule resolution ➜ resulting state.

### Unity-Specific Cleanliness
Avoid coupling all core logic directly to `MonoBehaviour`.
Keep domain logic testable and portable.

---

## Recommended Unity Folder Architecture (Best-of-Both)

> Goal: use a standard Unity folder layout while preserving clean architecture constraints.

Inside `PES_Unity/Assets/`:

```txt
_Project/
  Core/
    Simulation/
    TurnSystem/
    Random/
  Grid/
    Grid3D/
    Pathfinding/
    Visibility/
  Combat/
    Actions/
    Targeting/
    Resolution/
  Units/
    Domain/
    Authoring/
  AI/
    Decision/
  Infrastructure/
    Serialization/
    Eventing/
  Presentation/
    Scene/
    View/
    UI/
    VFX/
    Adapters/
  Tests/
    EditMode/
    PlayMode/
```

### Dependency Rules (must keep)
- `Presentation` can depend on domain modules (`Core`, `Grid`, `Combat`, `Units`).
- Domain modules must **never** depend on `Presentation`.
- `Infrastructure` may adapt persistence/logging concerns without polluting gameplay rules.

---

## Concrete Bootstrap (Asmdefs + Namespaces + Starter Files)

### Asmdef names
Create these assembly definitions under `Assets/_Project/`:
- `PES.Core`
- `PES.Grid`
- `PES.Combat`
- `PES.Units`
- `PES.AI`
- `PES.Infrastructure`
- `PES.Presentation`
- `PES.Tests.EditMode`
- `PES.Tests.PlayMode`

### Asmdef dependency direction
- `PES.Core`: no dependency on presentation.
- `PES.Grid`: depends on `PES.Core`.
- `PES.Units`: depends on `PES.Core`.
- `PES.Combat`: depends on `PES.Core`, `PES.Grid`, `PES.Units`.
- `PES.AI`: depends on `PES.Core`, `PES.Combat`, `PES.Grid`, `PES.Units`.
- `PES.Infrastructure`: depends on `PES.Core` (and optionally others for serialization adapters).
- `PES.Presentation`: depends on all gameplay/domain assemblies as needed.
- Test assemblies depend on the assemblies they validate.

### Namespace convention
- `PES.Core.*`
- `PES.Grid.*`
- `PES.Combat.*`
- `PES.Units.*`
- `PES.AI.*`
- `PES.Infrastructure.*`
- `PES.Presentation.*`

### 10 starter files (minimum skeleton)
1. `Assets/_Project/Core/Simulation/BattleState.cs`
2. `Assets/_Project/Core/Simulation/EntityId.cs`
3. `Assets/_Project/Core/Simulation/IActionCommand.cs`
4. `Assets/_Project/Core/Simulation/ActionResolver.cs`
5. `Assets/_Project/Core/Random/IRngService.cs`
6. `Assets/_Project/Core/Random/SeededRngService.cs`
7. `Assets/_Project/Grid/Grid3D/GridCoord3.cs`
8. `Assets/_Project/Grid/Pathfinding/PathfindingService.cs`
9. `Assets/_Project/Combat/Actions/MoveAction.cs`
10. `Assets/_Project/Combat/Actions/BasicAttackAction.cs`

> Optional right after bootstrap: add `CombatEventLog.cs` and one EditMode test per action to validate deterministic behavior.

---

## Long-Term Multiplayer/MMO Direction

### Decision
Do **not** build full multiplayer/MMO now.
Design the architecture so multiplayer can be added later with lower rewrite cost.

### Build Now (MMO-Compatible Foundations)
- Stable entity/player identifiers.
- Serializable combat/game-state snapshots.
- Action/event logs (who did what, when, and result).
- Clear separation between:
  - input collection
  - game rule resolution
  - state replication/presentation

### Postpone for Later
- Networking stack and transport.
- Dedicated authoritative server infra.
- Accounts, persistence, sharding, social systems (guild/chat/etc.).

---

## Immediate Next Steps
1. Create `_Project` folder structure + asmdefs.
2. Add the 10 starter files above with compile-safe stubs.
3. Implement `MoveAction` validation + resolution and test it in EditMode.
4. Implement `BasicAttackAction` with LOS/range/height modifier + tests.
5. Build one minimal vertical slice scene (2 units + stepped map + move/attack loop).

---


## Progress Tracking (Current Branch)
- [x] 1) `_Project` folder structure + asmdefs created.
- [x] 2) 10 starter files added with compile-safe stubs/evolutions.
- [x] 3) `MoveAction` validation + resolution implemented with EditMode tests.
- [x] 4) `BasicAttackAction` range/LOS/height modifier implemented with EditMode tests.
- [x] 5) First minimal vertical slice bootstrap added (2 units + stepped map + move/attack loop).

## Validation Status
- [x] Vertical slice scene (`VerticalSlice_BattleLoop`) validated manually on the current branch.
- [x] Unity Test Runner suite executed successfully on the current branch.
- [x] Add a short regression checklist (move, attack, LOS blocked, height bonus, turn advance) to standardize future validation sessions.

## Next Iteration Focus
1. Extend test coverage on edge cases (blocked LOS + elevation delta + out-of-range targeting).
2. Expand deterministic replay coverage (multiple seeds/scenarios + expected state snapshots).
3. Add one PlayMode smoke validation aligned with the regression checklist.

## Working Notes
- Initial code quality impression of the original project is “spaghetti / inconsistent architecture.”
- Treat this as a reason to verify behavior, not to copy implementation.
- Preserve *what the game does*; redesign *how the code is organized*.
