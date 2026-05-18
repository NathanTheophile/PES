# GDD — TacticalPort / PES

## 0. Statut du document

Ce document est la source de vérité temporaire du Game Design Document tant que la structure Notion n'est pas prête. Il doit rester lisible, sobre et maintenable. Les sections ci-dessous sont consolidées à partir des réponses validées.

**Statut actuel :** prototype vertical slice technique jouable en réseau pour playtests contrôlés.

**Projet actuel :** TacticalPort, prototype Unity en cours de portage depuis une base Godot appelée One Piece Tactics.

**Orientation produit :** tactical PvP compétitif au tour par tour, avec PvE limité à des tutoriels et défis scénarisés.

---

## 1. Vision générale

Le projet est un jeu tactique au tour par tour sur grille isométrique. Chaque joueur prépare une petite équipe personnalisable avant le combat, puis affronte une équipe adverse dans des combats courts, compétitifs et rejouables.

L'expérience recherchée repose sur la maîtrise des personnages, l'exécution de combos, l'adaptation pendant le combat, l'anticipation adverse et la connaissance progressive de la méta.

Le jeu vise principalement une sensation de puissance et de satisfaction stratégique. La victoire doit venir de la capacité du joueur à comprendre son équipe, exploiter ses synergies, lire les menaces adverses et prendre de bonnes décisions tour après tour.

Le projet s'inspire fortement de Dofus Arena et, dans une moindre mesure, de Krosmaster, tout en devant rester conçu comme un système original pouvant être séparé de toute licence existante.

---

## 2. État actuel du prototype

Le projet est actuellement un prototype Unity nommé TacticalPort, développé sous Unity 6000.3.8f1. Il utilise URP 2D, Tilemaps, Input System, UGUI/TextMeshPro, et DOTween est présent dans le projet.

Le projet est en cours de portage depuis une base Godot nommée One Piece Tactics. L'objectif technique est de passer vers une approche URP 3D tout en conservant la logique actuelle, en ajoutant une verticalité légère et en facilitant la gestion de la profondeur des sprites.

La vertical slice technique est fonctionnelle : le socle de combat est avancé et le matchmaking réseau permet désormais de lancer des combats entre deux clients dans un cadre de test contrôlé.

Le prototype dispose d'un socle réseau structurant : services UGS pour l'identité joueur, Matchmaker pour le match rapide, Lobby + Relay pour les parties privées, PurrNet pour la connexion et la réplication des commandes de combat. Le chemin EdgeGap / serveur dédié est préparé côté architecture et Cloud Code, mais reste à valider comme chemin de production complet.

Le contenu gameplay, l'équilibrage, les règles de scénario, le pool de cartes compétitives et la validation PlayMode restent à formaliser.

---

## 3. Boucle de jeu actuelle

Le flow jouable actuel principal est :

1. Scène de bootstrap.
2. Menu principal.
3. Sélection et sauvegarde locale d'équipe.
4. Lancement d'un match rapide, d'un lobby privé, ou d'un chemin debug local/direct IP.
5. Création d'un contexte de match avec identités joueurs, slots TeamA/TeamB, manifest de composition et mode de connexion.
6. Connexion PurrNet selon le mode : UDP pour serveur local/dédié, UTP Relay pour lobby privé.
7. Chargement de la scène de combat S_Poutch lorsque les compositions nécessaires sont connues.
8. Phase de placement des unités sur les cases de spawn autorisées.
9. Ready des deux joueurs, puis verrouillage du placement.
10. Début du combat et alternance des tours.
11. Actions possibles : déplacement, compétence, fin de tour.
12. Fin de combat avec victoire ou défaite.

Le flow local/offline contre IA reste utile pour le debug du combat, mais il n'est plus le flow de référence du prototype réseau.

---

## 4. Piliers de design

### Pilier 1 — Maîtrise tactique d'une équipe préparée

Le joueur construit une équipe avant le combat, mais la victoire doit principalement venir de sa capacité à maîtriser cette équipe en situation réelle. La composition définit un style de jeu, des forces, des faiblesses et des opportunités de synergie, mais elle ne doit pas décider seule de l'issue du combat.

### Pilier 2 — Adaptation en combat

Le combat doit créer des situations qui forcent le joueur à changer de plan, punir une erreur adverse, sauver une unité exposée, exploiter une opportunité ou renoncer à un combo initialement prévu.

### Pilier 3 — Combos et synergies lisibles

Les combos doivent être importants, mais pas automatiques. Un bon combo doit demander du placement, du timing, une lecture adverse et une prise de risque raisonnable.

### Pilier 4 — Anticipation adverse

Le joueur doit apprendre à identifier les menaces, les portées, les zones de danger, les combos adverses et les timings critiques. Le jeu ne doit pas tout afficher explicitement, mais les règles doivent rester inspectables et cohérentes.

### Pilier 5 — Équilibrage par compositions et méta

L'équilibrage doit être pensé à l'échelle des compositions, des matchups et de la méta plutôt qu'à l'échelle de chaque personnage isolé. Le jeu accepte les matchups défavorables, tant qu'ils ne rendent pas la partie automatiquement perdue.

---

## 5. Core combat

Le combat repose sur un système tactique au tour par tour sur grille. Chaque unité possède des HP, MP, AP, une initiative, une équipe, des compétences et des états.

Les MP servent aux déplacements case par case. Les AP servent à lancer des compétences. Ce découpage crée des dilemmes tactiques simples à comprendre : se repositionner, attaquer, préparer un combo, économiser sa sécurité ou s'exposer pour maximiser son impact.

Le système de compétences est data-driven et supporte : dégâts, soin, push, téléportation, échange de position, invocation, glyphes, pièges, états, AoE, falloff de dégâts, contraintes de portée, ligne de vue, cooldowns et limites d'utilisation.

La condition de victoire principale est l'élimination de l'équipe adverse. Les objectifs alternatifs peuvent exister dans certains modes ou scénarios, mais ils ne constituent pas le cœur initial de l'expérience.

La durée cible d'un combat standard est de 15 à 20 minutes.


### Tour, initiative et alternance

Chaque personnage possède sa propre statistique d'initiative. L'équipe possède également une initiative totale, calculée par l'addition des initiatives des personnages qui la composent.

Le système final de tour repose sur une alternance stricte entre les joueurs. Dans un format 3v3, l'ordre cible est : personnage 1 du joueur 1, personnage 1 du joueur 2, personnage 2 du joueur 1, personnage 2 du joueur 2, personnage 3 du joueur 1, personnage 3 du joueur 2, puis nouveau round.

