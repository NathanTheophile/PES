# Architecture Bridge — du paradigme Unity classique au découpage domaine

Ce document sert de pont entre des patterns Unity "habituels" et l'architecture utilisée dans ce repo.

## 1) « MonoBehaviour qui fait tout » → « Planner/Adapter + Commande domaine »

### Ancien réflexe
Un seul `MonoBehaviour` capte l'input, valide des règles de gameplay, modifie l'état, et déclenche les effets.

### Équivalent dans ce repo
- **Planner côté présentation**: construit une intention joueur en **commande domaine**.
- **Commande domaine**: objet métier exécutable par le moteur de résolution.
- **Boucle de combat**: orchestre l'exécution et la consommation de tour/action.

### Exemple concret
Dans `VerticalSliceCommandPlanner`, la sélection/planification UI (`SelectActor`, `PlanMove`, `PlanAttack`, `PlanSkill`) est séparée de la construction d'une commande via `TryBuildCommand`.
- Move → `new MoveAction(...)`
- Attack → `new BasicAttackAction(...)`
- Skill → `new CastSkillAction(...)`

👉 Pointeur principal: `PES_Unity/Assets/_Project/Presentation/Scene/VerticalSliceCommandPlanner.cs`

### Pourquoi c'est mieux
- Testable sans scène Unity.
- Les règles restent dans les actions/services domaine, pas dans un script de vue.
- Les sources d'intention (clavier, IA, réseau) peuvent produire les mêmes commandes.

---

## 2) « Classe héritée d'entité » → « Données dans `BattleState` + règles dans actions/services »

### Ancien réflexe
Créer des hiérarchies de classes (`Warrior : UnitBase`, `Mage : UnitBase`) qui portent à la fois données + logique + effets.

### Équivalent dans ce repo
- **Données runtime centralisées** dans `BattleState` (positions, PV, PM, ressources, cooldowns, logs, tick).
- **Règles métier** réparties dans les actions/services de combat (validation de move, ciblage, résolution dégâts, etc.).

### Exemple concret
`BattleState` ne dépend pas d'Unity et expose des opérations d'état déterministes:
- Position: `SetEntityPosition`, `TryMoveEntity`
- PV: `SetEntityHitPoints`, `TryApplyDamage`
- PM: `SetEntityMovementPoints`, `TryConsumeMovementPoints`, `ResetMovementPoints`
- Ressources/cooldowns: `SetEntitySkillResource`, `TryConsumeEntitySkillResource`, `SetSkillCooldown`, `TickDownSkillCooldowns`

👉 Pointeur principal: `PES_Unity/Assets/_Project/Core/Simulation/BattleState.cs`

### Pourquoi c'est mieux
- L'état est sérialisable/snapshotable (`CreateSnapshot` / `ApplySnapshot`).
- Moins de couplage à une hiérarchie OO fragile.
- Plus simple d'ajouter des mécaniques transverses (buffs, objectifs, replay).

---

## 3) « ScriptableObject gameplay » → « Asset d'authoring + conversion en policy domaine »

### Ancien réflexe
Le `ScriptableObject` contient directement la logique d'exécution gameplay.

### Équivalent dans ce repo
- **Asset de configuration (authoring)**: stocke des paramètres éditables Unity.
- **Conversion explicite**: transforme l'asset en politiques domaine (`MoveActionPolicy`, `BasicAttackActionPolicy`, `SkillActionPolicy`).
- **Adapter/provider**: injecte les policies dans le runtime.

### Exemple concret
1. `CombatRuntimeConfigAsset` expose des champs Unity (`[SerializeField]`, `[Range]`, `[Min]`) et des méthodes `ToMovePolicy()`, `ToBasicAttackPolicy()`, `ToSkillPolicy()`.
2. `CombatRuntimePolicyProvider.FromAsset(...)` convertit l'asset en `RuntimeCombatPolicies` (overrides prêts à injecter).

👉 Pointeurs principaux:
- `PES_Unity/Assets/_Project/Presentation/Configuration/CombatRuntimeConfigAsset.cs`
- `PES_Unity/Assets/_Project/Presentation/Adapters/CombatRuntimePolicyProvider.cs`

### Pourquoi c'est mieux
- Les designers éditent l'asset, le domaine consomme des objets métier purs.
- Les policies peuvent être testées sans Unity.
- On peut remplacer la source (JSON, backend, debug menu) sans toucher aux règles.

---

## Comment ajouter une nouvelle mécanique en 6 étapes (mini flux end-to-end)

Exemple: ajouter une mécanique "Poison Strike".

1. **Définir la policy domaine**
   - Créer/étendre une policy de mécanique (ex. coût, portée, dégâts initiaux, dégâts sur durée).

2. **Ajouter la commande/action domaine**
   - Créer une commande `IActionCommand` dédiée (ex. `PoisonStrikeAction`) et sa validation/résolution via services dédiés.

3. **Étendre l'état si nécessaire**
   - Si la mécanique nécessite de la mémoire runtime (ex. stacks de poison), ajouter les données dans `BattleState` (et snapshot).

4. **Brancher l'authoring Unity**
   - Ajouter les champs dans un asset de config (dans l'esprit de `CombatRuntimeConfigAsset`) et exposer la conversion vers une policy domaine.

5. **Adapter l'injection runtime**
   - Étendre le provider (dans l'esprit de `CombatRuntimePolicyProvider`) pour fournir la nouvelle policy à la boucle/planner/actions.

6. **Relier la présentation au domaine**
   - Ajouter la planification côté présentation (dans l'esprit de `VerticalSliceCommandPlanner`) pour produire la nouvelle commande.
   - Vérifier l'exécution dans la boucle de combat (turn/action + logs) de bout en bout.

### Mini flux E2E
Input joueur → Planner (intent) → `PoisonStrikeAction` (commande domaine) → Resolver/services (règles) → mutation `BattleState` + event log → rendu/feedback UI.
