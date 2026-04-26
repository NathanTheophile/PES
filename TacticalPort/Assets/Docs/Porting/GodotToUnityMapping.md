# Godot To Unity Mapping

Sources lues en priorité:
- `Source/One Piece Tactics/Battle Mechanics.gd`
- `Source/One Piece Tactics/characters/Entity.gd`
- `Source/One Piece Tactics/skills/SkillScript.gd`
- `Source/One Piece Tactics/skills/skillCell.gd`
- `Source/One Piece Tactics/Area.gd`
- Skills représentatives: `Rubber Bazooka`, `Musket Shot`, `Gunpowder Star`, `Lunch`, `Cura Fervente`, `Run Away!`, `Tatsu Maki`, `Caltrop Hell`, `Call Reinforcements`, `Purrsuit`

## Mapping recommandé

| Feature Godot | Constats | Equivalent Unity recommandé |
|---|---|---|
| `Battle Mechanics.gd` | Fait trop de choses: tour, pathfinding, LoS, dégâts, push, summon, états, UI hooks | `BattleFlowService` + `GridService` + `TargetingService` + `CombatResolver` + `ScenarioRules` |
| `Entity.gd` | Mélange data exportée, runtime, signaux, anim, IA | `CombatantDefinition` (ScriptableObject) + `CombatantRuntime` (MonoBehaviour/pure C# state) |
| `SkillScript.gd` + sous-scripts inline dans `.tscn` | Métadonnées communes + logique spéciale par skill | `SkillDefinition` + `SkillEffect` dédiés; data d’un côté, résolution de l’autre |
| `Area.gd` | Bootstrap de map, UI, spawn, input, règles de scène | `BattleScenarioDefinition` + `BattleBootstrapper` + `BattleHUDPresenter` |
| `skillCell.gd` | Mélange preview AoE + validation + exécution | `TargetingOverlayPresenter` + `TargetPatternLibrary` |
| `SkillButton.gd` | UI lit directement la logique combat | `SkillButtonViewModel` alimenté par un service de validation |
| `StateScript.gd` | Effets de statut instanciés en scène | `StatusEffectDefinition` + `StatusEffectInstance` |
| `Glyph.gd` | Pièges/cases persistantes pilotées par signaux de déplacement | `BoardEffectDefinition` + `BoardEffectRuntime` |
| AI inline dans les `.tscn` ennemis | IA simple souvent “skill 0 puis move” mais parfois boss scripté | `EnemyBehaviorProfile` + `EnemyTurnController`; scripts boss dans `ScenarioRules` |
| TileMap ids `0/1/2/-1` | Sol libre / joueur / ennemi / obstacle bloquant | `GridCellOccupancy` explicite, pas des ids magiques |

## Règles de combat à porter

### Déplacement
- Grille orthogonale uniquement, coût Manhattan, pas de diagonales.
- Pathfinding basé sur les cases `TileMap == 0`; les cases occupées bloquent.
- Une unité peut être désélectionnée tant qu’elle n’a pas encore fait d’action réelle.
- Le coût de déplacement est exactement le nombre de cases traversées.
- `big = true` existe: footprint `3x3`, déplacement seulement si les `9` cases sont libres.
- Le push ne gère pas les grosses unités dans le code lu.

### AP / MP
- Le runtime stocke `ap_used` et `mp_used`, pas des ressources “courantes”.
- Fin de tour perso ou ennemi: `ap_used` et `mp_used` reviennent à `0`.
- Certaines compétences modifient le “restant” via `removal("ap"/"mp", amount)`.
- `Run Away!` donne `+5 MP` en faisant `mp_used -= 5`.

### Ordre des tours
- Phase joueur: chaque unité jouable agit une fois.
- Phase ennemi: ordre séquentiel de `enemyOrder`, sans initiative fine.
- `enemyfirst = true` existe au moins sur `Area5`.
- Le round augmente au passage joueur->ennemis ou ennemis->joueur selon `enemyfirst`.

### Portée / LoS / AoE
- Portée en Manhattan entre `rangeMin` et `rangeMax`.
- `linear = true` signifie alignement horizontal ou vertical seulement.
- `sight = true` utilise `checkSight`, qui considère les cases occupées `1/2` comme des bloqueurs de LoS.
- La cellule cible n’est pas un bloqueur pour elle-même.
- AoE réellement implémentées:
  - `0` single
  - `1` circle = diamant Manhattan
  - `2` square
  - `3` cross orthogonale
  - `4` X diagonale
  - `5` line orientée par rapport au lanceur
  - `6` ligne perpendiculaire centrée
- `aoeShape = 7/8` (`Cone`, `ConeReverse`) sont déclarés dans `SkillScript.gd` mais non implémentés dans `skillCell.gd` ni trouvés dans les skills lues.

### Dégâts / soin / push / mort
- Dégâts:
  - multiplicateur caster `dmgModMltGeneral * dmgModProgression`
  - multiplicateur cible `dmgReceiveMltGeneral`
  - bonus directionnel: dos `1.25`, côté `1.1`, face `1.0`
- Soin: retire de `hp_loss`, sans dépasser la vie max.
- `nonLethal` laisse la cible à `1 HP`.
- Push:
  - avance case par case jusqu’au blocage
  - si collision et valeur `dmg` fournie, dégâts de collision = `base * cases manquantes`
  - la cible bloquante devant peut aussi prendre les dégâts de collision
- Défaite générique: tous les joueurs morts.

### Victoire / défaite
- Victoire boss: tous les ennemis `boss = true` sont morts.
- Victoire comptée: `currentDead == deathtowin`, mais ce champ est peu ou pas renseigné dans les scènes lues.
- Plusieurs maps injectent des scripts de scène pour vagues, trophées et conditions spécifiques.
- Conclusion: côté Unity, la victoire doit être une couche “scenario rules”, pas une règle codée en dur dans le moteur.

### IA minimale à porter d’abord
- Mêlée simple (`Sword Pirate`): tente son skill principal, puis dépense le MP restant pour s’approcher.
- Distance simple (`Musket Marine`): tente son skill principal, recule si un joueur est à moins de `8` cases, sinon avance.
- Boss scriptés (`Mihawk`, `Buggy`) doivent venir après le socle.

## Ne pas recopier tel quel

- Le god object `Battle Mechanics.gd`.
- Les ids magiques du `TileMap` pour occupation/obstacle.
- Les scripts inline dans les `.tscn` pour l’IA ou les skills.
- Le modèle `ap_used/mp_used` si on peut exposer directement `CurrentAP/CurrentMP`.
- Le tick de cooldown actuel: logique répartie entre `turnEnd` et `endPlayersTurn`, fragile et potentiellement incohérente.
- Le couplage direct gameplay <-> UI (`SkillButton`, `Area`, `BattleMechanics`).
- Les noms d’AoE `VLine/HLine`, qui ne décrivent pas proprement le comportement réel.

## Ambiguïtés à confirmer

- Sémantique cible des cooldowns: “fin du tour du porteur” ou “fin du round allié” ?
- Règle officielle de victoire sur les maps non-boss.
- Les stats `weakness` / `resist` sont exportées sur `Entity` mais non vues dans la résolution de dégâts lue.
- Les AoE `Cone` et `ConeReverse` sont-elles abandonnées ou prévues plus tard ?
- Les grosses unités `3x3` doivent-elles être dans le scope MVP Unity ou laissées pour une itération 2 ?