Le premier joueur à commencer est normalement déterminé par l'initiative totale de son équipe. Pour les premières versions du jeu, cette règle sera simplifiée : le joueur qui commence sera déterminé par un pile ou face. L'alternance restera cependant identique.

L'initiative sert uniquement à déterminer le premier joueur au lancement du combat. Elle ne sert pas à modifier l'ordre interne des tours, à permettre à une unité de rejouer plus vite ou à créer une timeline dynamique.

Il n'existe donc pas de buffs ou debuffs d'initiative dans la direction actuelle. Chaque personnage joue une fois par round, et aucun personnage ne peut jouer plusieurs fois avant qu'un autre personnage ait eu son tour normal.

Le placement initial ne doit pas influencer le premier tour. Les cartes doivent être équilibrées pour éviter qu'une équipe dispose d'un avantage structurel de placement au démarrage.

Le système doit éviter le burst tour 1, sans rendre le premier tour mou ou inutile. Le tour 1 peut permettre la prise de position, la préparation d'un plan, l'ouverture d'une ligne de menace ou un premier engagement modéré, mais il ne doit pas favoriser des éliminations immédiates trop difficiles à anticiper.

---

## 6. Modes de jeu et progression

Le mode principal du jeu est le PvP. Les formats visés sont : lobby privé, duel rapide non classé et ranked 1v1.

Le PvE existe comme support du PvP, principalement sous forme de tutoriels progressifs et de défis scénarisés. Il sert à apprendre les règles, tester les personnages, présenter des situations tactiques et approfondir la connaissance des kits.

La progression ne doit donner aucun avantage compétitif direct. Les personnages et options de gameplay principales doivent être accessibles librement.

La progression repose sur :

- cosmétiques ;
- classement ranked ;
- progression de compte ;
- maîtrise des personnages sans avantage statistique.

La sélection d'équipe est libre. Les personnages, PA, PM et points de vie ne sont pas encadrés par un coût de composition. Les doublons de personnages sont interdits. L'équilibrage repose principalement sur le design des personnages, des sorts, des passifs et sur les patches d'équilibrage.

L'équilibrage compétitif repose sur : design des kits, patches réguliers, séparation des files de matchmaking, suivi de la méta et counters assumés.


### Matchmaking, ranked et modes PvP

Pour la première version jouable en ligne, les modes prioritaires sont le lobby privé et le match rapide non classé. Le lobby privé facilite les playtests contrôlés, tandis que le match rapide permet de tester l'expérience standard de confrontation en ligne.

L'architecture réseau est séparée par intention de match, mais le combat doit rester un chemin runtime unique. Le handoff cible est : services de découverte de match, `MatchRuntimeContext`, `PurrNetMatchConnector`, puis `PurrNetCombatBridge`.

Les modes réseau de référence sont :

- match rapide non classé : UGS Matchmaker, queue simple, combat en ligne standard ;
- lobby privé : UGS Lobby avec code d'invitation, UGS Relay, match non classé et non fiable pour le ranked ;
- debug local/direct IP : outil de développement uniquement, conservé pour valider PurrNet et les serveurs locaux ;
- serveur dédié EdgeGap : chemin cible pour quick match autoritaire et futur ranked, préparé mais à valider en production.

Le ranked ne doit pas être disponible dès la première version publique. Il doit arriver après un niveau d'équilibrage suffisant, lorsque le roster, les règles de combat, les cartes, le matchmaking et la stabilité réseau sont assez solides.

Dans les premières versions, le matchmaking reste simple et ne tient pas encore compte du niveau joueur, du MMR ou du rang visible. Ces systèmes pourront être ajoutés plus tard lorsque le volume de joueurs et les besoins compétitifs le justifieront.

Le ranked ne peut être ouvert que sur un chemin serveur fiable. Les parties privées, Relay et direct IP ne peuvent pas reporter de résultat ranked.

Les matchs privés utilisent les mêmes règles que le mode standard. Ils ne permettent pas de modifier librement le timer, la carte, les restrictions ou les règles de combat dans la direction actuelle. Cela évite de fragmenter l'équilibrage et simplifie les tests.

En matchmaking, la cible produit reste une carte choisie aléatoirement parmi un pool compétitif. Ce pool doit contenir des cartes symétriques, testées et adaptées au format 3v3. Dans le prototype actuel, le flow réseau charge encore S_Poutch comme scène de combat de référence.

Le joueur choisit son équipe avant de lancer le matchmaking, de créer un lobby ou de rejoindre un lobby. L'équipe est donc verrouillée pour la session dès l'entrée dans le flow réseau, et ne doit plus être modifiable pendant la recherche, le placement ou le combat.

La cible produit est de révéler avant le lancement du combat les personnages choisis, leur ordre d'activation et leur passif actif. Les sorts équipés restent cachés pendant la phase de placement et ne doivent être découverts que pendant le combat ou après le match.

---

## 7. Roster, personnages et composition

Pour une première version jouable sérieuse, le roster cible est de 8 à 10 personnages.

Le format d'équipe cible reste ouvert entre 3 et 4 personnages par équipe. Le 3v3 est considéré comme le format de référence provisoire, car il favorise la lisibilité, la durée maîtrisée et l'équilibrage. Le 4v4 pourra être testé si les combats manquent de profondeur ou si les synergies d'équipe nécessitent davantage d'espace.

Les personnages n'ont pas de rôles formels stricts. Chaque personnage possède plutôt des tendances naturelles de rôle issues de son kit de sorts : dégâts, contrôle, support, mobilité, tanking, pression, poke, setup, protection, invocation, pièges ou perturbation.

Chaque personnage doit disposer d'un pool de sorts suffisamment riche. Lors de la personnalisation d'équipe, le joueur choisit un nombre limité de sorts disponibles pour le combat. Cette approche permet de créer de la personnalisation sans donner d'avantage statistique ou verrouiller les personnages derrière une progression.

Certains personnages peuvent disposer d'une ou plusieurs passives, mais ce n'est pas nécessairement obligatoire pour tout le roster.

Il n'y a pas de coût de composition pour les personnages. Les joueurs peuvent composer librement leur équipe, tant que les règles de format sont respectées. L'équilibrage des personnages doit se faire directement par les valeurs, les sorts, les passifs, les contraintes de kit et les patches.

Des familles, factions ou tags de synergie peuvent exister, mais doivent rester légers. Ils servent à orienter les compositions sans rendre la construction d'équipe automatique ou trop rigide.


### Composition, slots et ordre des personnages

Le format de référence est désormais fixé à 3v3.

Le joueur choisit explicitement l'ordre des personnages dans l'équipe : slot 1, slot 2 et slot 3. Cet ordre détermine l'ordre d'activation dans le système d'alternance.

