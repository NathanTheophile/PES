# GDD — TacticalPort / PES

## 0. Statut du document

`GDD.md` est la source de vérité unique du produit, du design, de l’état technique courant et du backlog de TacticalPort.

- **Dernière consolidation :** 14 juillet 2026.
- **Statut produit :** prototype vertical slice PvP jouable en réseau.
- **Projet :** TacticalPort, portage Unity d’une ancienne base Godot appelée One Piece Tactics.
- **Identité cible :** jeu original dans un univers d’aventure maritime et de piraterie, indépendant de toute licence existante.
- **Plateformes prioritaires :** PC et Android.

Les sections de design décrivent les décisions stabilisées. La section « État actuel » décrit uniquement ce qui existe à la date de consolidation. La dernière section liste les écarts connus et décisions encore ouvertes.

---

## 1. Vision générale

TacticalPort est un tactical PvP compétitif au tour par tour sur grille isométrique. Chaque joueur prépare une équipe personnalisée de trois personnages, puis affronte une équipe adverse dans des combats courts, lisibles et rejouables.

L’expérience repose sur la maîtrise des personnages, la construction de synergies, l’exécution de combos, l’adaptation pendant le combat et l’anticipation adverse. Une composition définit un plan de jeu et des opportunités, mais ne doit pas décider automatiquement de l’issue du match.

Le jeu vise une sensation de puissance et de satisfaction stratégique. Les règles et informations utiles doivent rester inspectables, sans que l’interface recommande automatiquement le meilleur coup.

Le mode principal est le PvP. Le PvE reste un support limité au tutoriel, à l’entraînement et à des défis scénarisés courts.

### Piliers

1. **Maîtrise tactique :** la victoire vient principalement de l’utilisation correcte d’une équipe préparée.
2. **Adaptation :** le combat doit forcer le joueur à réviser son plan et exploiter les erreurs adverses.
3. **Combos lisibles :** les synergies demandent placement, timing et prise de risque ; elles ne sont pas automatiques.
4. **Anticipation :** portées, menaces, états et fenêtres de puissance doivent pouvoir être comprises.
5. **Équilibrage par compositions :** l’équilibrage s’analyse à l’échelle des matchups et de la méta, pas seulement personnage par personnage.

### Lexique officiel anglais-français

Ce lexique définit les termes produit, UI, design et documentation. Le code, les assets et les textes destinés au joueur emploient les termes anglais ci-dessous. Les anciens noms ne subsistent que dans les références historiques explicitement signalées.

