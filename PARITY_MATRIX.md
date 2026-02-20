# Matrice de parité Godot ➜ Unity

> Objectif: suivre la parité fonctionnelle entre le projet Godot de référence (`One Piece Tactics/`) et l'implémentation Unity (`PES_Unity/`) pour piloter le portage gameplay complet sans dérive.

## Légende

### Statut
- `Not Started`: pas de portage Unity détecté.
- `In Progress`: portage partiel / comportement incomplet.
- `Parity`: comportement cible aligné avec Godot (à niveau d'intention gameplay).
- `Improved`: comportement volontairement amélioré (écart documenté).

### Risque
- `Low`: faible impact cross-systems.
- `Medium`: interactions avec plusieurs systèmes.
- `High`: impact sur boucle cœur, déterminisme ou données.

### Replay-ready
- `No`: pas de garantie structurée pour replay.
- `Partial`: logs/snapshots présents mais contrat incomplet.
- `Yes`: entrée/action/résultat sérialisables + tests déterministes.

## Matrice (v0)

| Domaine | Feature | Référence Godot | Cible Unity | Statut | Gap principal | Risque | Tests Unity | Replay-ready |
|---|---|---|---|---|---|---|---|---|
| Combat | Déplacement tactique (Move) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Combat/Actions/MoveAction.cs` + `MoveValidationService.cs` | Improved | Valider les coûts terrain/cas limites de maps réelles Godot; figer la parité sur maps de prod | High | `ActionResolverPipelineTests` | Partial |
| Combat | Attaque basique (range/LOS/hauteur) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Combat/Actions/BasicAttackAction.cs` + `Targeting/BasicAttackTargetingService.cs` | Improved | Aligner exactement les règles LOS Godot (sampling/masques collision) et effets secondaires | High | `ActionResolverPipelineTests` | Partial |
| Core Sim | Pipeline d'action (resolve/log/tick) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/Simulation/ActionResolver.cs` | Improved | Définir format de payload stable versionné pour outillage externe | Medium | `ActionResolverPipelineTests` | Yes |
| Core Sim | État combat + snapshots | `One Piece Tactics/characters/Entity.gd`, `Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/Simulation/BattleState.cs` | Improved | Vérifier couverture de tous champs gameplay futurs dans snapshot | High | `BattleReplayTests`, `ActionResolverPipelineTests` | Partial |
| Turn System | Initiative / ordre des tours | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/TurnSystem/RoundRobinTurnController.cs` | Improved | Valider si certains combats Godot utilisent exceptions d'initiative par unité | Medium | `RoundRobinTurnControllerTests`, `VerticalSliceBattleLoopTests` | Partial |
| Turn System | Fin de combat (win/lose/draw) | `One Piece Tactics/menus/Win.gd`, `areas/Score.gd` | `PES_Unity/Assets/_Project/Core/TurnSystem/BattleOutcomeEvaluator.cs` | In Progress | Brancher conditions scénario/objectif (pas seulement HP teams) | High | `BattleOutcomeEvaluatorTests`, `VerticalSliceBattleLoopTests` | Partial |
| Turn System | Timer de tour | (N/A explicite côté scripts listés) | `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceBattleLoop.cs` | Improved | Vérifier UX + règles d'expiration vs design final gameplay | Medium | `VerticalSliceBattleLoopTests` | No |
| Replay | Record + replay seedé | (N/A en l'état Godot) | `PES_Unity/Assets/_Project/Infrastructure/Replay/*` | Improved | Formaliser compatibilité version de record + contrats d'actions futures | High | `BattleReplayTests` | Yes |
| Input/Presentation | Planification input ➜ commande | `One Piece Tactics/areas/ui/UI.gd` | `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceCommandPlanner.cs` | In Progress | Passer des bindings debug à un input tactique complet (sélection cellule/targeting UI) | Medium | `VerticalSliceCommandPlannerTests` | No |
| Config gameplay | Policies runtime authoring | (probablement data in-scene/scripts Godot) | `PES_Unity/Assets/_Project/Presentation/Configuration/CombatRuntimeConfigAsset.cs` + adapter | In Progress | Ajouter source de config durable + fallback policy validé en CI Unity | Medium | `CombatRuntimePolicyProviderTests` | Partial |

## Priorités immédiates pour lancer le portage gameplay complet

1. **Compléter la matrice sur les mécaniques Godot restantes** (skills, états, objectifs, UI tactique, progression).
2. **Documenter chaque écart `Improved`** avec justification design (éviter ambiguïté “bug vs amélioration”).
3. **Passer `Replay-ready` à `Yes`** sur toutes actions cœur (Move, Attack, Turn, Win/Lose).
4. **Ajouter un owner + target sprint** par ligne de matrice (pilotage opérationnel).

## Process de mise à jour (obligatoire)

- Toute PR gameplay doit:
  1. Mettre à jour la ligne de matrice concernée.
  2. Ajouter/mettre à jour les tests associés.
  3. Expliquer le gap résolu ou introduit.
- Si une feature est marquée `Parity` ou `Improved`, elle doit avoir au minimum un test déterministe référencé.
