# Checklist Replay / Determinism (priorité 1)

> Objectif: garantir qu'une feature gameplay Unity soit **rejouable**, **testable**, et **MMO-compatible plus tard** sans refonte lourde.

Cette checklist est obligatoire pour toute feature qui ajoute/modifie:
- une `IActionCommand`,
- une règle de résolution combat,
- un état de simulation persistant,
- un flux de tour/initiative/victoire.

## 0) Scope de la PR

- [ ] La feature est liée à une ligne de `PARITY_MATRIX.md`.
- [ ] Le statut de la ligne est mis à jour (`Not Started` / `In Progress` / `Parity` / `Improved`).
- [ ] Le `Gap principal` est mis à jour (résolu/ajusté/ajouté).

---

## 1) Contrat d'entrée (Input deterministe)

- [ ] Les entrées de la commande sont explicites et sérialisables (pas de lecture implicite Unity `Transform`, UI state, etc.).
- [ ] Aucune dépendance cachée au temps réel (`Time.deltaTime`) dans la résolution métier.
- [ ] Aucune dépendance cachée à l'ordre d'itération non stable des collections pour les décisions métier.
- [ ] Toute source aléatoire passe par `IRngService` injecté.

## 2) Validation métier (rejets normalisés)

- [ ] Tous les refus métier retournent un `ActionResolutionCode` cohérent (`Rejected`/`Missed`/`Succeeded`).
- [ ] Tous les refus métier exposent un `ActionFailureReason` normalisé (pas de “string-only”).
- [ ] Les invariants invalides (policy/input impossible) sont rejetés explicitement (`InvalidPolicy`, etc.).
- [ ] Les cas “acteur KO / cible KO / positions manquantes” sont traités explicitement.

## 3) Résultat structuré (sortie sérialisable)

- [ ] Le résultat expose un payload structuré (`ActionResultPayload`) quand nécessaire.
- [ ] Les champs payload sont stables et documentés (`Kind`, `Value1..Value3` ou équivalent).
- [ ] Les infos critiques ne sont pas uniquement dans `Description` texte.
- [ ] Le résultat est suffisant pour alimenter replay/debug sans re-simuler côté présentation.

## 4) Journalisation / timeline

- [ ] L'action passe par `ActionResolver` (pas de bypass ad hoc).
- [ ] Un `CombatEventRecord` est écrit avec `Tick`, `Code`, `FailureReason`, `Description`, `Payload`.
- [ ] Le `Tick` est incrémenté de façon déterministe après résolution.
- [ ] Aucune mutation métier hors pipeline non tracée.

## 5) Snapshot / restauration

- [ ] Les données impactées par la feature sont présentes dans `BattleState` snapshot/replay si nécessaire.
- [ ] `CreateSnapshot` + `ApplySnapshot` permettent rollback/replay sans perte d'information critique.
- [ ] Les structures de données ajoutées ont un comportement stable à la restauration.

## 6) Tests déterministes (minimum requis)

- [ ] **Unity EditMode CI vert** (`Unity EditMode Tests` workflow).
- [ ] **1 test success path** (résolution nominale).
- [ ] **1 test rejet invariant** (validation métier non triviale).
- [ ] **1 test seed-identique => résultat identique**.
- [ ] **1 test seed-différente => divergence contrôlée** (si RNG impliqué).
- [ ] **1 test event log** (code/failure/payload/tick attendus).

### Bonus recommandé
- [ ] Tests de bords 3D (hauteur, LOS, obstacles intermédiaires, occupation dynamique).
- [ ] Test de non-régression policy default vs override runtime.

## 7) Couche présentation (hygiène archi)

- [ ] Aucun `MonoBehaviour` ne contient de logique de règles métier.
- [ ] L'input UI est transformé en `IActionCommand` explicite.
- [ ] La présentation consomme des résultats structurés (pas de parsing fragile de `Description`).

## 8) Replay-ready verdict (à reporter dans la matrice)

Cocher un verdict:
- [ ] `No` (gaps majeurs bloquants)
- [ ] `Partial` (pipeline présent mais contrat incomplet)
- [ ] `Yes` (contrat complet + tests déterministes)

Justification (obligatoire):
- `Replay-ready verdict:`
- `Gaps restants:`
- `Actions next sprint:`

---

## Template à coller dans la description de PR gameplay

```md
### Replay/Determinism Checklist
- [ ] Scope matrice mis à jour
- [ ] Input sérialisable
- [ ] Rejets normalisés
- [ ] Résultat structuré
- [ ] Event log + tick déterministes
- [ ] Snapshot/replay couverts
- [ ] Tests déterministes minimum (5)
- [ ] Verdict Replay-ready (`No`/`Partial`/`Yes`) + justification
```

## Definition of Done (Gameplay)

Une PR gameplay n'est pas “Done” si:
1. la ligne `PARITY_MATRIX.md` n'est pas mise à jour,
2. la checklist ci-dessus n'est pas remplie,
3. les tests déterministes minimum ne sont pas présents.