| English | Français | Définition et règle d’usage |
|---|---|---|
| Health | Vitalité | Ressource qui maintient une unité en vie. Employer `Current Health` / `Vitalité actuelle` et `Max Health` / `Vitalité maximale`. Ne plus afficher HP ou PV. |
| Energy | Énergie | Ressource dépensée pour utiliser des compétences pendant une activation. Remplace Action Points, AP et PA. |
| Mobility | Mobilité | Ressource dépensée pour se déplacer sur la grille pendant une activation. Remplace Movement Points, MP et PM. |
| Velocity | Vélocité | Statistique utilisée uniquement pour déterminer l’équipe qui commence. Elle ne représente ni la mobilité ni une vitesse physique. Remplace Initiative. |
| Skill | Compétence | Action active équipée et utilisable en combat. Terme retenu à la place de Spell / Sort. Le nom technique `SkillDefinition` reste cohérent. |
| Passive | Passif | Effet équipé qui modifie durablement le style de jeu d’un personnage. |
| State | État | Effet gameplay temporaire ou persistant appliqué à une unité. |
| Buff / Debuff | Bonus / Malus | État favorable ou défavorable. |
| Dispel | Dissipation | Retrait d’un ou plusieurs états dissipables. |
| Stack | Cumul | Niveau cumulé d’un état ou d’une mécanique. |
| Damage / Resistance | Dommages / Résistance | Vitalité retirée par un effet offensif et réduction en pourcentage des dommages reçus. |
| Melee / Ranged | Mêlée / Distance | Types de dommages et de résistances à courte ou longue portée. |
| Wear | Usure | Réduction de la vitalité maximale provoquée par les dommages de compétences. |
| Life Steal | Vol de vie | Soin du lanceur calculé à partir des dommages réellement infligés. |
| Healing | Soin | Restauration de vitalité, limitée par la vitalité maximale actuelle. |
| Critical Hit | Coup critique | Résultat exceptionnel augmentant les dommages d’une compétence. |
| Cooldown | Recharge | Nombre de tours avant qu’une compétence puisse être réutilisée. |
| Range | Portée | Distance minimale et maximale d’utilisation d’une compétence. |
| Area of Effect | Zone d’effet | Ensemble des cases affectées par une compétence. L’abréviation technique AoE reste admise. |
| Line of Sight | Ligne de vue | Condition de visibilité requise entre lanceur et cible. |
| Character | Personnage | Élément jouable du roster, possédant ses données, compétences et passifs. |
| Unit | Unité | Entité présente dans le runtime de combat : personnage, invocation ou entité neutre. |
| Team | Équipe | Groupe de personnages contrôlé par un joueur. |
| Roster | Roster | Ensemble des personnages jouables disponibles dans le jeu. |
| Team Preset | Preset d’équipe | Configuration sauvegardée contenant personnages, ordre, compétences, passifs et allocations. |
| Team Building | Création d’équipe | Préparation et modification d’un preset. `Deckbuilding` reste un nom technique historique, mais n’est plus le terme produit privilégié. |
| Match / Combat | Partie / Combat | La partie désigne la session complète ; le combat désigne sa phase gameplay sur la carte. |
| Deployment | Déploiement | Phase simultanée et cachée de positionnement initial. Remplace Placement dans les nouveaux textes produit. |
| Turn | Tour | Activation d’une unité. |
| Round | Cycle | Séquence complète dans laquelle chaque unité éligible reçoit un tour. |
| Turn Order | Ordre des tours | Ordre déterministe des activations. `Timeline` désigne uniquement sa représentation visuelle. |
| Caster / Target | Lanceur / Cible | Unité qui utilise une compétence et unité ou case visée. |
| Ally / Enemy / Neutral | Allié / Ennemi / Neutre | Relation d’une unité avec le lanceur ou le joueur local. |
| Grid / Cell | Grille / Case | Structure tactique de la carte et emplacement élémentaire qui la compose. |
| Summon | Invocation | Unité créée par une compétence. |
| Glyph / Trap / Hazard | Glyphe / Piège / Danger | Effets localisés sur la grille. Un danger est une menace environnementale ou télégraphiée générique. |
| Victory / Defeat / Draw | Victoire / Défaite / Égalité | Résultats possibles d’une partie. |

Le code first-party, les données et l’interface utilisent désormais les identifiants anglais `Health`, `Energy`, `Mobility`, `Velocity`, `Skill` et `Unit`. Les anciens termes ne sont admis que dans les références historiques explicitement citées.

---

## 2. État actuel au 13 juillet 2026

### Socle technique

- Unity `6000.4.5f1`.
- URP 3D, grille et environnement 3D avec TileWorldCreator.
- Input System, UGUI/TextMeshPro et DOTween.
- Architecture répartie en assembly definitions par domaine.
- Données de gameplay principalement exposées par ScriptableObjects.

La transition depuis le prototype URP 2D/Tilemaps est effectuée. La verticalité visuelle existe dans les cartes 3D, mais aucune règle compétitive de hauteur n’est encore stabilisée.

### Boucle jouable

1. Bootstrap des services.
2. Menu principal.
3. Création, validation et sauvegarde d’un preset d’équipe.
4. Lancement d’un match rapide, d’un lobby privé ou d’un chemin de debug réseau.
5. Création d’un manifest avec joueurs, slots, carte, preset, personnages, compétences, passif et allocations de statistiques.
6. Connexion PurrNet selon le mode de transport.
7. Résolution de la scène de combat à partir du `MapId`.
8. Déploiement des équipes, confirmation des joueurs puis combat.
9. Alternance des tours jusqu’à la victoire ou la défaite.