Pendant la phase de préparation et de placement, la timeline est déjà visible. Les deux joueurs peuvent donc voir dans quel ordre les personnages joueront avant le lancement du combat.

L'ordre des personnages est verrouillé dans le créateur d'équipe. Le joueur ne peut pas modifier cet ordre pendant la phase de placement.

Commencer le combat n'est pas considéré comme un avantage systématique. L'impact du premier tour dépend des compositions et de leur plan de jeu. L'équilibrage devra surveiller les compositions qui deviennent trop fortes lorsqu'elles commencent et ajuster l'initiative des personnages concernés si nécessaire.

Les personnages avec une initiative élevée ne paient pas cette statistique via un coût de composition, des statistiques plus faibles ou un kit moins flexible. L'initiative est séparée du coût et doit être équilibrée directement dans le design global du personnage.


### Personnages, archétypes et roster V1

Le roster V1 doit être conçu autour d'archétypes lisibles, mais avec des hybridations. Les personnages doivent être compréhensibles dans leur intention générale, sans être enfermés dans un rôle unique ou rigide.

Environ la moitié du roster doit être facile à prendre en main. Cela permet d'accueillir les nouveaux joueurs, de faciliter les premiers tests et de proposer des bases de composition simples.

Le roster V1 peut inclure 1 à 2 personnages très techniques ou experts. Ces personnages servent à créer de la profondeur, de la maîtrise et des objectifs de progression pour les joueurs avancés, sans rendre l'ensemble du roster difficile à comprendre.

Les archétypes obligatoires du roster V1 ne sont pas encore définis. Le jeu ne doit pas forcer trop tôt une checklist rigide de rôles comme tank, support, assassin ou invocateur. Les besoins seront précisés à partir des mécaniques de combat, des synergies recherchées et des premiers personnages conçus.

La difficulté des personnages peut être affichée, mais seulement dans le créateur d'équipe. Cette information doit aider le joueur à choisir, sans devenir une étiquette trop visible ou trop réductrice pendant le combat.

Chaque personnage doit avoir une faiblesse claire, mais pas trop évidente. Une faiblesse doit permettre le contre-jeu et l'équilibrage, sans rendre le personnage trivial à contrer ou trop prévisible.

Tous les personnages n'ont pas besoin d'une mécanique signature forte. Certains peuvent être plus simples. Les mécaniques identitaires peuvent souvent venir des passifs, qui orientent le style de jeu du personnage sans imposer une complexité excessive à tous ses sorts.

Les personnages sont équilibrés principalement pour leur rôle en composition. Un personnage n'a pas besoin d'être parfaitement autonome : sa valeur vient surtout de ses synergies, de ses complémentarités et de sa place dans une équipe de 3.

---

## 8. Skills, builds et personnalisation

Chaque personnage possède son propre pool de sorts et son propre choix de passif. La personnalisation ne repose pas sur une limite globale commune à toute l'équipe, mais sur des choix effectués pour chaque personnage individuellement.

Chaque personnage dispose d'un pool de sorts cible de 10 sorts. Cette valeur peut rester flexible pour de futures versions, mais elle constitue la base de design actuelle.

Pour le combat, chaque personnage équipe exactement 6 sorts actifs. Ce nombre est identique pour tous les personnages afin de préserver la lisibilité, l'équilibrage et la cohérence de l'interface.

Chaque personnage dispose de 2 passifs disponibles, parmi lesquels le joueur en choisit 1 pour le combat. Le passif est sélectionné séparément des sorts actifs et ne consomme pas de slot de sort.

Les sorts n'ont pas de variantes. Un sort correspond à une version claire et stable. La profondeur de build vient du choix des sorts équipés, pas de modifications internes des sorts.

Le joueur choisit librement ses 6 sorts actifs dans le pool du personnage. Il n'y a pas de catégories imposées, pas de minimum ou maximum par type de sort, pas d'incompatibilités entre sorts et pas de prérequis entre sorts. Les catégories peuvent éventuellement exister dans la documentation ou l'analyse de design, mais elles ne contraignent pas le deckbuilding du joueur.

Le passif choisi est indépendant des sorts disponibles. Il ne débloque, ne bloque et ne modifie pas l'accès aux sorts du personnage.

## Design des passifs

Chaque personnage possède 2 passifs disponibles et en équipe 1 seul pour le combat.

Les passifs doivent principalement modifier le style de jeu du personnage. Ils ne servent pas seulement à ajouter un bonus numérique : ils doivent orienter la manière de jouer le personnage, ses priorités tactiques, ses fenêtres de puissance ou son rôle dans une composition.

Les passifs peuvent être complexes. Leur complexité est acceptable parce qu'un seul passif est équipé par personnage et parce qu'ils sont visibles à l'adversaire dès la révélation de l'équipe. Cette complexité doit néanmoins rester lisible et documentée.

Les passifs sont visibles à l'adversaire dès la révélation de l'équipe. Ils font donc partie des informations stratégiques connues avant le placement et avant le lancement du combat.

Les passifs peuvent modifier les règles de base du personnage, mais seulement de façon limitée. Ils peuvent altérer certaines priorités, interactions, déclencheurs ou conditions de jeu, mais ne doivent pas rendre le personnage méconnaissable ou invalider son identité de base.

Les passifs peuvent créer des builds très différents pour un même personnage. C'est un objectif important : un même personnage doit pouvoir proposer plusieurs approches tactiques selon le passif choisi.

Les passifs sont équilibrés prioritairement autour du PvP. Le PvE peut s'adapter à ces choix, mais ne doit pas imposer de compromis qui affaiblissent la clarté ou l'équilibre compétitif.

Le joueur peut modifier librement son équipe avant de lancer une recherche de combat. Le créateur d'équipe cible permet de choisir les personnages, les sorts équipés et le passif sélectionné pour chaque personnage. Une fois la recherche ou le combat lancé, l'équipe est verrouillée pour la partie.

Les compositions complètes doivent pouvoir être sauvegardées sous forme de presets d'équipe. Un preset contient les 3 personnages, leur ordre de slot, leurs 6 sorts équipés et leur passif sélectionné.

Dans le prototype actuel, la sélection d'équipe implémentée reste plus limitée que la cible design : elle sauvegarde principalement les identifiants des unités choisies localement. La sélection de sorts, de passif et les presets complets restent des objectifs de production à implémenter.

Le build adverse n'est pas entièrement révélé pendant le combat. Le passif sélectionné peut être visible afin de fournir une information stratégique forte, tandis que les sorts équipés restent partiellement ou totalement à découvrir pendant le combat.

