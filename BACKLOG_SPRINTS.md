# Backlog portage Godot ➜ Unity (post Sprint 1)

## Sprint A — Stabilisation technique + data-driven combat
- [x] Externaliser les paramètres de `MoveAction` via `MoveActionPolicy` injecté par commande.
- [x] Externaliser les paramètres de `BasicAttackAction` via `BasicAttackActionPolicy` injecté par commande.
- [x] Ajouter des tests EditMode validant qu'un override de policy change effectivement le comportement.
- [ ] Introduire une source de config runtime (ScriptableObject d'authoring + adapter vers policies domaine).
- [ ] Ajouter un test de non-régression sur les defaults (fallback policy).

## Sprint B — Tour de jeu minimal
- [ ] Initiative/ordre des tours.
- [ ] Fin de tour + consommation de ressources d'action.
- [ ] Conditions de victoire/défaite minimales.

## Sprint C — Input tactique
- [x] Sélection unité / intention (move/attack) en scène (bindings clavier de vertical slice).
- [x] Conversion UI/input en `IActionCommand` via `VerticalSliceCommandPlanner` (sans logique métier dans MonoBehaviour).

## Sprint D — Determinism & replay
- [x] Persister un flux `action log` + snapshots (in-memory recorder/replay).
- [x] Rejouer une simulation à seed égale et comparer les résultats.
