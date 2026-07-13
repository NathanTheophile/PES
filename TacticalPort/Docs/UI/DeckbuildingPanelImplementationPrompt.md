# Prompt - Implementation UI Panel Deckbuilding

Tu es Codex dans le projet Unity `C:\Users\ntheo\Documents\_Work\PES\TacticalPort`.

Objectif : transformer le prefab `Assets/Prefabs/UI/UI_Panel_Deckbuilding.prefab` en copie fonctionnelle du wireframe `Docs/UI/DeckbuildingPanelWireframe.svg`, puis creer les deux panels annexes :

- une popup de selection de personnage ouverte par le bouton `CHANGER` ;
- une popup de selection de skill ouverte en cliquant sur un slot de sort.

Avant toute modification, lis et applique :

- `C:\Users\ntheo\Documents\_Work\PES\AGENTS.md`
- `C:\Users\ntheo\Documents\_Work\PES\GDD.md`
- `C:\Users\ntheo\Documents\_Work\PES\TacticalPort\Docs\UI\DeckbuildingPanelWireframe.svg`

Respecte le workflow Unity du projet : prefabs, references serializees, composants explicites dans l'Inspector, separation presentation/logique/data, pas de generation runtime excessive d'objets UI qui devraient etre prefab/scene.

## Contexte existant a respecter

Prefab cible :

- `Assets/Prefabs/UI/UI_Panel_Deckbuilding.prefab`

Le prefab existe deja et contient une base proche du wireframe, notamment des objets nommes comme :

- `UI_Panel_Deckbuilding`
- `Panel_Left`
- `Panel_Right`
- `Panel_Preview`
- `HBox_SkillsGrid`
- `HBox_StatsGrid`
- `HBox_MainStats`
- `Btn_Switch`
- `Txt_Name`

Ne repars pas d'un prefab vide. Reorganise et renomme seulement si cela clarifie vraiment la structure. Si tu renommes un objet reference par un champ serialize, mets a jour les references prefab.

Data a exploiter :

- `UnitDefinition`
  - `DisplayName`
  - `Description`
  - `DisplaySprite`, `PreviewSprite`, `Portrait`
  - `ModelPrefab`
  - `Tint`
  - `Skills`
  - `Passives`
  - `DefaultPassive`
  - `MeleeDamagePercent`
  - `MeleeResistancePercent`
  - `RangedDamagePercent`
  - `RangedResistancePercent`
  - `Initiative`
- `SkillDefinition`
  - `DisplayName`
  - `Description`
  - `Icon`
  - `ActionPointCost`
  - `RangeMin`
  - `RangeMax`
  - `RequiresLineOfSight`
  - `AoeShape`
  - `CooldownTurns`
  - `UsePerTurn`
  - `UsePerTarget`
  - `PrimaryEffectType`
  - `AdditionalEffectType`
- `PassiveDefinition`
  - `DisplayName`
  - `Description`
  - `Icon`

Etat de selection deja present :

- `TeamSelectionState`
- `CombatTeamCompositionState`
- `TeamSelectionController`
- `MenuScreenRouter`
- `ITeamSelectionPopupView`

Le flux actuel sauvegarde deja les personnages via `TeamSelectionState.SetSelectedUnits(...)` puis `TeamSelectionState.SaveSelectedUnits()`. Ne casse pas ce flow.

## Layout attendu du panel principal

Le panel principal doit reprendre strictement le wireframe :

1. Grande carte/panel principal centre, fond clair, bordure lisible.
2. Colonne gauche :
   - nom du personnage en haut ;
   - zone preview du personnage, avec sprite/portrait si pas de vrai rendu 3D disponible ;
   - zone skin petite a cote ou sous la preview, placeholder pour l'instant ;
   - bouton `CHANGER`.
3. Zone droite :
   - grille de deckbuilding en 2 lignes de 4 cellules :
     - colonne 1 = 2 passifs, affiches verticalement ;
     - colonnes 2 a 4 = 6 slots de sorts, 3 par ligne.
   - au-dessus des 3 colonnes de sorts : badges ou labels `PA`, `PV`, `PM` comme dans le wireframe.
   - le passif actif est visuellement selectionne.
   - le passif non selectionne est grise/desature.
