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

## Matrice (v1)

| Domaine | Feature | Référence Godot | Cible Unity | Statut | Gap principal | Justification `Improved` | Risque | Tests Unity | Replay-ready | Owner | Target sprint |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Combat | Déplacement tactique (Move) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Combat/Actions/MoveAction.cs` + `MoveValidationService.cs` | Improved | Valider les coûts terrain/cas limites de maps réelles Godot; figer la parité sur maps de prod | Règles métier explicites + rejets normalisés + payload structuré | High | `ActionResolverPipelineTests` | Partial | Combat | Sprint F |
| Combat | Attaque basique (range/LOS/hauteur) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Combat/Actions/BasicAttackAction.cs` + `Targeting/BasicAttackTargetingService.cs` | Improved | Aligner exactement les règles LOS Godot (sampling/masques collision) et effets secondaires | Séparation validation/ciblage/résolution + résultats structurés | High | `ActionResolverPipelineTests` | Partial | Combat | Sprint F |
| Core Sim | Pipeline d'action (resolve/log/tick) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/Simulation/ActionResolver.cs` | Improved | Définir format de payload stable versionné pour outillage externe | Architecture orientée actions + log structuré déterministe | Medium | `ActionResolverPipelineTests` | Yes | Core | Sprint F |
| Core Sim | État combat + snapshots | `One Piece Tactics/characters/Entity.gd`, `Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/Simulation/BattleState.cs` | Improved | Vérifier couverture de tous champs gameplay futurs dans snapshot | Snapshots explicites pour replay/rollback | High | `BattleReplayTests`, `ActionResolverPipelineTests` | Partial | Core | Sprint F |
| Turn System | Initiative / ordre des tours | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/TurnSystem/RoundRobinTurnController.cs` | Improved | Valider exceptions d'initiative par unité si présentes sur combats Godot | Contrôleur dédié + acteurs actifs/inactifs | Medium | `RoundRobinTurnControllerTests`, `VerticalSliceBattleLoopTests` | Partial | Core | Sprint F |
| Turn System | Fin de combat (win/lose/draw) | `One Piece Tactics/menus/Win.gd`, `areas/Score.gd` | `PES_Unity/Assets/_Project/Core/TurnSystem/BattleOutcomeEvaluator.cs` | In Progress | Brancher conditions scénario/objectif (pas seulement HP teams) | n/a | High | `BattleOutcomeEvaluatorTests`, `VerticalSliceBattleLoopTests` | Partial | Gameplay | Sprint G |
| Turn System | Timer de tour | (N/A explicite côté scripts listés) | `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceBattleLoop.cs` | Improved | Vérifier UX + règles d'expiration vs design final gameplay | Extension qualité de boucle tactique | Medium | `VerticalSliceBattleLoopTests` | No | Gameplay | Sprint G |
| Replay | Record + replay seedé | (N/A en l'état Godot) | `PES_Unity/Assets/_Project/Infrastructure/Replay/*` | Improved | Formaliser compatibilité version de record + contrats d'actions futures | Fondations MMO-compatible (logs + snapshots) | High | `BattleReplayTests` | Yes | Infra | Sprint F |
| Input/Presentation | Planification input ➜ commande | `One Piece Tactics/areas/ui/UI.gd` | `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceCommandPlanner.cs` | In Progress | Passer des bindings debug à un input tactique complet (sélection cellule/targeting UI) | n/a | Medium | `VerticalSliceCommandPlannerTests` | No | Presentation | Sprint G |
| Config gameplay | Policies runtime authoring | (data in-scene/scripts Godot) | `PES_Unity/Assets/_Project/Presentation/Configuration/CombatRuntimeConfigAsset.cs` + adapter | Improved | Valider pipeline de config en CI Unity + brancher sur assets gameplay réels | Data-driven tuning sans coupler le domaine à Unity | Medium | `CombatRuntimePolicyProviderTests` | Partial | Presentation | Sprint F |
| Skills | Système de skills / effets | `One Piece Tactics/skills/SkillScript.gd`, `skills/SkillEffectTemplate.gd` | `PES_Unity/Assets/_Project/Combat/*` (à créer: abilities/effects) | Not Started | Concevoir contrat de skill déterministe + targeting/aoe + coût | n/a | High | n/a | No | Gameplay | Sprint G |
| Skills | UI de skills / sélection | `One Piece Tactics/skills/SkillButton.gd`, `skills/skillCell.gd` | `PES_Unity/Assets/_Project/Presentation/UI/*` (à créer) | Not Started | Créer flow UX intention ➜ commande skill | n/a | Medium | n/a | No | Presentation | Sprint G |
| States | Machine d'états combat | `One Piece Tactics/states/StateScript.gd`, `StatePreview.gd` | `PES_Unity/Assets/_Project/Core/TurnSystem/*` (à étendre) | Not Started | Formaliser états/timing/interruptions côté domaine | n/a | High | n/a | No | Core | Sprint G |
| Meta/Progression | Sauvegarde/config | `One Piece Tactics/save.gd`, `SaveConfigs.gd` | `PES_Unity/Assets/_Project/Infrastructure/Serialization/*` (à créer) | Not Started | Définir schéma de save/versionnage et mapping domaine | n/a | Medium | n/a | No | Infra | Sprint H |
| Flow | Gestion scènes/chargement | `One Piece Tactics/SceneManager.gd`, `Run.gd` | `PES_Unity/Assets/_Project/Presentation/Scene/*` | In Progress | Harmoniser flow boot/menu/battle avec contrats domaine | n/a | Medium | `VerticalSliceBattleLoopTests` | No | Presentation | Sprint H |

## Gate de passage vers “portage gameplay complet”

Passage autorisé si les 5 conditions sont vraies:

1. Toutes les lignes `High` ont un `Owner` + `Target sprint` renseignés. ✅
2. Les features cœur (`Move`, `BasicAttack`, `ActionResolver`, `Turn`) sont `Parity` ou `Improved` avec gap explicite. ✅
3. Les features cœur sont au moins `Replay-ready: Partial` et planifiées vers `Yes`. ✅
4. Les mécaniques restantes (skills/states/save/flow) sont tracées dans la matrice. ✅
5. Chaque PR gameplay applique `REPLAY_CHECKLIST.md`. ✅ (process outillé, à appliquer en exécution)

## Process de mise à jour (obligatoire)

- Toute PR gameplay doit:
  1. Mettre à jour la ligne de matrice concernée.
  2. Ajouter/mettre à jour les tests associés.
  3. Expliquer le gap résolu ou introduit.
  4. Coller le template de `REPLAY_CHECKLIST.md` dans la PR.
- Si une feature est marquée `Parity` ou `Improved`, elle doit avoir au minimum un test déterministe référencé.