Après le match, les decks adverses doivent être visibles afin de favoriser l'apprentissage, l'analyse de la méta et la compréhension des choix de build.

Les sorts n'ont pas de coût de composition. Le joueur construit librement le deck de sorts de chaque personnage dans la limite des slots disponibles. L'équilibrage doit donc être porté par le design intrinsèque des sorts, leurs coûts AP, leurs cooldowns, leurs contraintes de lancement, leurs limites d'utilisation, leurs interactions et les patches d'équilibrage.


### Créateur d'équipe et expérience hors-combat

Le nombre de presets d'équipe sauvegardables n'est pas encore défini. Le système doit permettre au joueur de conserver plusieurs compositions, mais la limite exacte sera décidée plus tard selon les besoins UX, techniques et de progression.

Le créateur d'équipe doit être accessible depuis le menu principal. Il constitue l'espace principal de préparation hors-combat : choix des 3 personnages, ordre des slots, sélection des 6 sorts actifs par personnage et choix du passif.

La duplication de presets n'est pas prioritaire pour les premières versions. Elle peut être ajoutée plus tard si le besoin de créer des variantes de compositions devient important.

Les presets d'équipe ne sont pas nommés manuellement par le joueur dans la direction actuelle. Le jeu peut se contenter d'une présentation visuelle de la composition, par exemple via les portraits des personnages et leur ordre de slot.

Les statistiques de composition, comme dégâts, défense, mobilité, contrôle ou difficulté, ne sont pas prioritaires. Elles pourront être envisagées plus tard, mais il faut éviter les scores trompeurs ou les évaluations automatiques qui donnent une fausse impression d'optimalité.

Le créateur d'équipe doit afficher uniquement les alertes bloquantes : personnage manquant, slot vide, nombre de sorts incorrect, passif manquant ou configuration invalide. Il ne doit pas alerter sur des notions subjectives comme composition déséquilibrée ou manque de mobilité.

Le test immédiat contre un bot ou un poutch depuis le créateur d'équipe n'est pas prioritaire. Cette fonction pourra être envisagée plus tard, possiblement via un mode entraînement séparé.

Le jeu n'a pas besoin d'un codex de personnages séparé dans un premier temps. Le créateur d'équipe doit suffire à consulter les personnages, leurs sorts, leurs passifs et les informations nécessaires à la préparation.


### Design des sorts

La présence de sorts très spécialisés est variable selon les personnages. Certains personnages peuvent disposer de sorts situationnels qui renforcent leur identité, tandis que d'autres doivent conserver un pool plus généraliste et régulièrement utile.

Les sorts ne sont pas structurés autour d'une typologie obligatoire comme dégâts, mobilité, contrôle, défense ou finisher. Ces catégories peuvent être utiles pour l'analyse de design, mais elles ne doivent pas contraindre directement la conception ou la présentation des sorts.

Les coûts AP doivent être modérément variés. Le jeu doit proposer des sorts à coûts différents pour créer des arbitrages de tour, sans aller vers des écarts extrêmes qui rendraient certaines actions trop rares ou trop automatiques.

La force des contraintes de lancement est variable selon les personnages et les sorts. Certains kits peuvent reposer sur des contraintes fortes pour créer de la maîtrise, tandis que d'autres doivent rester plus fluides et accessibles.

Les sorts de mobilité doivent être présents mais contrôlés. Ils sont importants pour la profondeur tactique, mais ne doivent pas rendre le positionnement, les obstacles ou la ligne de vue trop faciles à ignorer.

Les sorts de contrôle doivent être présents mais contrôlés. Ils doivent créer du contre-jeu, de l'entrave et des opportunités de combo, sans devenir trop frustrants ou empêcher l'adversaire de jouer.

Les sorts de dégâts directs constituent une base nécessaire, mais ne doivent pas dominer tout le système. Les dégâts doivent coexister avec le placement, les états, les buffs/debuffs, les contraintes de ligne de vue, la mobilité, la protection et les synergies.

Les sorts doivent utiliser un résumé court dans l'interface principale, accompagné de détails complets au survol ou via une tooltip avancée.

Les contraintes des sorts doivent être représentées par des icônes standardisées. Ces icônes doivent aider à lire rapidement la portée, le coût AP, la ligne de vue, le cooldown, les limites d'utilisation, l'AoE ou les contraintes particulières.

La tooltip avancée doit afficher les valeurs exactes, mais pas nécessairement des formules complexes. L'objectif est de fournir une information fiable sans surcharger inutilement le joueur.

Les dégâts prévus doivent être prévisualisés pour les propres sorts du joueur. Cette prévisualisation aide à prendre des décisions mécaniques fiables sans révéler excessivement les informations adverses.

La prévisualisation doit afficher les dégâts normaux et les dégâts critiques, lorsqu'un sort peut critiquer. Elle ne doit pas nécessairement afficher toute la formule détaillée.

Le jeu n'affiche pas systématiquement pourquoi un sort est invalide. Le joueur doit apprendre à comprendre les règles par l'expérience, les icônes, les contraintes visibles et les feedbacks d'interface.

Les cooldowns doivent être visibles directement sur les icônes de sorts.

Les limites par tour ou par cible doivent être visibles dans la tooltip du sort. Elles n'ont pas besoin d'occuper l'interface principale en permanence, mais doivent être consultables facilement.

---

## 9. UI, lisibilité et rythme

L'interface de combat doit afficher tous les sorts équipés par l'unité active. Les sorts indisponibles peuvent être grisés ou marqués comme inutilisables, mais ils doivent rester visibles afin que le joueur conserve une vision complète de ses options.

Le jeu ne révèle pas librement l'ensemble des sorts équipés par les adversaires. La connaissance des personnages, des builds possibles, des portées et des menaces fait partie de la progression du joueur. Le passif actif adverse peut être visible, mais les sorts équipés ne sont pas inspectables librement.

Les zones de danger adverses ne sont pas affichées automatiquement. Le joueur doit apprendre les portées et menaces par l'expérience. L'interface doit toutefois rester claire sur les règles mécaniques : cases accessibles, coûts AP/MP, validité d'un sort, portée, ligne de vue, zone d'effet et raisons d'invalidité.

L'UI doit assister le joueur sur la compréhension des règles, mais pas sur la décision stratégique. Elle ne doit pas recommander le meilleur coup, afficher automatiquement toutes les menaces adverses ou prédire l'action optimale.

En PvP, un timer de tour est utilisé pour préserver le rythme du combat. La cible actuelle est de 30 à 45 secondes par tour. Le PvE, notamment les tutoriels, peut rester plus permissif afin de faciliter l'apprentissage. Dans le prototype réseau actuel, le ready de placement est branché, mais les timers PvP ne sont pas encore considérés comme finalisés.

