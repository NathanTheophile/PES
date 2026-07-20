# Combat Rules Checklist

Checklist de portage combat. Cocher seulement quand le comportement Unity est vérifié en jeu, pas seulement codé.

## Déplacement

- [ ] Déplacement orthogonal uniquement.
- [ ] Coût de déplacement = nombre exact de cases traversées.
- [ ] Une case occupée bloque le pathfinding.
- [ ] Les obstacles de map bloquent le pathfinding.
- [ ] Une unité non encore engagée peut être désélectionnée et une autre unité alliée peut être choisie.
- [ ] Une unité `big` utilise un footprint `3x3`.
- [ ] Le push ignore ou refuse proprement les unités `big`.

## Ressources

- [ ] Chaque unité possède un budget d’`Energy` et de `Mobility` par tour.
- [ ] Fin de tour: reset du budget consommé.
- [ ] Les compétences consomment de l’`Energy` à l’utilisation.
- [ ] Les compétences peuvent rendre ou retirer de l’`Energy` ou de la `Mobility` sur le tour courant.
- [ ] `usePerTurn`, `usePerTarget`, `cooldown` sont validés par le runtime, pas par l’UI seule.

## Turn Flow

- [ ] Tour joueur: chaque unité jouable agit au plus une fois.
- [ ] Tour ennemi: ordre déterministe simple.
- [ ] Le round s’incrémente correctement selon `enemyfirst`.
- [ ] Fin de tour d’unité possible même sans dépenser toute son `Energy` ou sa `Mobility`.
- [ ] La phase ennemie démarre automatiquement quand tous les alliés ont joué.

## Targeting

- [ ] Portée calculée en Manhattan.
- [ ] `linear` limite aux axes X/Y.
- [ ] `rangeMin = 0` permet les skills self/centre sur soi.
- [ ] La visibility bloque sur unités et obstacles.
- [ ] Une cellule sans visibility est previewée comme invalide ou non ciblable.

## AoE

- [ ] `Single`
- [ ] `Circle` diamant Manhattan
- [ ] `Square`
- [ ] `Cross`
- [ ] `X`
- [ ] `Forward line` orientée par le lanceur
- [ ] `Perpendicular line` centrée
- [ ] Les shapes non supportées sont explicitement refusées ou non exposées

## Résolution

- [ ] Bonus de dégâts directionnels: face `1.0`, côté `1.1`, dos `1.25`.
- [ ] Les dégâts mettent à jour la `Health` restante sans race condition visuelle.
- [ ] Le soin ne dépasse pas la vie max.
- [ ] `nonLethal` laisse la cible à `1 Health`.
- [ ] Le push avance case par case jusqu’au blocage.
- [ ] Les collisions de push infligent les dégâts attendus.
- [ ] Les summons occupent correctement une case.
- [ ] Les téléportations/jumps vérifient l’occupation avant déplacement.

## Mort / Fin de combat

- [ ] Défaite quand tous les joueurs sont morts.
- [ ] Victoire boss quand tous les `boss` sont morts.
- [ ] Les unités `counts = false` ne comptent pas pour la victoire standard.
- [ ] Les règles de victoire de scénario sont branchées séparément du moteur.

## IA MVP

- [ ] Ennemi mêlée: cast si possible, sinon avance.
- [ ] Ennemi distance: cast si possible, recule si un joueur est trop proche.
- [ ] L’IA choisit une case atteignable avant de caster.
- [ ] L’IA n’a pas besoin des patterns boss custom pour la première milestone.

## Scénario / Hors moteur

- [ ] Vagues de renfort gérées au niveau scénario.
- [ ] Trophées/objectifs annexes gérés au niveau scénario.
- [ ] Boss patterns spéciaux gérés au niveau scénario.
- [ ] Les pièges/glyphes sont gérés comme effets de plateau persistants.

## Confirmations produit

- [ ] Politique finale des cooldowns validée.
- [ ] Scope MVP confirmé pour unités `big`.
- [ ] Scope MVP confirmé pour summons/glyphes.
- [ ] Scope MVP confirmé pour boss scripts complexes.
