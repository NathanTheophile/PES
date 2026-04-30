# Skills Porting Backlog

Backlog court, orienté “valider les systèmes”, pas “porter tout le roster”.

## Ordre recommandé

| Priorité | Skill | Ce que ça valide | Notes |
|---|---|---|---|
| P0 | `Musket Shot` | portée longue, `linear`, LoS, single target | Bon test de base pour projectile sans AoE |
| P0 | `Rubber Bazooka` | mêlée linéaire, dégâts + push + collision | Très bon test du resolver de push |
| P0 | `Gunpowder Star` | projectile + `Circle` AoE | Valide centre AoE + dégâts multi-cibles |
| P0 | `Lunch` ou `Cura Fervente` | self-heal / ally-heal | Valide `range 0`, soin, limites d’usage |
| P0 | `Run Away!` | buff de mobilité sur le tour courant | Valide le modèle `MP` runtime |
| P1 | `Tatsu Maki` | AoE centrée sur soi + refund d’AP sur kill | Bon test d’événements `on kill` |
| P1 | `Caltrop Hell` | glyphes/pièges persistants | À faire après le cœur combat |
| P1 | `Call Reinforcements` | invocation | À garder séparé du MVP si besoin |
| P1 | `Purrsuit` / `Effortless Pursuit` | auto-déplacement offensif | Utile une fois le déplacement forcé propre |

## Hors MVP conseillé

- Boss scripts custom inline dans les `.tscn` (`Mihawk`, certains patterns `Buggy`)
- Phases boss complexes
- Unités `big` si elles ne sont pas nécessaires à la première vertical slice
- `Cone` / `ConeReverse` tant que leur design réel n’est pas confirmé

## Définition de done par skill

- La portée et les cellules ciblables correspondent au design Unity retenu.
- Les coûts `AP/MP`, limites d’usage et cooldown sont respectés.
- Les effets gameplay sont découplés des VFX.
- La skill fonctionne côté joueur et côté IA simple si nécessaire.