### Matchmaking et réseau

- UGS Authentication pour l’identité joueur.
- UGS Matchmaker pour le match rapide.
- UGS Lobby + Relay pour les parties privées.
- PurrNet pour la connexion, l’attribution des slots, les commandes de combat et leur réplication.
- UDP pour les serveurs locaux ou dédiés ; UTP Relay pour les lobbies privés.
- EdgeGap pour le chemin serveur dédié du match rapide et du futur ranked.

La boucle EdgeGap a été validée fonctionnellement en prototype : matchmaking, ouverture de session, lancement du combat et fermeture de session ont fonctionné de bout en bout. Cela ne signifie pas encore que le chemin est prêt pour la production : reconnexion, supervision, exploitation, déploiement et gestion complète des incidents restent à durcir.

La synchronisation est command-based. L’autorité valide les commandes, les exécute puis diffuse les commandes acceptées. Un checksum détecte les divergences. Il n’existe pas encore de rollback, de snapshot complet ni de resynchronisation automatique.

### Deckbuilding

Le créateur d’équipe gère aujourd’hui :

- trois slots ordonnés ;
- sélection sans doublon de personnage ;
- six compétences équipées par personnage ;
- un passif équipé ;
- allocations de statistiques individuelles ;
- validation avant sauvegarde ;
- persistance de plusieurs presets ;
- transmission du build complet dans le manifest réseau.

### Contenu actuel

Le roster de production est volontairement vide afin de repartir sur des personnages conçus selon l'architecture et les règles stabilisées.

Des unités, compétences, états et adversaires simples restent présents comme contenu de test. Ils ne définissent pas le roster cible.

`S_Map_Alpha_1` est la première carte 3D jouable. Son identifiant canonique est `alpha-1`. Les anciens identifiants `poutch`, `custom-direct` et `custom-relay` restent acceptés comme alias de compatibilité.

---

## 3. Prochain jalon produit

La prochaine vertical slice validée vise :

- huit personnages jouables et suffisamment polis ;
- dix compétences et deux passifs fonctionnels pour chacun ;
- deux cartes PvP symétriques ;
- création d’équipe complète et persistante ;
- lobby privé et match rapide fonctionnels ;
- combat 3v3 jouable de bout en bout sur PC et Android ;
- déploiement simultané caché et timers PvP ;
- validation réseau et PlayMode sur deux clients.

Ce jalon représente 80 compétences et 16 passifs. Le contenu doit être intégré personnage par personnage, avec validation et nettoyage du kit précédent avant d’élargir le roster.

---

## 4. Règles du combat

### Ressources et actions

Chaque unité possède de la vitalité, de la mobilité, de l’énergie, une vélocité, une équipe, des compétences et des états.

- La mobilité sert aux déplacements orthogonaux case par case.
- L’énergie sert à utiliser les compétences.
- Les dommages de base ne comportent pas de variation aléatoire.
- La condition de victoire standard est l’élimination de l’équipe adverse.

Le socle data-driven supporte dommages, soin, poussée, téléportation, échange de position, invocation, glyphes, pièges, états, zones, atténuation des dommages, ligne de vue, recharges et limites d’utilisation.

Le runtime supporte également l’usure de la vitalité maximale, le vol de vie et des modificateurs généraux de dommages et de résistance. Ces règles sont déterministes et intégrées au checksum réseau.

La durée cible d’un combat standard est de 15 à 20 minutes. Un tour PvP doit durer environ 30 à 45 secondes.

### Alternance, slots et vélocité

Le format est une alternance stricte entre les joueurs. Pour un 3v3 : slot 1 du premier joueur, slot 1 du second, slot 2 du premier, slot 2 du second, slot 3 du premier, slot 3 du second, puis nouveau round.

L’ordre interne d’une équipe vient exclusivement des slots choisis dans le créateur d’équipe. Le déploiement sur la carte ne change pas cet ordre.

