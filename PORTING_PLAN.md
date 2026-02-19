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
1. Audit existing Godot systems and list gameplay mechanics to preserve.
2. Define Unity domain model for `Grid3D`, turns, units, abilities, LOS, and reach.
3. Implement a small vertical slice (movement + one attack + height effect).
4. Validate parity with intended original behavior, then iterate.

---

## Working Notes
- Initial code quality impression of the original project is “spaghetti / inconsistent architecture.”
- Treat this as a reason to verify behavior, not to copy implementation.
- Preserve *what the game does*; redesign *how the code is organized*.