Les informations suivantes doivent être visibles en permanence : HP, AP, MP, ordre des tours, effets d'état et passif actif.


### Phase de placement et révélation d'informations

La cible de la phase de placement PvP est de 45 secondes. Cette durée doit laisser assez de temps pour positionner les 3 personnages sans ralentir excessivement le lancement du combat.

Les deux joueurs placent leurs unités simultanément, avec un placement caché. Les positions adverses ne sont pas visibles en temps réel pendant la phase de placement.

Les positions adverses sont révélées à la fin du placement. Cela permet de conserver une part de mindgame initial sans transformer le placement en réaction directe et permanente à l'adversaire.

Après avoir cliqué Ready, un joueur peut encore modifier son placement tant que l'autre joueur n'est pas prêt. Une fois les deux joueurs prêts, le placement est verrouillé et le combat peut commencer.

Les zones de spawn doivent être de taille moyenne. Elles doivent offrir quelques choix de placement significatifs sans créer une phase de mindgame trop longue ou trop complexe.

La timeline complète est visible pendant le placement, avec les personnages des deux joueurs dans l'ordre d'activation.

La cible produit est que le passif adverse soit visible pendant la phase de placement. Cette information donne une indication stratégique importante sans révéler les sorts équipés.

Les sorts adverses ne sont pas révélés pendant le placement. Le joueur connaît les personnages adverses, leur ordre d'activation et leurs passifs, mais il doit découvrir les sorts équipés pendant le combat ou après le match.

Dans le prototype réseau actuel, le ready de placement et le masquage des positions adverses sont branchés, mais le timer de placement, la révélation de passifs et la révélation pré-combat complète ne sont pas encore finalisés.

---

## 10. Cartes, grille et verticalité

La taille standard des cartes n'est pas encore définie. Elle devra être déterminée par des tests liés au format 3v3, à la durée cible des combats, à la quantité d'obstacles, à la mobilité moyenne des personnages et à la lisibilité globale.

En PvP, les cartes doivent être symétriques. Cette symétrie est importante pour préserver l'équité compétitive, notamment en ranked et dans les matchs rapides.

Les obstacles doivent avoir une place forte dans la structure des cartes. Les cartes ne doivent pas être de simples arènes ouvertes : les obstacles servent à casser les lignes de vue, créer des zones de contournement, structurer les engagements, protéger certains placements et forcer des décisions de déplacement.

La verticalité doit avoir un impact gameplay modéré mais réel. Elle peut influencer la portée et la ligne de vue. Le projet vise une verticalité légère, cohérente avec le passage vers URP 3D, sans transformer le jeu en tactical 3D complexe.

Les hazards et glyphes neutres ne constituent pas un élément central des cartes. Les glyphes, pièges et hazards doivent principalement venir des sorts des personnages, afin de préserver la lisibilité et de faire des kits de personnages la source principale des effets de terrain.

Les spawns sont fixes et symétriques, mais peuvent dépendre de la carte. Chaque carte peut donc proposer une disposition spécifique de zones de départ, tant que l'équité compétitive reste respectée.

Les cartes doivent favoriser la diversité selon la carte. Certaines cartes peuvent encourager le combat direct, d'autres le contrôle de zone, le contournement, le jeu à distance ou les engagements progressifs. Cette diversité ne doit pas compromettre la lisibilité ni créer de déséquilibre structurel.

## Cartes compétitives et dimensions

La taille exacte des cartes pour le format 3v3 reste à définir. Les premières cartes devront être testées selon la durée réelle des combats, la mobilité moyenne, la portée des sorts, la quantité d'obstacles et la lisibilité globale.

La distance initiale entre les deux équipes doit être moyenne : une menace peut exister dès le tour 1, mais les engagements forts doivent plutôt apparaître à partir du tour 2. Cette règle aide à éviter le burst tour 1 tout en gardant un premier tour actif.

Les obstacles peuvent avoir des comportements variables selon leur type. Certains peuvent bloquer le mouvement, d'autres la ligne de vue, et certains peuvent bloquer les deux. Cette distinction doit rester lisible visuellement.

Plusieurs types d'obstacles peuvent être envisagés plus tard, par exemple bloquants, semi-bloquants, destructibles ou liés à la hauteur. Pour les premières versions, il faut rester prudent afin de ne pas complexifier trop vite la lecture des cartes.

La hauteur doit rester rare. Les cartes n'ont pas besoin d'avoir systématiquement des zones de hauteur fixes. La verticalité doit être utilisée avec parcimonie pour préserver la lisibilité du PvP.

Les lignes de vue doivent être moyennement contraintes. Les cartes doivent contenir assez d'obstacles pour créer du contournement, du positionnement et des décisions de placement, sans devenir des labyrinthes qui ralentissent excessivement le combat.

Les cartes ne doivent pas forcément avoir un centre unique dominant. Elles doivent plutôt proposer plusieurs zones importantes afin de favoriser les choix de trajectoire, les contournements et les styles de composition différents.

Pour la première version jouable, il faut viser 2 à 3 cartes compétitives. Ce volume permet de tester la variété sans diluer l'effort de polish et d'équilibrage.

---

## 11. Équilibrage, AP/MP/HP et rythme de combat

La létalité doit varier selon les personnages, les compositions et les situations tactiques. Le jeu doit permettre des éliminations rapides lorsqu'un combo est bien préparé sur une cible fragile, sans rendre tous les combats instantanés ou impossibles à rattraper.

Une unité moyenne peut mourir entre 1 et 3 tours de focus selon son archétype, son positionnement, les ressources investies et les défenses disponibles. Une cible fragile peut être éliminée en un tour bien exécuté, tandis qu'une unité plus résistante doit demander davantage d'engagement.

Le soin est présent mais limité. Il ne doit pas permettre d'annuler systématiquement les erreurs, ni créer des combats interminables. Son rôle doit être tactique : sauver une unité, prolonger une fenêtre de jeu, soutenir une stratégie précise ou forcer l'adversaire à investir davantage de ressources.

Les boucliers et réductions de dégâts peuvent être centraux dans certaines compositions. Ils constituent un outil défensif majeur, mais doivent rester contrables par le timing, le focus, les effets de contrôle, le contournement ou les dégâts différés.

Les valeurs AP/MP peuvent varier principalement via les buffs et debuffs en combat. Les valeurs de base doivent rester suffisamment lisibles, tandis que les variations temporaires créent des fenêtres tactiques et des opportunités de combo.