L’équipe qui commence est déterminée ainsi :

1. comparaison de la somme des vélocités effectives des trois personnages, allocations comprises ;
2. en cas d’égalité, comparaison des vélocités individuelles triées de la plus haute à la plus basse ;
3. en cas de miroir parfait, pile ou face unique choisi par l’autorité du match et partagé à tous les clients.

La RNG est donc limitée au seul cas impossible à départager par les builds. Aucun client ne tire localement le premier joueur.

La vélocité ne modifie pas l’ordre interne, ne crée pas d’ordre des tours dynamique et ne permet pas à une unité de rejouer plus vite. Aucun bonus ou malus de vélocité n’est prévu pendant le combat.

### Rythme et létalité

- Une unité peut mourir après environ un à trois tours de focus selon son profil.
- Le burst tour 1 doit être évité sans rendre le premier tour passif.
- Le soin existe mais ne doit pas annuler durablement la pression adverse.
- Boucliers et résistances peuvent structurer certaines compositions.
- Les variations d’énergie et de mobilité pendant le combat viennent principalement des bonus et malus.

---

## 5. Équipes, personnages et builds

### Format et roster

- Format principal fixé à 3v3.
- Roster du prochain jalon : huit personnages.
- Aucun doublon dans une équipe.
- Aucun rôle formel obligatoire ; les rôles émergent des kits.
- Environ la moitié du roster doit rester facile à prendre en main.
- Un à deux personnages experts peuvent exister dans le premier roster.

Chaque personnage doit avoir une faiblesse exploitable, sans être réduit à un contre évident. L’équilibrage vise d’abord sa place dans les compositions et matchups.

### Compétences et passifs

Chaque personnage possède exactement :

- un pool de dix compétences actives ;
- six compétences équipées pour le combat ;
- deux passifs disponibles ;
- un passif équipé pour le combat.

Les compétences ne possèdent pas de variantes sélectionnables supplémentaires dans la création d’équipe. Des variantes runtime sont autorisées lorsqu’elles proviennent d’une mécanique clairement expliquée. Les systèmes réutilisables doivent employer une terminologie générique et ne pas intégrer le vocabulaire d’un personnage particulier.

Les passifs doivent modifier le style de jeu, les priorités tactiques ou les fenêtres de puissance. Ils peuvent modifier ponctuellement une règle de base, mais ne doivent pas rendre le personnage illisible ni invalider les règles communes du jeu.

### Allocations de statistiques

Chaque `UnitDefinition` définit un capital individuel de points, généralement compris entre 100 et 150. Le joueur peut dépenser moins que ce capital mais ne peut jamais le dépasser.

Coûts de référence :

- 1 point par point de vitalité ;
- 1 point par point de vélocité ;
- 25 points par point d’énergie ;
- 20 points par point de mobilité ;
- 5 points par point de dommages mêlée ou distance ;
- 5 points par point de résistance mêlée ou distance.

Ce budget ne constitue pas un coût de composition. Les personnages, l’énergie, la mobilité et la vitalité ne sont pas soumis à un coût global d’équipe.

### Presets

Un preset contient les trois personnages, leur ordre, leurs compétences, leur passif et leurs allocations. Il est verrouillé dès l’entrée dans un flux réseau.

Plusieurs presets peuvent être sauvegardés. Leur nombre maximal et leur présentation finale restent à définir. Le nom interne d’un preset sert à la persistance ; la saisie manuelle d’un nom par le joueur n’est pas prioritaire.

Le créateur d’équipe affiche les erreurs bloquantes, pas des recommandations subjectives de composition.

---

## 6. Dégâts, critiques et états

Les dommages mêlée/distance, les dommages généraux et les résistances correspondantes utilisent des pourcentages.

- `100` en dommages représente la valeur neutre.
- Les dommages généraux d’un personnage ou d’un état s’ajoutent aux dommages mêlée ou distance utilisés par la compétence.
- La résistance générale s’ajoute à la résistance mêlée ou distance correspondant à la compétence.
- Une résistance peut devenir négative.
- La résistance finale est plafonnée à `100%` pour éviter des dommages négatifs.
- Les états appliquent des modificateurs signés.