4. Stats sous la grille, alignees a gauche avec la colonne des passifs :
   - afficher exactement 5 stats :
     - `Dmg Melee`
     - `Res Melee`
     - `Dmg Distance`
     - `Res Distance`
     - `Initiative`
   - pas de sliders ;
   - presenter les stats en 2 colonnes de cellules de meme largeur ;
   - chaque cellule affiche un libelle et une valeur.
5. Bouton `EDIT` a droite de la grille de stats.
6. La zone doit rester lisible en 16:9, sans debordement, sans texte coupe, sans overlap.

## Comportements fonctionnels attendus

### Selection de personnage

- Le bouton `CHANGER` ouvre une popup/panel annexe de selection de personnage.
- Cette popup affiche tous les personnages disponibles en grille de portraits.
- Chaque tuile de personnage affiche :
  - portrait ou `DisplaySprite` ;
  - nom du personnage ;
  - etat visuel selectionne si le personnage est deja choisi.
- Cliquer un personnage :
  - remplace le personnage courant dans le slot actif ;
  - ferme la popup ;
  - rafraichit le panel principal : nom, portrait/model preview, passifs, sorts, stats ;
  - met a jour `TeamSelectionState` / `CombatTeamCompositionState` avec la composition courante.
- Les doublons de personnages doivent etre interdits dans la composition. Si un personnage est deja choisi dans un autre slot, le griser ou desactiver son bouton dans la popup.
- Prevoir un bouton fermer/retour dans la popup.

### Passifs

- Afficher au maximum 2 passifs par personnage.
- Selectionner un passif fonctionne comme un switch :
  - un seul passif actif ;
  - l'actif est mis en valeur ;
  - l'inactif est grise.
- Si le personnage a 0 ou 1 passif, gerer proprement les slots vides.
- La selection de passif doit rester dans l'etat local du deckbuilding, meme si elle n'est pas encore envoyee au matchmaking.

### Sorts

- Le panel principal affiche 6 slots de sorts equipes.
- Par defaut, remplir les 6 slots avec les 6 premiers skills du `UnitDefinition.Skills`, sans doublon.
- Cliquer sur un slot de sort ouvre une popup/panel annexe de selection de skills pour ce personnage.
- Cette popup affiche le pool complet des skills du personnage en grille.
- Chaque tuile de skill affiche :
  - icone ;
  - nom ;
  - cout PA ;
  - range min/max ;
  - indicateur LoS si `RequiresLineOfSight` ;
  - cooldown si `CooldownTurns > 0`.
- Cliquer un skill :
  - remplace le skill du slot actif ;
  - interdit les doublons dans les 6 slots ;
  - ferme la popup ;
  - rafraichit les 6 slots.
- Les skills deja equipes ailleurs doivent etre grises ou non interactifs.
- Prevoir un bouton fermer/retour dans la popup.

### Stats

- Les 5 stats affichent les valeurs du `UnitDefinition` courant :
  - `Dmg Melee` = `MeleeDamagePercent`
  - `Res Melee` = `MeleeResistancePercent`
  - `Dmg Distance` = `RangedDamagePercent`
  - `Res Distance` = `RangedResistancePercent`
  - `Initiative` = `Initiative`
- Le bouton `EDIT` peut rester non fonctionnel si l'edition de stats n'est pas encore supportee, mais il doit etre present, reference et pret pour un futur handler.
- Si tu branches une action temporaire, elle doit seulement ouvrir une popup placeholder propre indiquant que l'edition de stats n'est pas encore implementee.

## Architecture attendue

Favorise un composant dedie au prefab, par exemple :

- `DeckbuildingPanelController`
- `DeckbuildingCharacterPickerPanel`
- `DeckbuildingSkillPickerPanel`
- vues serializees courtes pour :
  - slot personnage ;
  - slot passif ;
  - slot skill ;
  - cellule de stat ;
  - item de grille personnage ;
  - item de grille skill.