Les dégâts de base ne sont pas aléatoires. Il n'y a pas de plage de dégâts standard. L'aléatoire est déplacé vers le système de coups critiques : certains sorts peuvent avoir un taux de critique propre, et ce taux peut être modifié en combat par des buffs ou debuffs issus de sorts.

Les coups critiques doivent donc être conçus comme une mécanique de tension contrôlée, dépendante du sort et des effets actifs, plutôt qu'une variance universelle appliquée à tous les dégâts.

Les cooldowns sont variables selon la puissance et la fonction du sort. Certains sorts peuvent être utilisés fréquemment, tandis que d'autres doivent créer un timing fort, une menace ponctuelle ou une fenêtre stratégique identifiable.

Les limites par tour ou par cible doivent être utilisées seulement sur certains sorts, principalement pour éviter les abus, empêcher les boucles trop fortes ou encadrer les compétences à fort potentiel de combo.

---

## 12. Critiques, buffs/debuffs et états

Les critiques ont deux rôles principaux : créer une surprise occasionnelle et permettre à certains personnages de construire une partie de leur identité autour du critique. Ils ne doivent pas devenir une mécanique universelle obligatoire pour tous les personnages.

Les critiques modifient uniquement les dégâts. Un critique ne déclenche pas d'effet bonus, n'applique pas d'état supplémentaire et ne modifie pas les mécaniques de push, soin ou bouclier. Cette décision préserve la lisibilité et limite la variance des effets.

Le taux critique doit être visible sur les propres sorts du joueur. Les taux critiques adverses ne sont pas affichés librement, afin de conserver une part d'apprentissage et de connaissance des personnages.

Les buffs et debuffs peuvent couvrir plusieurs familles : AP/MP, portée, dégâts infligés ou reçus, critique, bouclier/réduction, états de contrôle et éventuellement altération de ligne de vue ou ciblage. Toutes ces familles peuvent exister, mais elles doivent être hiérarchisées pour éviter une surcharge de lecture.

Les états stackables sont un système central. Le stacking doit donc être pensé comme un outil important de profondeur tactique, de montée en puissance, de pression progressive ou de contre-jeu. Il doit toutefois rester lisible : le nombre de stacks, leur durée et leur effet doivent être clairement accessibles.

La durée des états est variable selon l'état. Certains états peuvent être très courts pour créer une fenêtre tactique immédiate, tandis que d'autres peuvent durer plus longtemps lorsqu'ils structurent une stratégie ou un plan de jeu.

Les états doivent être dissipables, mais le dispel doit rester rare et précieux. Il ne doit pas annuler trop facilement les stratégies basées sur les états, mais il doit exister comme outil de réponse important contre certaines compositions.

Les buffs et debuffs doivent être visibles sur les unités. Les icônes doivent être visibles en permanence ou facilement accessibles, avec le détail complet disponible au survol. L'interface doit permettre de comprendre l'état actuel d'une unité sans afficher automatiquement toutes les décisions optimales.

---

## 13. Systèmes déjà implémentés

### Services runtime

- BattleService : orchestration du combat, phases, actions, fin de combat.
- TurnSystem : ordre de tour par initiative, rounds, ajout/retrait d'unités.
- GridService : grille, cases marchables, occupation, footprints, glyphes.
- PathService : pathfinding orthogonal avec coûts de déplacement.
- SimpleSkillExecutor : validation et exécution des compétences.
- DefaultEnemyBrain : IA ennemie configurable.
- RuntimeServicesBootstrap : initialisation persistante des services runtime et chargement du menu principal.
- MatchRuntimeContext : contexte de match partagé entre menu, matchmaking, réseau et combat.
- QuickMatchFlowController : flow match rapide avec sign-in, ticket, polling, annulation et handoff vers le contexte de match.
- MatchRuntimeSessionLifecycle : nettoyage de session, arrêt PurrNet, release serveur et sortie de lobby au retour menu.
- QuickMatchSceneHandoff : chargement de la scène de combat lorsque le match et les compositions sont prêts.

### Matchmaking et réseau

Le prototype supporte un chemin quick match via UGS Matchmaker avec queue actuellement configurée en scène sur `quickmatch1v1edgegap`. Le ticket peut produire un simple match id, ou un endpoint IP/port lorsque le hosting serveur dédié est configuré.

Le prototype supporte un lobby privé via UGS Lobby + Relay. Le joueur hôte crée un lobby privé, reçoit un code de lobby, initialise l'allocation Relay et attend un second joueur. Le joueur invité rejoint par code. Ce mode est prévu pour les playtests privés non classés.

Le prototype conserve un chemin direct IP pour debug local, ainsi qu'un mode de test serveur dédié local. Ces chemins ne font pas partie de l'expérience joueur finale.

PurrNet est utilisé pour connecter les joueurs, attribuer les slots TeamA/TeamB, synchroniser le manifest de match, valider le ready de placement, transmettre les commandes de combat au serveur/host et répliquer les commandes acceptées. Le serveur ou host reste l'autorité d'exécution des commandes en ligne.

La synchronisation réseau actuelle est command-based : les clients envoient des commandes de combat sérialisées, l'autorité les valide et les exécute, puis diffuse les commandes acceptées. Il n'y a pas encore de snapshot complet, de rollback ou de resynchronisation automatique. Un checksum de combat permet de détecter une divergence côté client.

Le manifest de match transporte aujourd'hui le match id, le map id, les assignations joueur-slot, le preset id et les unit ids. Il ne transporte pas encore la sélection complète de sorts, de passif ou une vraie phase de révélation pré-combat.

En lobby Relay, le joueur hôte démarre le rôle PurrNet serveur tout en agissant comme joueur TeamA. Ce mode reste non classé et non fiable pour le reporting compétitif.

L'architecture réseau cible distingue :

- quick match / futur ranked : UGS Matchmaker, serveur dédié EdgeGap, transport UDP, autorité fiable ;
- lobby privé : UGS Lobby + Relay, host joueur, transport UTP Relay, non classé ;
- debug local : direct IP ou serveur local, transport UDP, non classé.

Le module Cloud Code EdgegapAllocator existe pour préparer l'allocation de serveurs EdgeGap via Matchmaker, avec release d'allocation prévue. Le flow scène actuel attend surtout un endpoint IP/port retourné par Matchmaker ; l'allocation manuelle côté client n'est pas le chemin de production. Ce chemin reste à valider côté dashboard, build serveur Linux, image EdgeGap et tests bout en bout.

### Grille