Formules de référence :

- dommages sortants : `base × max(0, dommages typés + dommages généraux + modificateurs typés et généraux) / 100` ;
- dommages reçus : `dommages entrants × (100 - min(100, résistances typées + générales et leurs modificateurs)) / 100`.

Les dommages et résistances généraux sont définis par les personnages et les états. Ils ne sont pas achetables dans les allocations de presets.

### Usure et vol de vie

Toutes les unités possèdent une usure universelle, permanente et indissipable de `5%`. Seuls les dommages provenant directement d’une compétence appliquent cette usure. Collisions, glyphes et dangers n’usent pas la vitalité maximale.

L’usure retire à la vitalité maximale actuelle un pourcentage de la vitalité réellement perdue après résistances et limitation des dommages excédentaires. Les fractions sont conservées entre les impacts afin que les petits dommages produisent à terme la même usure. Les états peuvent ajouter de l’usure ; le taux total est additif, ne peut jamais descendre sous `5%` et est plafonné à `50%`.

Un état d’usure appliqué par une compétence de dommages prend effet après son impact. Il influence donc uniquement les dommages suivants. Les soins sont limités par la vitalité maximale actuelle, qui ne peut pas descendre sous `1`.

Une compétence peut posséder le vol de vie. Elle soigne alors son lanceur de `50%`, arrondi à l’entier inférieur, de la vitalité réellement retirée aux unités ennemies par cette exécution. Les dommages aux alliés, au lanceur, aux unités neutres et les dommages excédentaires ne contribuent pas au soin. Sur une compétence de zone, les dommages valides sont additionnés avant d’appliquer le soin.

Les coups critiques sont une surprise occasionnelle et un élément d’identité pour certaines compétences. Ils augmentent uniquement les dommages. Leur taux doit être visible sur les compétences du joueur et peut être modifié par des bonus ou malus.

Les états cumulables constituent un système central. Les bonus et malus sont visibles sur les unités, avec détails accessibles. La dissipation doit rester rare et précieuse.

---

## 7. Déploiement et révélation d’informations

La phase de déploiement PvP dure 45 secondes.

- Les deux joueurs placent simultanément leurs trois personnages.
- Les positions adverses restent cachées pendant le déploiement.
- Les positions sont révélées lorsque les deux joueurs sont prêts ou à expiration du timer.
- Un joueur peut annuler son ready tant que l’autre joueur n’est pas prêt.
- L’ordre des slots est verrouillé avant le déploiement.
- L’ordre complet des tours reste visible.
- Les personnages et passifs adverses sont visibles avant le combat.
- Les compétences adverses ne sont pas révélées pendant le déploiement.

Le déploiement initial ne doit pas favoriser structurellement une équipe. Les zones de déploiement sont fixes et symétriques pour chaque carte.

---

## 8. Cartes, grille et verticalité

Les cartes PvP sont symétriques et conçues pour le 3v3. Elles doivent comporter plusieurs zones importantes plutôt qu’un centre dominant unique.

Les obstacles structurent mouvement et lignes de vue. Certains peuvent bloquer le mouvement, la ligne de vue ou les deux, à condition que cette distinction soit immédiatement lisible.

La distance initiale doit permettre une menace modérée au tour 1 et favoriser les engagements forts à partir du tour 2.

Le prochain jalon comporte deux cartes compétitives. La taille standard, la densité d’obstacles et la seconde carte restent à définir par les playtests.

La verticalité gameplay est reportée après le jalon huit personnages/deux cartes. Jusqu’à cette validation, la hauteur reste principalement visuelle et ne doit pas modifier portée, déplacement ou ligne de vue. Une verticalité légère pourra ensuite être testée sans transformer le jeu en tactical 3D complexe.

Les dangers neutres ne sont pas centraux. Glyphes, pièges et zones dangereuses doivent principalement provenir des kits des personnages.

---

## 9. Modes de jeu, réseau et progression

