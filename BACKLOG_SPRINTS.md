# Backlog portage Godot ➜ Unity (post Sprint 1)

## Sprint A — Stabilisation technique + data-driven combat
- [x] Externaliser les paramètres de `MoveAction` via `MoveActionPolicy` injecté par commande.
- [x] Externaliser les paramètres de `BasicAttackAction` via `BasicAttackActionPolicy` injecté par commande.
- [x] Ajouter des tests EditMode validant qu'un override de policy change effectivement le comportement.
- [x] Introduire une source de config runtime (ScriptableObject d'authoring + adapter vers policies domaine).
- [x] Ajouter un test de non-régression sur les defaults (fallback policy).

## Sprint B — Tour de jeu minimal
- [x] Initiative/ordre des tours.
- [x] Fin de tour + consommation de ressources d'action.
- [x] Conditions de victoire/défaite minimales.

## Sprint C — Input tactique
- [x] Sélection unité / intention (move/attack) en scène (bindings clavier de vertical slice).
- [x] Conversion UI/input en `IActionCommand` via `VerticalSliceCommandPlanner` (sans logique métier dans MonoBehaviour).

## Sprint D — Determinism & replay
- [x] Persister un flux `action log` + snapshots (in-memory recorder/replay).
- [x] Rejouer une simulation à seed égale et comparer les résultats.


### Sprint B.1 — Timer de tour
- [x] Ajouter un timer par tour avec bascule auto vers le prochain acteur à expiration.
- [x] Exposer l'état du timer (`RemainingTurnSeconds`) pour la couche présentation/debug HUD.
- [x] Ajouter des tests EditMode couvrant timeout partiel, timeout complet, et reset du timer après fin de tour.


## Sprint E — Pilotage portage complet
- [x] Créer la matrice de parité Godot ➜ Unity (`PARITY_MATRIX.md`).
- [x] Créer la checklist replay/déterminisme par feature (`REPLAY_CHECKLIST.md`).


## Sprint F — Readiness gate avant portage gameplay complet
- [x] Étendre la matrice de parité avec owner + target sprint + couverture skills/states/save/flow.
- [x] Définir un gate explicite de passage vers le portage gameplay complet.
- [x] Faire passer `Replay-ready` à `Yes` pour Move/BasicAttack/Turn (validation CI Unity).
- [x] Activer template `Replay/Determinism Checklist` sur toutes les PR gameplay.


## Sprint G — Objectifs scénario & skills
- [x] Ajouter un objectif scénario de base (Control Point) dans le domaine de fin de combat.
- [x] Ajouter coûts ressources + cooldowns sur `CastSkillAction` avec couverture tests/replay.
- [x] Faire décroître les cooldowns de skills au changement de tour (déterministe).
- [ ] Étendre vers objectifs multi-conditions (capture, survive, eliminate target).
- [ ] Porter le système de skills Godot vers contrats domaine Unity.
- [x] Ajouter un authoring Unity pour entités/kits de skills (`EntityArchetypeAsset` + `SkillDefinitionAsset`) avec adapter runtime vers le domaine.

- [x] Ajouter un premier support AOE splash sur `CastSkillAction` avec tests EditMode.

- [x] Démarrer une UI de sélection de skills (slots + état ready/cooldown/resource) dans le vertical slice.

- [x] Amorcer le système d'états domaine (status effects + tick déterministe + snapshot).
- [x] Permettre le timing DoT configurable par sort (début vs fin de tour).
- [x] Étendre `CastSkillAction` aux effets de statut configurables (cible + lanceur) via contrat domaine (buff/debuff).


## Sprint H — Milestone "niveau jouable" (focus UX/gameplay)
- [x] Finaliser la boucle joueur complète en combat: sélection unité ➜ move/attack/skill ➜ fin de tour explicite/auto sans blocage UX.
- [x] Ajouter des feedbacks visuels minimum viables: surbrillance des cases ciblables, prévisualisation dégâts/état, logs de résolution lisibles.
- [x] Stabiliser la UI skills en combat: tooltip (coût/portée/effets), états visuels cohérents (ready/cooldown/ressource insuffisante), annulation claire.
- [x] Uniformiser les feedbacks d'action rejetée (`OutOfRange`, `NoLineOfSight`, `NotEnoughResource`) entre domaine et HUD.
- [x] Ajouter un pass animation/VFX placeholder branché sur les events domaine (sans coupler la logique métier à Unity).
- [x] Définir une "démo jouable" courte (1 map + 2 camps + 3-4 skills représentatifs) avec checklist de validation UX (`DEMO_JOUABLE_CHECKLIST.md`).
- [x] Couvrir ce milestone par un scénario replay déterministe golden-path (seed fixée + snapshot final attendu).

_Progression Sprint H en cours:_
- ✅ Feedbacks visuels minimum viables validés sur le vertical slice (surbrillance cases atteignables, logs d'actions lisibles, previews move/attack/skill + markers shader d'intention + highlight de cible survolée en attack/skill).

## Sprint H.1 — Dé-risquage MMO (strict minimum)
- ✅ `SchemaVersion` ajouté sur `ActionResultPayload` (+ tests) pour stabiliser la compatibilité future des logs.
- [x] Versionner le format des payloads de log d'actions (compat replay inter-build).
- [ ] Poser un contrat de sérialisation stable pour snapshots de combat (identifiants, ordering, champs obligatoires).
- [ ] Reporter explicitement tout chantier réseau/live-op non requis à post-milestone jouable.