Le système de grille supporte : déplacement orthogonal, cases non marchables, coûts de déplacement, occupation par unité, unités runtime à footprint supérieur à 1x1, ligne de vue, cellules de spawn, previews visuelles de déplacement, zone, ciblage, glyphes et hazards.

### Unités

Les unités sont définies via ScriptableObject : HP max, équipe, portée de déplacement, AP par tour, initiative, compétences disponibles, profil IA ennemi, états de base, états conditionnels selon les PV, prefab visuel, portrait et teinte.

Contenu actuellement présent :

- SimpleUnit : unité joueur simple, 100 HP, 4 MP, 6 AP.
- TestUnit : unité joueur de test, 80 HP, 3 MP, 7 AP.
- SimpleEnnemy : ennemi simple, 100 HP, 4 MP, 6 AP, IA configurée.
- Unit_PoutchSingle : cible/ennemi statique de test, 200 HP, 0 MP, 0 AP.

### Compétences

Les compétences sont data-driven via SkillDefinition. Le système supporte dégâts, soin, push, téléportation, échange de position, invocation, glyphes/pièges, états, coût AP, limite par tour, limite par cible, cooldown, portée min/max, ligne de vue, alignement orthogonal ou diagonal, AoE variées, falloff de dégâts AoE et modificateurs directionnels front/côté/dos.

Compétences actuellement créées :

- Skill_SimpleHit : dégâts single target, portée 1-6, ligne de vue, 10 dégâts, coût 3 AP.
- Test_CrossSkill : dégâts en croix, portée 4-8, alignement orthogonal, ligne de vue, coût 3 AP.

### États

Le système d'états supporte durée en tours, max stacks, marqueurs passifs, bonus/malus dégâts, réduction de dégâts, modificateur de portée, modificateur AP et modificateur mouvement.

État de test : New State, durée 5 tours, réduction de dégâts de 10.

### IA ennemie

L'IA peut choisir une compétence valide, scorer les cibles, prioriser les ennemis proches/faibles/forts selon profil, prendre en compte dégâts, soins, kill confirm, AoE, sécurité, se repositionner, garder ses distances, kite après action et utiliser des règles spécifiques par compétence.

Le profil EnemyAiProfile_simplekit utilise une logique de distance : priorité cible proche, politique KeepDistance, distance préférée 5, rayon de menace 5, kite après action activé.

---

## 14. Décisions stabilisées

- Mode principal : PvP.
- Système de tour final : alternance stricte entre joueurs.
- Initiative d'équipe calculée par somme des initiatives des personnages.
- Initiative utilisée uniquement pour déterminer le premier joueur.
- Première version : premier joueur déterminé par pile ou face.
- Aucun buff/debuff d'initiative prévu.
- Chaque personnage joue une fois par round.
- Le joueur choisit explicitement les slots 1/2/3 de son équipe.
- La timeline est visible pendant la préparation et le placement.
- L'ordre des personnages est verrouillé avant la phase de placement.
- Aucun coût de composition pour les personnages.
- Aucun coût de composition pour les sorts.
- Pas de contraintes de deck : choix libre des 6 sorts dans le pool.
- Passif indépendant des sorts disponibles.
- Passifs conçus pour modifier le style de jeu du personnage.
- Passifs visibles dès la révélation de l'équipe.
- Passifs potentiellement complexes.
- Passifs pouvant modifier de façon limitée les règles de base du personnage.
- Passifs capables de créer des builds très différents.
- Passifs équilibrés prioritairement pour le PvP.
- Spécialisation des sorts variable selon personnage.
- Pas de typologie obligatoire des sorts pour le joueur.
- Coûts AP modérément variés.
- Contraintes de lancement variables selon personnage/sort.
- Mobilité présente mais contrôlée.
- Contrôle présent mais contrôlé.
- Dégâts directs nécessaires mais non dominants.
- Résumé court des sorts dans l'interface principale.
- Détails complets des sorts au survol / tooltip avancée.
- Icônes standardisées pour les contraintes de sorts.
- Tooltips avec valeurs exactes, sans formules complexes obligatoires.
- Prévisualisation des dégâts sur ses propres sorts.
- Prévisualisation des dégâts normaux et critiques.
- Cooldowns visibles directement sur les icônes de sorts.
- Limites par tour/cible visibles dans la tooltip.
- Presets d'équipe complète sauvegardables.
- Decks adverses visibles après le match.
- Roster V1 avec archétypes lisibles mais hybridés.
- Environ la moitié du roster doit être facile à prendre en main.
- 1 à 2 personnages experts en V1.
- Difficulté affichable dans le créateur d'équipe.
- Chaque personnage doit avoir une faiblesse claire mais pas trop évidente.
- Mécanique signature non obligatoire pour chaque personnage.
- Personnages équilibrés surtout pour leur rôle en composition.
- Créateur d'équipe accessible depuis le menu principal.
- Presets d'équipe sauvegardables, nombre exact non défini.
- Duplication de presets non prioritaire.
- Presets non nommés manuellement.
- Alertes du créateur d'équipe limitées aux erreurs bloquantes.
- Statistiques de composition non prioritaires.
- Test contre bot/poutch non prioritaire.
- Pas de codex séparé prioritaire : le créateur d'équipe doit suffire.
- Première version en ligne : lobby privé + match rapide non classé.
- Combat réseau fonctionnel pour playtests contrôlés.
- Architecture réseau séparée par intention de match, avec combat partagé par tous les modes.
- Match rapide via UGS Matchmaker.
- Queue actuelle de match rapide en scène : `quickmatch1v1edgegap`.
- Lobby privé via UGS Lobby + Relay.
- Lobby privé non classé, non autorisé à reporter des résultats ranked.
- Direct IP conservé uniquement comme chemin debug.
- Serveur dédié EdgeGap ciblé pour quick match autoritaire et futur ranked.
- PurrNet utilisé pour connexion, slots joueurs, ready de placement, commandes de combat et réplication.
- UDPTransport utilisé pour serveur local/dédié.
- UTPTransport utilisé pour les lobbies Relay.
- Ranked autorisé uniquement sur un chemin serveur fiable.
- Ranked après équilibrage suffisant.
- Matchmaking simple au début, sans MMR ni rang.
- Matchs privés avec règles standard.
- Carte aléatoire parmi un pool compétitif.
- Équipe choisie avant matchmaking, création de lobby ou join lobby.
- Build verrouillé dès l'entrée dans le flow réseau.
- Avant combat, personnages adverses, ordre d'activation et passif actif révélés.
- Phase de placement PvP : 45 secondes.
- Placement simultané caché.
- Positions adverses révélées à la fin du placement.
- Ready modifiable tant que l'autre joueur n'est pas prêt.
- Zones de spawn moyennes.
- Timeline complète visible pendant le placement.
- Passif adverse visible pendant le placement.
- Sorts adverses non révélés pendant le placement.
- Distance initiale moyenne : menace possible tour 1, engagement fort plutôt tour 2.
- Obstacles à comportement variable selon type.
- Hauteur rare.
- Lignes de vue moyennement contraintes.
- Cartes avec plusieurs zones importantes plutôt qu'un centre unique.
- Première version jouable : 2 à 3 cartes compétitives.
- L'équilibrage repose sur le design des kits, les contraintes de sorts et les patches.
- Le placement initial ne doit pas favoriser une équipe.
- Le burst tour 1 doit être évité sans rendre le premier tour passif.
- PvE : tutoriels et défis scénarisés courts.
- Progression : cosmétiques, ranked, compte, maîtrise des personnages.
- Pas de déblocage de personnages comme progression compétitive.
- Sélection d'équipe libre avant matchmaking.
- Doublons de personnages interdits.
- Roster V1 : 8 à 10 personnages.
- Format principal fixé : 3v3.
- Personnages sans rôles formels stricts.
- Pool de sorts par personnage.
- Chaque personnage équipe exactement 6 sorts actifs.
- Pool cible : 10 sorts par personnage, flexible pour futures versions.
- 2 passifs par personnage, 1 passif choisi.
- Pas de variantes de sorts.
- Tous les sorts équipés de l'unité active sont affichés.
- Timer de tour en PvP.
- Durée cible d'un tour : 30 à 45 secondes.
- Infos permanentes : HP, AP, MP, timeline, états, passif actif.
- Cartes PvP symétriques.
- Obstacles fortement structurants.
- Verticalité légère influençant portée et ligne de vue.
- Hazards principalement issus des sorts des personnages.
- Létalité variable selon personnages, compositions et situations.
- Une unité peut mourir entre 1 et 3 tours de focus selon son archétype.
- Soin présent mais limité.
- Boucliers et réductions de dégâts centraux dans certaines compositions.
- Variations AP/MP principalement via buffs et debuffs en combat.
- Pas de RNG sur les dégâts de base.
- Critiques possibles avec taux dépendant du sort et modifiable via buffs/debuffs.
- Cooldowns variables selon puissance et fonction du sort.
- Limites par tour/cible utilisées seulement sur certains sorts.
- Critiques utilisés comme surprise occasionnelle et outil d'identité pour certains personnages.
- Critiques limités à une augmentation de dégâts.
- Taux critique visible sur les propres sorts du joueur.
- États stackables comme système central.
- Dispel rare et précieux.
- Buffs/debuffs visibles avec détail complet au survol.