### Modes prioritaires

1. Lobby privé non classé avec code d’invitation, Lobby et Relay.
2. Match rapide non classé avec Matchmaker et serveur dédié EdgeGap.
3. Debug local ou direct IP réservé au développement.

Tous les modes partagent le même runtime de combat. Les modes privés utilisent les règles standard et ne reportent aucun résultat ranked.

Le ranked arrivera seulement après stabilisation du roster, des cartes, de l’équilibrage, du réseau et de l’exploitation serveur. Il nécessite un serveur autoritaire fiable. Aucun MMR n’est nécessaire pour les premiers playtests publics.

### Progression

La progression ne procure aucun avantage compétitif. Les personnages et options de gameplay principales restent accessibles librement.

La progression cible repose sur :

- cosmétiques ;
- classement ranked ;
- progression de compte ;
- maîtrise des personnages sans bonus statistique.

### Robustesse réseau cible

Le serveur ou host reste l’autorité des commandes. Le ranked utilisera uniquement le chemin serveur dédié. Les politiques de déconnexion, reconnexion, abandon, resynchronisation, late join et spectateur restent à formaliser.

---

## 10. Interface, lisibilité et plateformes

Les informations permanentes sont : vitalité actuelle et maximale, taux d’usure effectif, énergie, mobilité, ordre des tours, états et passif actif. Toutes les compétences équipées de l’unité active sont affichées avec coût, portée, recharge et limites utiles.

L’interface doit fonctionner sur PC et Android :

- souris/clavier sur PC ;
- interaction tactile sur Android ;
- aucune information essentielle accessible uniquement au survol ;
- les tooltips doivent disposer d’un équivalent par tap, appui long ou panneau de détail ;
- tailles de cibles, densité de texte et performances doivent être adaptées au mobile.

L’UI explique les règles et invalidités sans recommander le meilleur coup. La prévisualisation affiche les dommages normaux et critiques des compétences du joueur.

Le thème global reste configurable via `ThemeDefinition` et `ThemeManager`. Les compétences utilisent des catégories visuelles cohérentes, notamment dommages, utilitaire et soin.

---

## 11. PvE et intelligence artificielle

Le PvE sert à apprendre les règles et tester les kits. Il prend la forme de tutoriels progressifs, défis scénarisés et cibles d’entraînement.

L’IA peut choisir des compétences, évaluer les cibles, prendre en compte dommages, soin, élimination, zones, sécurité et distance, puis se repositionner. Ce système reste un outil de support du PvP et non le contenu principal.

---

## 12. Écarts connus et backlog

### Priorité immédiate

- Concevoir et réaliser les huit personnages complets du prochain jalon.
- Concevoir et intégrer la seconde carte compétitive.
- Implémenter le masquage visuel effectif des positions adverses pendant le déploiement.
- Implémenter le timer de déploiement de 45 secondes.
- Implémenter et valider le timer de tour PvP.
- Vérifier l’UX tactile complète du menu, de la création d’équipe, du déploiement et du combat.

### Réseau et production

- Politique de déconnexion, abandon et reconnexion.
- Resynchronisation après mismatch de checksum.
- Validation durable des builds serveur Linux et déploiements EdgeGap.
- Supervision, métriques, logs d’exploitation et gestion des incidents.
- Late join et mode spectateur éventuels.

### Design encore ouvert

- Nom final du jeu et direction artistique définitive.
- Identité des sept personnages restant dans le roster du jalon.
- Thème, dimensions et règles exactes de la seconde carte.
- Valeur définitive du timer de tour entre 30 et 45 secondes.
- Formule finale des coups critiques et liste des compétences concernées.
- Limites de cumuls, familles d’états et règles précises de dissipation.
- Nombre maximal et présentation finale des presets.
- Règles de verticalité à expérimenter après le prochain jalon.
- Date, MMR et conditions d’ouverture du ranked.

Toute nouvelle décision doit modifier sa section de design correspondante et retirer l’entrée associée de ce backlog afin d’éviter les doublons et contradictions.