Les champs Unity doivent etre `[SerializeField] private`, assignables dans l'Inspector. Evite les `FindObjectOfType`, `GameObject.Find` et recherches runtime globales.

Reutilise les patterns de `TeamSelectionController` quand ils sont adaptes :

- liste `_AvailableUnits` serializee ;
- chargement depuis `TeamSelectionState`;
- sauvegarde via `TeamSelectionState.SetSelectedUnits(...)` et `TeamSelectionState.SaveSelectedUnits()`;
- refresh quand `MenuScreenRouter.ShowTeamSelectionPopup()` affiche le panel.

Si tu modifies ou remplaces `TeamSelectionController`, preserve son contrat `ITeamSelectionPopupView.RefreshSelectionView()`.

Si la selection complete de skills/passif n'a pas encore de state persistant dedie, cree un etat local propre dans le nouveau controller, mais n'invente pas un systeme global complexe. Laisse les points d'extension clairs pour un futur state persistant.

## Prefabs/UI a creer ou completer

Dans `UI_Panel_Deckbuilding.prefab`, creer/organiser :

- `Panel_Main`
- `Panel_Left`
- `Panel_Right`
- `Preview_Character`
- `Btn_ChangeCharacter`
- `Grid_Build`
- `Slot_Passive_1`
- `Slot_Passive_2`
- `Slot_Skill_1` a `Slot_Skill_6`
- `Grid_Stats`
- `Stat_DmgMelee`
- `Stat_ResMelee`
- `Stat_DmgDistance`
- `Stat_ResDistance`
- `Stat_Initiative`
- `Btn_EditStats`
- `Popup_CharacterPicker`
- `Grid_CharacterPicker`
- `Item_CharacterPicker_Template`
- `Popup_SkillPicker`
- `Grid_SkillPicker`
- `Item_SkillPicker_Template`

Les templates doivent etre des GameObjects enfants des popups, inactifs par defaut, instancies au runtime dans les grilles. Les popups doivent etre inactives par defaut.

## Contraintes visuelles

- UI sobre, claire, proche wireframe.
- Utiliser TextMeshPro pour tous les textes.
- Garder les cellules rectangulaires, coins legerement arrondis si possible.
- Eviter les panels imbriques inutiles.
- Les popups doivent avoir un fond voile leger ou un panel modal clair.
- Les grilles doivent utiliser `GridLayoutGroup` ou `LayoutGroup` configure dans le prefab.
- Les textes doivent tenir dans leurs cellules.
- Les boutons doivent avoir un etat disabled visible.

## Validation obligatoire

Apres implementation :

1. Ouvrir/verifier le prefab `UI_Panel_Deckbuilding.prefab`.
2. Verifier que toutes les references serializees du/des controllers sont assignees.
3. Verifier que le prefab se rapproche visuellement de `Docs/UI/DeckbuildingPanelWireframe.svg`.
4. Verifier qu'au runtime ou en scene menu :
   - `CHANGER` ouvre la popup personnages ;
   - choisir un personnage rafraichit le panel ;
   - cliquer un passif switch bien l'etat actif ;
   - cliquer un slot de sort ouvre la popup skills ;
   - choisir un skill remplace le slot ;
   - les doublons de personnages et de skills sont empeches ;
   - les stats affichent les valeurs du personnage courant.
5. Verifier la console Unity et corriger les erreurs.
6. Si la compilation Unity ou le test Play Mode ne peut pas etre lance, le dire explicitement dans le compte rendu.

## Limites

- Ne pas implementer de vrai deck persistant reseau complet si le projet ne l'a pas encore.
- Ne pas inventer d'edition de stats gameplay definitive.
- Ne pas casser le flow de selection d'equipe existant.
- Ne pas supprimer les assets ou scripts existants sans raison forte.
- Ne pas modifier des APIs publiques ou champs serializees existants sans strategie de migration.

Livrable attendu : prefab `UI_Panel_Deckbuilding.prefab` fonctionnel et propre, scripts associes compiles, references Inspector renseignees, et bref compte rendu des elements Unity a verifier/assigner manuellement si certains assets manquent.
