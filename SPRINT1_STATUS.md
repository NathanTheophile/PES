# Sprint 1 — Audit d'implémentation (Godot ➜ Unity)

## Verdict rapide
Le bootstrap technique minimal demandé dans le Sprint 1 est **globalement implémenté** dans l'état actuel du dépôt.

## Points vérifiés

### 1) Arborescence `_Project` créée
Présent sous `PES_Unity/Assets/_Project/` avec les domaines :
- `Core`
- `Grid`
- `Combat`
- `Units`
- `AI`
- `Infrastructure`
- `Presentation`
- `Tests` (`EditMode`, `PlayMode`)

### 2) Assemblies (`asmdef`) en place et dépendances cohérentes
Assemblies détectées :
- `PES.Core`
- `PES.Grid`
- `PES.Combat`
- `PES.Units`
- `PES.AI`
- `PES.Infrastructure`
- `PES.Presentation`
- `PES.Tests.EditMode`
- `PES.Tests.PlayMode`

Les dépendances principales suivent la direction domain-first décrite dans `PORTING_PLAN.md`
(`Presentation` dépend du domaine ; le domaine ne dépend pas de `Presentation`).

### 3) Fichiers C# de bootstrap
Les 10 fichiers de base listés dans `PORTING_PLAN.md` sont présents, et complétés au-delà du simple stub (compilation orientée simulation/action).

### 4) Test EditMode pipeline d'action
Un test `ActionResolverPipelineTests` est présent et valide le passage d'une action par le pipeline `ActionResolver` (résolution + événement + tick).

### 5) Contraintes d'architecture
- Namespace `PES.*` respecté sur les fichiers audités.
- Simulation orientée actions (`IActionCommand`, `ActionResolver`) en place.
- RNG centralisée introduite (`IRngService`, `SeededRngService`) pour préparer la déterminisation.

## Ambiguïtés / écarts à signaler

1. **Le plan parle de “stubs minimaux”, mais le repo contient déjà une implémentation plus avancée**
   (notamment `MoveAction`, `BasicAttackAction`, `PathfindingService`, boucle de vertical slice). Cela dépasse le strict Sprint 1.

2. **Le plan “Immediate Next Steps” demande ensuite MoveAction puis BasicAttackAction**,
   alors qu'une première version des deux existe déjà. La suite doit donc être interprétée comme
   “durcir/industrialiser” ces actions (règles métier plus strictes, couverture de tests, invariants réseau/replay).

## Reste à faire prioritaire (MoveAction / BasicAttackAction)

### MoveAction (vertical slice robuste)
- Formaliser explicitement les règles de mouvement (budget PM, coût terrain, occupation dynamique, jump max).
- Exposer des raisons de rejet normalisées (codes métier dédiés).
- Ajouter des tests déterministes multi-cas (coûts terrain, blocages dynamiques, rollback complet).
- Préparer la sérialisation de commande/résultat pour replay/réseau futur.

### BasicAttackAction
- Renforcer LOS/range/hauteur (actuellement simplifiés), en séparant validation/ciblage/résolution.
- Introduire des résultats de combat structurés (hit/miss/crit, damage packet, tags d'effet).
- Ajouter une batterie de tests orientés déterminisme (même seed => même séquence de résultats).

## Next step immédiate recommandée
Faire un **vertical slice MoveAction v2** :
1. Spécifier les invariants de mouvement dans un contrat métier.
2. Implémenter `MoveValidationService` + `MoveResolutionService`.
3. Étendre les tests EditMode sur les cas limites 3D (hauteur, obstacles, coûts).
4. Ajouter un log d'événement structuré stable (codes + payload sérialisable).

---

## Point de situation (mise à jour)

### Où en est le portage ?
- Le socle **domain-first** est en place et fonctionnel: simulation orientée actions, pipeline `ActionResolver`, snapshots/replay, et premières policies runtime.
- Les briques cœur `Move` et `BasicAttack` sont déjà au niveau **Improved** dans la matrice de parité (avec gaps documentés à refermer pour la parité stricte Godot sur certains détails).
- Le projet est donc **au-delà d'un simple bootstrap Sprint 1**: la base technique est suffisamment mature pour industrialiser les features gameplay.

### Prochaines étapes recommandées (ordre conseillé)
1. **Fermer les gaps High-risk déjà identifiés** sur `Move` et `BasicAttack` (LOS, hauteur, coûts terrain réels, collisions dynamiques).
2. **Stabiliser les contrats replay** (payloads versionnés, invariants de sérialisation des actions/résultats).
3. **Lancer le portage du système de skills** (domaine d'abord: coût, targeting, AOE, effets, rejets normalisés).
4. **Brancher la couche présentation/UI** (sélection cible/cellule ➜ commandes explicites) sans remettre de logique métier dans les `MonoBehaviour`.
5. **Étendre la couverture de tests déterministes** par feature avant montée en volume de contenu.

### Peut-on passer au portage du gameplay complet ?
**Oui, avec garde-fous.**

La gate "portage gameplay complet" de la matrice est déjà validée, donc le passage est possible.
En pratique, il faut démarrer par les features `Skills`/`States`/`Flow` déjà tracées, tout en continuant à fermer les gaps critiques sur le cœur combat pour éviter une dette architecture/replay.

### Préparation à faire avant d'accélérer
- Figer un **contrat d'action standard** (input, validation, résultat, event log, payload versionné) appliqué à toutes les futures mécaniques.
- Définir un **cadre de migration feature-par-feature**: pour chaque mécanique Godot, ouvrir une ligne de parité + checklist replay + tests déterministes minimum.
- Prioriser une **verticalisation gameplay**: intégrer d'abord 1-2 skills end-to-end (domaine + présentation + tests), puis élargir progressivement.
