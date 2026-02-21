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
| Combat | Déplacement tactique (Move) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Combat/Actions/MoveAction.cs` + `MoveValidationService.cs` | Improved | Finaliser PM avancés (buff/debuff/regen) et parité fine des coûts terrain Godot | Règles métier explicites + rejets normalisés + payload structuré | High | `ActionResolverPipelineTests` + `VerticalSliceBattleLoopTests` + CI Unity EditMode | Yes | Combat | Sprint F |
| Combat | Attaque basique (range/LOS/hauteur) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Combat/Actions/BasicAttackAction.cs` + `Targeting/BasicAttackTargetingService.cs` | Improved | Aligner exactement les règles LOS Godot (sampling/masques collision) et effets secondaires | Séparation validation/ciblage/résolution + résultats structurés | High | `ActionResolverPipelineTests` + `VerticalSliceBattleLoopTests` + CI Unity EditMode | Yes | Combat | Sprint F |
| Core Sim | Pipeline d'action (resolve/log/tick) | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/Simulation/ActionResolver.cs` | Improved | Définir format de payload stable versionné pour outillage externe | Architecture orientée actions + log structuré déterministe | Medium | `ActionResolverPipelineTests` | Yes | Core | Sprint F |
| Core Sim | État combat + snapshots | `One Piece Tactics/characters/Entity.gd`, `Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/Simulation/BattleState.cs` | Improved | Vérifier couverture de tous champs gameplay futurs dans snapshot | Snapshots explicites pour replay/rollback | High | `BattleReplayTests`, `ActionResolverPipelineTests` | Partial | Core | Sprint F |
| Turn System | Initiative / ordre des tours | `One Piece Tactics/Battle Mechanics.gd` | `PES_Unity/Assets/_Project/Core/TurnSystem/RoundRobinTurnController.cs` | Improved | Valider exceptions d'initiative par unité si présentes sur combats Godot | Contrôleur dédié + acteurs actifs/inactifs | Medium | `RoundRobinTurnControllerTests`, `VerticalSliceBattleLoopTests` + CI Unity EditMode | Yes | Core | Sprint F |
| Turn System | Fin de combat (win/lose/draw) | `One Piece Tactics/menus/Win.gd`, `areas/Score.gd` | `PES_Unity/Assets/_Project/Core/TurnSystem/BattleOutcomeEvaluator.cs` | Improved | Étendre les objectifs scénario (multi-objectifs, priorité, tie-break) au-delà du control point de base | Évaluation de victoire découplée, extensible via objectifs domaine | High | `BattleOutcomeEvaluatorTests`, `VerticalSliceBattleLoopTests` + CI Unity EditMode | Yes | Gameplay | Sprint G |
| Turn System | Timer de tour | (N/A explicite côté scripts listés) | `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceBattleLoop.cs` | Improved | Vérifier UX + règles d'expiration vs design final gameplay | Extension qualité de boucle tactique | Medium | `VerticalSliceBattleLoopTests` + CI Unity EditMode | Yes | Gameplay | Sprint G |
| Replay | Record + replay seedé | (N/A en l'état Godot) | `PES_Unity/Assets/_Project/Infrastructure/Replay/*` | Improved | Formaliser compatibilité version de record + contrats d'actions futures | Fondations MMO-compatible (logs + snapshots) | High | `BattleReplayTests` | Yes | Infra | Sprint F |
| Input/Presentation | Planification input ➜ commande | `One Piece Tactics/areas/ui/UI.gd` | `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceCommandPlanner.cs` + `VerticalSliceBootstrap.cs` | In Progress | Finaliser UX souris/GUI (retours visuels, surbrillance, annulation, confirmations) | Flux jouable in-engine (+ overlay déplacements possibles + preview de path) | Medium | `VerticalSliceCommandPlannerTests` (move/attack/skill) | Partial | Presentation | Sprint G |
| Config gameplay | Policies runtime authoring | (data in-scene/scripts Godot) | `PES_Unity/Assets/_Project/Presentation/Configuration/CombatRuntimeConfigAsset.cs` + `EntityArchetypeAsset.cs` + adapters | Improved | Finaliser import auto des assets de roster/skills dans toutes les scènes de combat (pas seulement vertical slice) | Data-driven tuning sans coupler le domaine à Unity (policies + archetypes + loadouts) | Medium | `CombatRuntimePolicyProviderTests` + `EntityArchetypeRuntimeAdapterTests` | Partial | Presentation | Sprint G |
| Skills | Système de skills / effets | `One Piece Tactics/skills/SkillScript.gd`, `skills/SkillEffectTemplate.gd` | `PES_Unity/Assets/_Project/Combat/Actions/CastSkillAction.cs` + `Targeting/SkillTargetingService.cs` + `Resolution/SkillResolutionService.cs` | In Progress | Finaliser uniquement les interactions d'effets Godot les plus exotiques (helper visuals/scripts) après verrouillage du contrat domaine | Ciblage x/z + LOS raycast domaine + bonus portée par élévation + coût/cooldown + splash + statuts configurables + rejets/payloads normalisés et testés | High | `CastSkillActionTests` + `CastSkillPipelineTests` + `BattleReplayTests` + `VerticalSliceBattleLoopTests` + CI Unity EditMode | Yes | Gameplay | Sprint G |
| Skills | UI de skills / sélection | `One Piece Tactics/skills/SkillButton.gd`, `skills/skillCell.gd` | `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceBootstrap.cs` (HUD skill slots) + `Presentation/UI/*` (à étendre) | In Progress | Finaliser UI dédiée (tooltips, coûts/icônes, sélection cible avancée) au-delà du HUD debug | Première sélection de slot de skill en HUD debug (ready/cooldown/resource) branchée au planner domaine | Medium | `VerticalSliceCommandPlannerTests` + checks manuels vertical slice | Partial | Presentation | Sprint G |
| Units/Stats | Socle caractéristiques RPG (AP/MP/PO/élévation/invocs/HP/assiduité/rapidité + attaques/puissances/défenses/résistances par élément + critiques) | (N/A explicite côté scripts listés) | `PES_Unity/Assets/_Project/Core/Simulation/CombatantRpgStats.cs` + `DamageFormulaCalculator.cs` + `Combat/Actions/*Attack*` + `CastSkillAction.cs` + `Presentation/Configuration/EntityArchetypeAsset.cs` | Improved | Brancher les derniers cas de dommages secondaires (splash/DoT) sur les stats élémentaires et enrichir payloads critiques | Attaque basique + skill utilisent désormais la formule élémentaire + roll critique (base+chance), et l'initiative du vertical slice exploite Rapidité | High | `DamageFormulaCalculatorTests` + `ActionResolverPipelineTests` + `VerticalSliceBattleLoopTests` | Partial | Core | Sprint G |
| States | Machine d'états combat | `One Piece Tactics/states/StateScript.gd`, `StatePreview.gd` | `PES_Unity/Assets/_Project/Core/Simulation/BattleState.cs` + `Core/TurnSystem/*` | In Progress | Étendre au catalogue complet d'états Godot (interruptions/crowd-control) et au preview UI dédié | Fondations status-effects déterministes (Poison + modificateurs Weakened/Fortified/Marked appliqués sur dégâts, durée, tick configurable, snapshot/replay) | High | `StatusEffectStateTests` + `StatusEffectDamageModifierTests` + `VerticalSliceBattleLoopTests` | Partial | Core | Sprint G |
| Meta/Progression | Sauvegarde/config | `One Piece Tactics/save.gd`, `SaveConfigs.gd` | `PES_Unity/Assets/_Project/Infrastructure/Serialization/*` | In Progress | Couvrir la migration de schéma (v2+) + préparer backend fichier/chiffrement pour production | Contrat de save minimal versionné + sérialiseur tolérant + store mémoire + store PlayerPrefs branché flow | Medium | `SessionSaveSerializerTests` + `ProductFlowControllerTests` + `PlayerPrefsSessionSaveStoreTests` | Partial | Infra | Sprint H |
| Flow | Gestion scènes/chargement | `One Piece Tactics/SceneManager.gd`, `Run.gd` | `PES_Unity/Assets/_Project/Presentation/Flow/*` + `Presentation/Scene/VerticalSliceBootstrap.cs` | In Progress | Brancher les transitions UI réelles (scènes menu dédiées + boutons continue/new game) sur le contrôleur de flow | Contrôleur produit explicite boot/menu/battle branché au bootstrap avec reprise de seed de session | Medium | `ProductFlowControllerTests` + `VerticalSliceBattleLoopTests` | Partial | Presentation | Sprint H |

## Gate de passage vers “portage gameplay complet”

Passage autorisé si les 5 conditions sont vraies:

1. Toutes les lignes `High` ont un `Owner` + `Target sprint` renseignés. ✅
2. Les features cœur (`Move`, `BasicAttack`, `ActionResolver`, `Turn`) sont `Parity` ou `Improved` avec gap explicite. ✅
3. Les features cœur sont au moins `Replay-ready: Partial` et planifiées vers `Yes`. ✅
4. Les mécaniques restantes (skills/states/save/flow) sont tracées dans la matrice. ✅
5. Chaque PR gameplay applique `REPLAY_CHECKLIST.md`. ✅ (template PR + contrôle CI)

## Process de mise à jour (obligatoire)

- Toute PR gameplay doit:
  1. Mettre à jour la ligne de matrice concernée.
  2. Ajouter/mettre à jour les tests associés.
  3. Expliquer le gap résolu ou introduit.
  4. Coller le template de `REPLAY_CHECKLIST.md` dans la PR.
- Si une feature est marquée `Parity` ou `Improved`, elle doit avoir au minimum un test déterministe référencé.
