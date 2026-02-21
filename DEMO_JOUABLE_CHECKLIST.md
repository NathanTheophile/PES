# Démo jouable — Vertical Slice Sprint H

Objectif: disposer d'une démo courte, reproductible et testable en playtest interne.

## Scope de la démo (v1)

- **Carte**: 1 map tactique (vertical slice actuelle) avec relief + obstacles.
- **Camps**: 2 équipes opposées (Team A vs Team B).
- **Roster**: 1 unité jouable par camp (extensible à 2v2 ensuite sans changer la boucle).
- **Kit skills représentatif**: 3 à 4 skills couvrant:
  - mono-cible dégâts,
  - skill avec coût ressource,
  - skill avec cooldown,
  - (optionnel) skill avec statut/DoT.

## Checklist UX minimale (Go / No-Go)

### A. Boucle de jeu
- [ ] Sélection d’unité claire (état sélectionné visible).
- [ ] Intention Move/Attack/Skill claire et modifiable.
- [ ] Annulation claire (ESC + bouton Cancel).
- [ ] Fin de tour explicite (Pass Turn) et auto-pass AP=0.

### B. Lisibilité tactique
- [ ] Cases atteignables visibles en mode Move.
- [ ] Prévisualisation de path au survol.
- [ ] Prévisualisation d’action planifiée lisible (move/attack/skill).
- [ ] Surbrillance de cible au survol (attack/skill).

### C. Skills / ressources
- [ ] Slots skills lisibles (READY / CD / NO_RES).
- [ ] Tooltip skill lisible (portée/coût/dégâts/hit/état).
- [ ] Rejets métier visibles et compréhensibles dans le HUD.

### D. Feedbacks visuels
- [ ] Pulses VFX placeholder branchés (succès/miss/rejet).
- [ ] Historique court des actions visible dans le HUD.

### E. Déterminisme
- [ ] Seed fixée documentée pour la démo.
- [ ] Scenario replay golden-path passe sur cette seed.
- [ ] Snapshot final attendu documenté/validé en test.

## Critère de validation de la démo (Sprint H)

La démo est considérée **prête** si:
1. Les sections A → D sont validées en playtest interne.
2. La section E est validée par tests déterministes/replay.
3. Aucun blocage UX majeur n’est signalé sur 3 runs consécutifs.

## Notes d'exécution playtest (template)

- Build/commit testé:
- Seed utilisée:
- Scénario joué:
- Résultat (Win/Lose/Draw):
- Retours UX majeurs:
- Bugs bloquants:
- Décision: GO / NO-GO

## Gate QA interne (Release Candidate)

Objectif: valider la démo avec **3 runs consécutifs** sans blocage majeur avant partage plus large.

### Conditions de passage
- [ ] 3 runs consécutifs terminés (win/lose/draw accepté) sans soft-lock ni crash.
- [ ] Aucun bug critique (blocant combat, corruption save, flow figé) sur les 3 runs.
- [ ] Seed et profil de test consignés pour chaque run.
- [ ] Même binaire/commit utilisé pour la série de 3 runs.

### Journal des runs QA (à remplir)
| Run | Commit | Seed | Profil/scénario | Résultat | Blocage majeur | Notes |
|---|---|---|---|---|---|---|
| 1 |  |  |  |  | Non/Oui |  |
| 2 |  |  |  |  | Non/Oui |  |
| 3 |  |  |  |  | Non/Oui |  |
