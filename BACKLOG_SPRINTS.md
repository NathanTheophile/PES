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