---

## 15. Décisions ouvertes

- Facing : absent, global ou mécanique spécifique à certains personnages.
- Plateforme prioritaire : PC, web ou mobile.
- Univers : fangame inspiré One Piece ou IP originale.
- Hauteur / verticalité : ampleur exacte de l'impact gameplay.
- Nombre maximal de presets sauvegardables.
- Présentation visuelle des presets sans nom manuel.
- Besoin éventuel de duplication de presets.
- Besoin futur d'un mode test depuis le créateur d'équipe.
- Besoin futur d'un codex séparé.
- Archétypes exacts obligatoires du roster V1.
- Liste des personnages faciles, intermédiaires et experts.
- Critères de difficulté affichée.
- Définition des faiblesses par personnage.
- Rôle exact des passifs dans l'identité des personnages.
- Niveau maximal de complexité acceptable pour un passif.
- Types de règles de base modifiables par les passifs.
- Méthode d'équilibrage des passifs en PvP.
- Niveau acceptable de sorts situationnels par personnage.
- Répartition réelle des coûts AP.
- Niveau de contrainte par type de sort.
- Format exact des descriptions de sorts.
- Règles UX des tooltips avancés.
- Liste exacte des icônes standardisées.
- Niveau exact d'explication des invalidités de sorts.
- Règles de prévisualisation des dégâts selon buffs/debuffs.
- Format exact des dégâts critiques dans l'UI.
- Date et conditions d'ouverture du ranked.
- Futur système de MMR/rang.
- Taille du pool de cartes compétitives.
- Détail du comportement du timer de placement.
- UX exacte du bouton Ready et annulation Ready.
- Taille exacte des zones de spawn par carte.
- Taille cible des cartes 3v3.
- Typologie exacte des obstacles.
- Règles visuelles pour distinguer les obstacles.
- Critères de validation d'une carte compétitive.
- Moment exact de révélation des positions si le timer expire.
- Valeur exacte du timer PvP.
- Différence entre timer normal et ranked.
- Présence ou non d'un historique de combat.
- Niveau de détail des tooltips.
- Codex / encyclopédie des personnages et sorts.
- Taille standard des cartes.
- Degré exact de symétrie des cartes selon les modes.
- Règles exactes de verticalité.
- Valeurs cibles HP/AP/MP par archétype.
- Règle définitive de départ du premier joueur : pile ou face en première version, puis initiative totale potentiellement.
- Suivi statistique de l'impact du premier joueur selon les compositions.
- Méthode d'équilibrage de l'initiative par personnage.
- Règles anti-burst tour 1.
- Ordre interne exact des personnages dans l'équipe : slot choisi par joueur, initiative individuelle, ou ordre de composition.
- Formule exacte des critiques.
- Liste des sorts pouvant critiquer.
- Niveau de puissance acceptable des boucliers.
- Ratio dégâts/soin/bouclier.
- Durée moyenne réelle d'élimination par archétype.
- Hiérarchie exacte des familles de buffs/debuffs.
- Formule de stacking des états.
- Limites de stacks par type d'état.
- Règles de dispel.
- Liste des personnages ou sorts capables de dispel.
- Règles d'affichage des effets adverses.

---

## 16. À compléter

- Identité finale du jeu.
- Roster définitif.
- Progression détaillée.
- Objectifs de mission éventuels.
- Conditions de victoire spécifiques.
- Boss patterns éventuels.
- Vagues de renfort éventuelles.
- Trophées / objectifs secondaires.
- Équilibrage AP / MP / HP / dégâts.
- Design final des skills.
- Règles exactes de cooldown.
- Validation production du chemin EdgeGap / serveur dédié.
- Critères techniques d'ouverture du quick match autoritaire.
- Politique de gestion des déconnexions, abandons et reconnexions.
- Règles de resynchronisation après mismatch checksum.
- Politique de late join et spectateur.
- Timeout de placement en réseau.
- Place des grosses unités.
- Place des invocations, glyphes et hazards.
- Direction artistique finale.

