#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Editor tooling
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.EditorTools
{
    public static class DeckbuildingPanelPrefabBuilder
    {
        #region _____________________________/ VALUES

        private const string PrefabPath = "Assets/Prefabs/UI/UI_Panel_Deckbuilding.prefab";
        private const string SkillButtonPrefabPath = "Assets/Prefabs/UI/HUD/UI_Btn_Skill.prefab";
        private static readonly Color PanelColor = new Color(1f, 1f, 1f, 0.96f);
        private static readonly Color SurfaceColor = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color BorderColor = new Color(0.18f, 0.23f, 0.31f, 1f);
        private static readonly Color TextColor = new Color(0.06f, 0.09f, 0.16f, 1f);
        private static readonly Color MutedTextColor = new Color(0.29f, 0.33f, 0.41f, 1f);
        private static readonly Color SelectedColor = new Color(0.75f, 0.9f, 1f, 1f);
        private static readonly Color DisabledColor = new Color(0.72f, 0.75f, 0.8f, 1f);
        private static readonly Color ButtonDarkColor = new Color(0.06f, 0.09f, 0.16f, 1f);

        #endregion

        #region _____________________________| MENU

        [MenuItem("Tools/TacticalPort/UI/Rebuild Deckbuilding Panel Prefab")]
        public static void RebuildPrefab()
        {
            GameObject lRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Build(lRoot);
                PrefabUtility.SaveAsPrefabAsset(lRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Rebuilt deckbuilding panel prefab: {PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(lRoot);
            }
        }

        [MenuItem("Tools/TacticalPort/UI/Add or Bind Deckbuilding Validate Button")]
        public static void AddOrBindValidateButton()
        {
            GameObject lRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                DeckbuildingPanelController lController = lRoot.GetComponent<DeckbuildingPanelController>();
                Transform lPanelMain = FindByName(lRoot.transform, "Panel_Main");
                Transform lSource = FindByName(lRoot.transform, "Btn_ChangeCharacter");
                if (lController == null || lPanelMain == null || lSource == null)
                    throw new System.InvalidOperationException("Deckbuilding prefab is missing its controller, Panel_Main or Btn_ChangeCharacter.");

                Transform lValidateTransform = FindByName(lRoot.transform, "Btn_Validate");
                if (lValidateTransform == null)
                {
                    GameObject lValidateObject = Object.Instantiate(lSource.gameObject, lPanelMain, false);
                    lValidateObject.name = "Btn_Validate";
                    lValidateTransform = lValidateObject.transform;
                }

                RectTransform lRect = EnsureRectTransform(lValidateTransform.gameObject);
                lRect.anchorMin = new Vector2(1f, 0f);
                lRect.anchorMax = new Vector2(1f, 0f);
                lRect.pivot = new Vector2(1f, 0f);
                lRect.anchoredPosition = new Vector2(-24f, 24f);
                lRect.sizeDelta = new Vector2(240f, 72f);
                lRect.localScale = Vector3.one;

                LayoutElement lLayout = lValidateTransform.GetComponent<LayoutElement>();
                if (lLayout != null)
                    lLayout.ignoreLayout = true;

                TMP_Text[] lTexts = lValidateTransform.GetComponentsInChildren<TMP_Text>(true);
                for (int lIndex = 0; lIndex < lTexts.Length; lIndex++)
                {
                    if (lTexts[lIndex] != null && !string.IsNullOrWhiteSpace(lTexts[lIndex].text))
                        lTexts[lIndex].text = "VALIDER";
                }

                Button lValidateButton = lValidateTransform.GetComponent<Button>();
                SerializedObject lObject = new SerializedObject(lController);
                lObject.FindProperty("_ValidateButton").objectReferenceValue = lValidateButton;
                lObject.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(lValidateTransform.gameObject);
                EditorUtility.SetDirty(lController);
                PrefabUtility.SaveAsPrefabAsset(lRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Added and bound deckbuilding validate button: {PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(lRoot);
            }
        }

        #endregion

        #region _____________________________| BUILD

        private static void Build(GameObject pRoot)
        {
            pRoot.name = "UI_Panel_Deckbuilding";
            RectTransform lRootRect = EnsureRectTransform(pRoot);
            SetFullStretch(lRootRect);
            ClearChildren(pRoot.transform);

            DeckbuildingPanelController lController = EnsureComponent<DeckbuildingPanelController>(pRoot);
            EnsureComponent<CanvasRenderer>(pRoot);
            Image lRootImage = EnsureComponent<Image>(pRoot);
            lRootImage.color = new Color(0f, 0f, 0f, 0f);

            GameObject lPanelMain = CreatePanel("Panel_Main", pRoot.transform, new Vector2(1100f, 610f), Vector2.zero, PanelColor);
            AddOutline(lPanelMain, BorderColor, new Vector2(2f, -2f));
            Button lCloseButton = CreateButton("Btn_CloseDeckbuilding", lPanelMain.transform, "X", new Vector2(42f, 36f), new Vector2(520f, 270f), ButtonDarkColor, Color.white);
            Button lValidateButton = CreateButton("Btn_Validate", lPanelMain.transform, "VALIDER", new Vector2(180f, 48f), new Vector2(430f, -265f), ButtonDarkColor, Color.white);

            GameObject lPanelLeft = CreatePanel("Panel_Left", lPanelMain.transform, new Vector2(320f, 520f), new Vector2(-350f, 10f), new Color(1f, 1f, 1f, 0f));
            GameObject lPanelRight = CreatePanel("Panel_Right", lPanelMain.transform, new Vector2(650f, 520f), new Vector2(190f, 10f), new Color(1f, 1f, 1f, 0f));
            CreateDivider(lPanelMain.transform);

            CharacterRefs lCharacter = BuildLeftPanel(lPanelLeft.transform);
            BuildRefs lBuild = BuildRightPanel(lPanelRight.transform);
            lBuild.CloseButton = lCloseButton;
            lBuild.ValidateButton = lValidateButton;
            CharacterPickerRefs lCharacterPicker = BuildCharacterPicker(pRoot.transform);
            SkillPickerRefs lSkillPicker = BuildSkillPicker(pRoot.transform);
            StatsPlaceholderRefs lStatsPlaceholder = BuildStatsPlaceholder(pRoot.transform);

            AssignController(lController, lCharacter, lBuild, lCharacterPicker, lSkillPicker, lStatsPlaceholder);
            EditorUtility.SetDirty(pRoot);
        }

        private static CharacterRefs BuildLeftPanel(Transform pParent)
        {
            TMP_Text lName = CreateText("Txt_Name", pParent, "NOM DU PERSONNAGE", 20f, FontStyles.Bold, TextColor, new Vector2(265f, 36f), new Vector2(0f, 230f));

            GameObject lPreview = CreatePanel("Preview_Character", pParent, new Vector2(240f, 310f), new Vector2(0f, 35f), SurfaceColor);
            AddOutline(lPreview, new Color(0.6f, 0.65f, 0.72f, 1f), new Vector2(1.5f, -1.5f));

            RawImage lModelRenderTarget = CreateRawImage("Img_ModelPreview", lPreview.transform, new Vector2(210f, 270f), new Vector2(0f, 10f), Color.white);
            Image lPortrait = CreateImage("Img_FallbackPortrait", lPreview.transform, new Vector2(190f, 250f), new Vector2(0f, 10f), Color.white);
            DeckbuildingUnitPreviewView lPreviewView = CreateUnitPreviewView(lPreview, lModelRenderTarget, lPortrait);

            GameObject lSkin = CreatePanel("Slot_Skin", pParent, new Vector2(52f, 52f), new Vector2(130f, -115f), SurfaceColor);
            AddOutline(lSkin, BorderColor, new Vector2(1f, -1f));
            CreateText("Txt_SkinLetter", lSkin.transform, "S", 18f, FontStyles.Bold, TextColor, new Vector2(40f, 28f), Vector2.zero).alignment = TextAlignmentOptions.Center;
            CreateText("Txt_Skin", pParent, "Skin", 14f, FontStyles.Normal, MutedTextColor, new Vector2(60f, 22f), new Vector2(130f, -155f)).alignment = TextAlignmentOptions.Center;

            Button lChange = CreateButton("Btn_ChangeCharacter", pParent, "CHANGER", new Vector2(150f, 48f), new Vector2(0f, -210f), Color.white, TextColor);

            return new CharacterRefs
            {
                Name = lName,
                Portrait = lPortrait,
                Preview = lPreviewView,
                ChangeButton = lChange
            };
        }

        private static BuildRefs BuildRightPanel(Transform pParent)
        {
            Transform lBuildRoot = CreateEmpty("Grid_Build", pParent, new Vector2(650f, 330f), new Vector2(0f, 125f)).transform;

            DeckbuildingPanelController.CoreValueView lEnergy = CreateCoreValueBadge("Badge_Energy", lBuildRoot, new Vector2(-70f, 135f));
            DeckbuildingPanelController.CoreValueView lHealth = CreateCoreValueBadge("Badge_Health", lBuildRoot, new Vector2(95f, 135f));
            DeckbuildingPanelController.CoreValueView lMobilityPerTurn = CreateCoreValueBadge("Badge_Mobility", lBuildRoot, new Vector2(260f, 135f));

            DeckbuildingPanelController.PassiveSlotView[] lPassiveSlots =
            {
                CreatePassiveSlot("Slot_Passive_1", lBuildRoot, new Vector2(-250f, 20f)),
                CreatePassiveSlot("Slot_Passive_2", lBuildRoot, new Vector2(-250f, -95f))
            };

            DeckbuildingPanelController.SkillSlotView[] lSkillSlots =
            {
                CreateSkillSlot("Slot_Skill_1", lBuildRoot, new Vector2(-70f, 20f)),
                CreateSkillSlot("Slot_Skill_2", lBuildRoot, new Vector2(95f, 20f)),
                CreateSkillSlot("Slot_Skill_3", lBuildRoot, new Vector2(260f, 20f)),
                CreateSkillSlot("Slot_Skill_4", lBuildRoot, new Vector2(-70f, -95f)),
                CreateSkillSlot("Slot_Skill_5", lBuildRoot, new Vector2(95f, -95f)),
                CreateSkillSlot("Slot_Skill_6", lBuildRoot, new Vector2(260f, -95f))
            };

            CreateText("Txt_StatsTitle", pParent, "STATS", 18f, FontStyles.Bold, TextColor, new Vector2(120f, 28f), new Vector2(-300f, -115f));
            Transform lStatsRoot = CreateEmpty("Grid_Stats", pParent, new Vector2(470f, 120f), new Vector2(-80f, -185f)).transform;

            DeckbuildingPanelController.StatCellView[] lStats =
            {
                CreateStatCell("Stat_DmgMelee", lStatsRoot, "Dmg Melee", new Vector2(-130f, 38f)),
                CreateStatCell("Stat_ResMelee", lStatsRoot, "Res Melee", new Vector2(100f, 38f)),
                CreateStatCell("Stat_DmgDistance", lStatsRoot, "Dmg Distance", new Vector2(-130f, -2f)),
                CreateStatCell("Stat_ResDistance", lStatsRoot, "Res Distance", new Vector2(100f, -2f)),
                CreateStatCell("Stat_Velocity", lStatsRoot, "Velocity", new Vector2(-130f, -42f))
            };

            Button lEditButton = CreateButton("Btn_EditStats", pParent, "EDIT", new Vector2(64f, 56f), new Vector2(270f, -185f), ButtonDarkColor, Color.white);

            return new BuildRefs
            {
                Energy = lEnergy,
                Health = lHealth,
                MobilityPerTurn = lMobilityPerTurn,
                PassiveSlots = lPassiveSlots,
                SkillSlots = lSkillSlots,
                Stats = lStats,
                EditStatsButton = lEditButton
            };
        }

        private static CharacterPickerRefs BuildCharacterPicker(Transform pParent)
        {
            GameObject lPopup = CreateModalRoot("Popup_CharacterPicker", pParent);
            GameObject lCard = CreatePanel("Panel_Card", lPopup.transform, new Vector2(760f, 520f), Vector2.zero, PanelColor);
            AddOutline(lCard, BorderColor, new Vector2(1.5f, -1.5f));

            CreateText("Txt_Title", lCard.transform, "Selection personnage", 24f, FontStyles.Bold, TextColor, new Vector2(420f, 40f), new Vector2(-120f, 220f));
            Button lClose = CreateButton("Btn_Close", lCard.transform, "FERMER", new Vector2(110f, 38f), new Vector2(295f, 220f), Color.white, TextColor);
            RectTransform lGrid = CreateEmpty("Grid_CharacterPicker", lCard.transform, new Vector2(690f, 390f), new Vector2(0f, -20f)).GetComponent<RectTransform>();
            GridLayoutGroup lGridLayout = lGrid.gameObject.AddComponent<GridLayoutGroup>();
            lGridLayout.cellSize = new Vector2(150f, 160f);
            lGridLayout.spacing = new Vector2(18f, 18f);
            lGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            lGridLayout.constraintCount = 4;

            RectTransform lTemplate = CreateCharacterTemplate(lGrid);
            lTemplate.gameObject.SetActive(false);
            lPopup.SetActive(false);

            return new CharacterPickerRefs
            {
                Popup = lPopup,
                Root = lGrid,
                Template = lTemplate,
                CloseButton = lClose
            };
        }

        private static SkillPickerRefs BuildSkillPicker(Transform pParent)
        {
            GameObject lPopup = CreateModalRoot("Popup_SkillPicker", pParent);
            GameObject lCard = CreatePanel("Panel_Card", lPopup.transform, new Vector2(850f, 540f), Vector2.zero, PanelColor);
            AddOutline(lCard, BorderColor, new Vector2(1.5f, -1.5f));

            CreateText("Txt_Title", lCard.transform, "Selection skill", 24f, FontStyles.Bold, TextColor, new Vector2(420f, 40f), new Vector2(-165f, 230f));
            Button lClose = CreateButton("Btn_Close", lCard.transform, "FERMER", new Vector2(110f, 38f), new Vector2(340f, 230f), Color.white, TextColor);
            RectTransform lGrid = CreateEmpty("Grid_SkillPicker", lCard.transform, new Vector2(780f, 410f), new Vector2(0f, -20f)).GetComponent<RectTransform>();
            GridLayoutGroup lGridLayout = lGrid.gameObject.AddComponent<GridLayoutGroup>();
            lGridLayout.cellSize = new Vector2(180f, 120f);
            lGridLayout.spacing = new Vector2(16f, 16f);
            lGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            lGridLayout.constraintCount = 4;

            RectTransform lTemplate = CreateSkillTemplate(lGrid);
            lTemplate.gameObject.SetActive(false);
            lPopup.SetActive(false);

            return new SkillPickerRefs
            {
                Popup = lPopup,
                Root = lGrid,
                Template = lTemplate,
                CloseButton = lClose
            };
        }

        private static StatsPlaceholderRefs BuildStatsPlaceholder(Transform pParent)
        {
            GameObject lPopup = CreateModalRoot("Popup_StatsEditPlaceholder", pParent);
            GameObject lCard = CreatePanel("Panel_Card", lPopup.transform, new Vector2(420f, 180f), Vector2.zero, PanelColor);
            AddOutline(lCard, BorderColor, new Vector2(1.5f, -1.5f));
            CreateText("Txt_Title", lCard.transform, "Edition stats", 22f, FontStyles.Bold, TextColor, new Vector2(320f, 34f), new Vector2(0f, 55f)).alignment = TextAlignmentOptions.Center;
            CreateText("Txt_Message", lCard.transform, "L'edition de stats n'est pas encore implementee.", 16f, FontStyles.Normal, MutedTextColor, new Vector2(350f, 48f), new Vector2(0f, 10f)).alignment = TextAlignmentOptions.Center;
            Button lClose = CreateButton("Btn_Close", lCard.transform, "OK", new Vector2(90f, 36f), new Vector2(0f, -55f), ButtonDarkColor, Color.white);
            lPopup.SetActive(false);

            return new StatsPlaceholderRefs
            {
                Popup = lPopup,
                CloseButton = lClose
            };
        }

        #endregion

        #region _____________________________| ASSIGN

        private static void AssignController(
            DeckbuildingPanelController pController,
            CharacterRefs pCharacter,
            BuildRefs pBuild,
            CharacterPickerRefs pCharacterPicker,
            SkillPickerRefs pSkillPicker,
            StatsPlaceholderRefs pStatsPlaceholder)
        {
            SerializedObject lObject = new SerializedObject(pController);

            AssignUnits(lObject.FindProperty("_AvailableUnits"));
            lObject.FindProperty("_TeamSize").intValue = 3;
            lObject.FindProperty("_CurrentSlotIndex").intValue = 0;

            SerializedProperty lCharacter = lObject.FindProperty("_Character");
            lCharacter.FindPropertyRelative("Name").objectReferenceValue = pCharacter.Name;
            lCharacter.FindPropertyRelative("Portrait").objectReferenceValue = pCharacter.Portrait;
            lCharacter.FindPropertyRelative("Preview").objectReferenceValue = pCharacter.Preview;

            lObject.FindProperty("_EnergyValue").FindPropertyRelative("Value").objectReferenceValue = pBuild.Energy.Value;
            lObject.FindProperty("_HealthValue").FindPropertyRelative("Value").objectReferenceValue = pBuild.Health.Value;
            lObject.FindProperty("_MobilityPerTurnValue").FindPropertyRelative("Value").objectReferenceValue = pBuild.MobilityPerTurn.Value;

            AssignPassiveSlots(lObject.FindProperty("_PassiveSlots"), pBuild.PassiveSlots);
            AssignSkillSlots(lObject.FindProperty("_SkillSlots"), pBuild.SkillSlots);
            AssignStats(lObject.FindProperty("_Stats"), pBuild.Stats);

            lObject.FindProperty("_ChangeCharacterButton").objectReferenceValue = pCharacter.ChangeButton;
            lObject.FindProperty("_EditStatsButton").objectReferenceValue = pBuild.EditStatsButton;
            lObject.FindProperty("_ValidateButton").objectReferenceValue = pBuild.ValidateButton;
            lObject.FindProperty("_CloseButton").objectReferenceValue = pBuild.CloseButton;

            lObject.FindProperty("_CharacterPickerPopup").objectReferenceValue = pCharacterPicker.Popup;
            lObject.FindProperty("_CharacterPickerRoot").objectReferenceValue = pCharacterPicker.Root;
            lObject.FindProperty("_CharacterPickerItemTemplate").objectReferenceValue = pCharacterPicker.Template;
            lObject.FindProperty("_CloseCharacterPickerButton").objectReferenceValue = pCharacterPicker.CloseButton;

            lObject.FindProperty("_SkillPickerPopup").objectReferenceValue = pSkillPicker.Popup;
            lObject.FindProperty("_SkillPickerRoot").objectReferenceValue = pSkillPicker.Root;
            lObject.FindProperty("_SkillPickerItemTemplate").objectReferenceValue = pSkillPicker.Template;
            lObject.FindProperty("_CloseSkillPickerButton").objectReferenceValue = pSkillPicker.CloseButton;

            lObject.FindProperty("_StatsEditPlaceholderPopup").objectReferenceValue = pStatsPlaceholder.Popup;
            lObject.FindProperty("_CloseStatsPlaceholderButton").objectReferenceValue = pStatsPlaceholder.CloseButton;

            lObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignUnits(SerializedProperty pUnits)
        {
            string[] lGuids = AssetDatabase.FindAssets("t:UnitDefinition", new[] { "Assets" });
            List<UnitDefinition> lUnits = new List<UnitDefinition>();
            for (int lIndex = 0; lIndex < lGuids.Length; lIndex++)
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuids[lIndex]);
                UnitDefinition lUnit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(lPath);
                if (lUnit != null && !lUnits.Contains(lUnit))
                    lUnits.Add(lUnit);
            }

            pUnits.arraySize = lUnits.Count;
            for (int lIndex = 0; lIndex < lUnits.Count; lIndex++)
                pUnits.GetArrayElementAtIndex(lIndex).objectReferenceValue = lUnits[lIndex];
        }

        private static void AssignPassiveSlots(SerializedProperty pSlots, IReadOnlyList<DeckbuildingPanelController.PassiveSlotView> pViews)
        {
            pSlots.arraySize = pViews.Count;
            for (int lIndex = 0; lIndex < pViews.Count; lIndex++)
            {
                SerializedProperty lElement = pSlots.GetArrayElementAtIndex(lIndex);
                lElement.FindPropertyRelative("Button").objectReferenceValue = pViews[lIndex].Button;
                lElement.FindPropertyRelative("Background").objectReferenceValue = pViews[lIndex].Background;
                lElement.FindPropertyRelative("Icon").objectReferenceValue = pViews[lIndex].Icon;
                lElement.FindPropertyRelative("Name").objectReferenceValue = pViews[lIndex].Name;
                lElement.FindPropertyRelative("Tooltip").objectReferenceValue = pViews[lIndex].Tooltip;
                lElement.FindPropertyRelative("SelectedIndicator").objectReferenceValue = pViews[lIndex].SelectedIndicator;
            }
        }

        private static void AssignSkillSlots(SerializedProperty pSlots, IReadOnlyList<DeckbuildingPanelController.SkillSlotView> pViews)
        {
            pSlots.arraySize = pViews.Count;
            for (int lIndex = 0; lIndex < pViews.Count; lIndex++)
            {
                SerializedProperty lElement = pSlots.GetArrayElementAtIndex(lIndex);
                lElement.FindPropertyRelative("Button").objectReferenceValue = pViews[lIndex].Button;
                lElement.FindPropertyRelative("Background").objectReferenceValue = pViews[lIndex].Background;
                lElement.FindPropertyRelative("Icon").objectReferenceValue = pViews[lIndex].Icon;
                lElement.FindPropertyRelative("Name").objectReferenceValue = pViews[lIndex].Name;
                lElement.FindPropertyRelative("Meta").objectReferenceValue = pViews[lIndex].Meta;
                lElement.FindPropertyRelative("Tooltip").objectReferenceValue = pViews[lIndex].Tooltip;
            }
        }

        private static void AssignStats(SerializedProperty pStats, IReadOnlyList<DeckbuildingPanelController.StatCellView> pViews)
        {
            pStats.arraySize = pViews.Count;
            for (int lIndex = 0; lIndex < pViews.Count; lIndex++)
            {
                SerializedProperty lElement = pStats.GetArrayElementAtIndex(lIndex);
                lElement.FindPropertyRelative("Label").objectReferenceValue = pViews[lIndex].Label;
                lElement.FindPropertyRelative("Value").objectReferenceValue = pViews[lIndex].Value;
            }
        }

        #endregion

        #region _____________________________| UI FACTORIES

        private static DeckbuildingUnitPreviewView CreateUnitPreviewView(GameObject pPreviewRoot, RawImage pRenderTarget, Image pFallbackPortrait)
        {
            GameObject lStage = new GameObject("Preview3D_Stage");
            lStage.transform.SetParent(pPreviewRoot.transform, false);
            lStage.transform.localPosition = Vector3.zero;
            lStage.transform.localRotation = Quaternion.identity;
            lStage.transform.localScale = Vector3.one;

            GameObject lModelRoot = new GameObject("ModelRoot");
            lModelRoot.transform.SetParent(lStage.transform, false);
            lModelRoot.transform.localPosition = Vector3.zero;
            lModelRoot.transform.localRotation = Quaternion.identity;
            lModelRoot.transform.localScale = Vector3.one;

            GameObject lCameraObject = new GameObject("PreviewCamera");
            lCameraObject.transform.SetParent(lStage.transform, false);
            lCameraObject.transform.localPosition = new Vector3(0f, 1f, -5f);
            lCameraObject.transform.localRotation = Quaternion.identity;

            Camera lCamera = lCameraObject.AddComponent<Camera>();
            lCamera.clearFlags = CameraClearFlags.SolidColor;
            lCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            lCamera.orthographic = true;
            lCamera.orthographicSize = 2f;
            lCamera.enabled = false;

            DeckbuildingUnitPreviewView lPreviewView = pPreviewRoot.AddComponent<DeckbuildingUnitPreviewView>();
            SerializedObject lObject = new SerializedObject(lPreviewView);
            lObject.FindProperty("_RenderTarget").objectReferenceValue = pRenderTarget;
            lObject.FindProperty("_FallbackPortrait").objectReferenceValue = pFallbackPortrait;
            lObject.FindProperty("_PreviewCamera").objectReferenceValue = lCamera;
            lObject.FindProperty("_ModelRoot").objectReferenceValue = lModelRoot.transform;
            lObject.ApplyModifiedPropertiesWithoutUndo();
            return lPreviewView;
        }

        private static DeckbuildingPanelController.PassiveSlotView CreatePassiveSlot(string pName, Transform pParent, Vector2 pPosition)
        {
            GameObject lRoot = CreateDeckSkillButton(pName, pParent, pPosition);
            ConfigureDeckSkillButtonTexts(lRoot, "P", string.Empty);

            return new DeckbuildingPanelController.PassiveSlotView
            {
                Button = lRoot.GetComponent<Button>(),
                Background = FindChildImage(lRoot, "Btn_Skill_Background"),
                Icon = FindChildImage(lRoot, "Btn_Skill_Icon"),
                Name = FindChildText(lRoot, "Txt_SkillName"),
                Tooltip = EnsureSkillTooltip(lRoot, new Vector2(380f, 260f), new Vector2(175f, 140f)),
                SelectedIndicator = null
            };
        }

        private static DeckbuildingPanelController.SkillSlotView CreateSkillSlot(string pName, Transform pParent, Vector2 pPosition)
        {
            GameObject lRoot = CreateDeckSkillButton(pName, pParent, pPosition);
            ConfigureDeckSkillButtonTexts(lRoot, "Skill", "Energy 0 | 0-0");

            return new DeckbuildingPanelController.SkillSlotView
            {
                Button = lRoot.GetComponent<Button>(),
                Background = FindChildImage(lRoot, "Btn_Skill_Background"),
                Icon = FindChildImage(lRoot, "Btn_Skill_Icon"),
                Name = FindChildText(lRoot, "Txt_SkillName"),
                Meta = FindChildText(lRoot, "Txt_EnergyCostAndRange"),
                Tooltip = EnsureSkillTooltip(lRoot, new Vector2(380f, 260f), new Vector2(175f, 140f))
            };
        }

        private static GameObject CreateDeckSkillButton(string pName, Transform pParent, Vector2 pPosition)
        {
            GameObject lPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillButtonPrefabPath);
            GameObject lRoot = lPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(lPrefab, pParent)
                : CreatePanel(pName, pParent, new Vector2(96f, 96f), pPosition, SurfaceColor);

            lRoot.name = pName;

            if (lRoot.TryGetComponent(out SkillButtonView lHudSkillView))
                Object.DestroyImmediate(lHudSkillView);

            if (lRoot.TryGetComponent(out UIInteractionAnimator lInteractionAnimator))
                Object.DestroyImmediate(lInteractionAnimator);

            if (lRoot.TryGetComponent(out AspectRatioFitter lAspectRatioFitter))
                Object.DestroyImmediate(lAspectRatioFitter);

            RectTransform lRect = EnsureRectTransform(lRoot);
            lRect.anchorMin = new Vector2(0.5f, 0.5f);
            lRect.anchorMax = new Vector2(0.5f, 0.5f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.sizeDelta = new Vector2(96f, 96f);
            lRect.anchoredPosition = pPosition;
            lRect.localScale = Vector3.one;

            Button lButton = lRoot.GetComponent<Button>() ?? lRoot.AddComponent<Button>();
            Image lBackground = FindChildImage(lRoot, "Btn_Skill_Background");
            if (lBackground != null)
                lButton.targetGraphic = lBackground;

            if (lRoot.GetComponent<CanvasGroup>() == null)
                lRoot.AddComponent<CanvasGroup>();

            return lRoot;
        }

        private static void ConfigureDeckSkillButtonTexts(GameObject pRoot, string pNameText, string pMetaText)
        {
            TMP_Text lName = FindChildText(pRoot, "Txt_SkillName");
            if (lName != null)
            {
                lName.text = pNameText;
                lName.fontSize = 10f;
                lName.fontStyle = FontStyles.Bold;
                lName.color = TextColor;
                lName.alignment = TextAlignmentOptions.Center;
                lName.textWrappingMode = TextWrappingModes.NoWrap;
                lName.overflowMode = TextOverflowModes.Ellipsis;
                SetChildRect(lName.rectTransform, new Vector2(84f, 24f), new Vector2(0f, 14f));
            }

            TMP_Text lMeta = FindChildText(pRoot, "Txt_EnergyCostAndRange");
            if (lMeta != null)
            {
                lMeta.text = pMetaText;
                lMeta.fontSize = 9f;
                lMeta.color = MutedTextColor;
                lMeta.alignment = TextAlignmentOptions.Center;
                lMeta.textWrappingMode = TextWrappingModes.NoWrap;
                lMeta.overflowMode = TextOverflowModes.Ellipsis;
                lMeta.gameObject.SetActive(!string.IsNullOrEmpty(pMetaText));
                SetChildRect(lMeta.rectTransform, new Vector2(86f, 18f), new Vector2(0f, -24f));
            }

            TMP_Text lAdditionalEffect = FindChildText(pRoot, "Txt_AdditionalEffect");
            if (lAdditionalEffect != null)
                lAdditionalEffect.gameObject.SetActive(false);

            TMP_Text lPower = FindChildText(pRoot, "Txt_Power");
            if (lPower != null)
                lPower.gameObject.SetActive(false);
        }

        private static DeckbuildingPanelController.StatCellView CreateStatCell(string pName, Transform pParent, string pLabel, Vector2 pPosition)
        {
            GameObject lRoot = CreatePanel(pName, pParent, new Vector2(220f, 28f), pPosition, Color.white);
            AddOutline(lRoot, new Color(0.6f, 0.65f, 0.72f, 1f), new Vector2(0.5f, -0.5f));
            TMP_Text lLabel = CreateText("Txt_Label", lRoot.transform, pLabel, 13f, FontStyles.Normal, TextColor, new Vector2(140f, 22f), new Vector2(-35f, 0f));
            TMP_Text lValue = CreateText("Txt_Value", lRoot.transform, "0", 13f, FontStyles.Bold, TextColor, new Vector2(44f, 22f), new Vector2(78f, 0f));
            lValue.alignment = TextAlignmentOptions.Center;

            return new DeckbuildingPanelController.StatCellView
            {
                Label = lLabel,
                Value = lValue
            };
        }

        private static RectTransform CreateCharacterTemplate(Transform pParent)
        {
            GameObject lRoot = CreatePanel("Item_CharacterPicker_Template", pParent, new Vector2(150f, 160f), Vector2.zero, SurfaceColor);
            AddOutline(lRoot, new Color(0.6f, 0.65f, 0.72f, 1f), new Vector2(1f, -1f));
            Button lButton = lRoot.AddComponent<Button>();
            lButton.targetGraphic = lRoot.GetComponent<Image>();
            CreateImage("Img_Icon", lRoot.transform, new Vector2(95f, 88f), new Vector2(0f, 28f), Color.white);
            CreateText("Txt_Name", lRoot.transform, "Character", 13f, FontStyles.Bold, TextColor, new Vector2(130f, 24f), new Vector2(0f, -38f)).alignment = TextAlignmentOptions.Center;
            CreateText("Txt_State", lRoot.transform, string.Empty, 11f, FontStyles.Normal, MutedTextColor, new Vector2(130f, 20f), new Vector2(0f, -63f)).alignment = TextAlignmentOptions.Center;
            return lRoot.GetComponent<RectTransform>();
        }

        private static RectTransform CreateSkillTemplate(Transform pParent)
        {
            GameObject lRoot = CreatePanel("Item_SkillPicker_Template", pParent, new Vector2(180f, 120f), Vector2.zero, SurfaceColor);
            AddOutline(lRoot, new Color(0.6f, 0.65f, 0.72f, 1f), new Vector2(1f, -1f));
            Button lButton = lRoot.AddComponent<Button>();
            lButton.targetGraphic = lRoot.GetComponent<Image>();
            CreateImage("Img_Icon", lRoot.transform, new Vector2(38f, 38f), new Vector2(-58f, 28f), Color.white);
            CreateText("Txt_Name", lRoot.transform, "Skill", 13f, FontStyles.Bold, TextColor, new Vector2(105f, 26f), new Vector2(28f, 31f));
            CreateText("Txt_Meta", lRoot.transform, "Energy 0 | Range 0-0", 11f, FontStyles.Normal, MutedTextColor, new Vector2(160f, 36f), new Vector2(0f, -15f));
            CreateText("Txt_State", lRoot.transform, string.Empty, 11f, FontStyles.Normal, MutedTextColor, new Vector2(160f, 20f), new Vector2(0f, -47f)).alignment = TextAlignmentOptions.Center;
            EnsureSkillTooltip(lRoot, new Vector2(380f, 260f), new Vector2(210f, 110f));
            return lRoot.GetComponent<RectTransform>();
        }

        private static DeckbuildingSkillTooltipView EnsureSkillTooltip(GameObject pRoot, Vector2 pSize, Vector2 pPosition)
        {
            Transform lNativeTooltip = FindChildTransform(pRoot.transform, "UI_Panel_Tooltip");
            TooltipRefs lTooltip = lNativeTooltip != null
                ? CreateNativeTooltipRefs(lNativeTooltip)
                : CreateTooltipPanel(pRoot.transform, pSize, pPosition);

            DeckbuildingSkillTooltipView lTooltipView = EnsureComponent<DeckbuildingSkillTooltipView>(pRoot);
            SerializedObject lObject = new SerializedObject(lTooltipView);
            lObject.FindProperty("_TooltipPanel").objectReferenceValue = lTooltip.Panel;
            lObject.FindProperty("_LayoutRoot").objectReferenceValue = lTooltip.LayoutRoot;
            lObject.FindProperty("_NameText").objectReferenceValue = lTooltip.NameText;
            lObject.FindProperty("_MetaText").objectReferenceValue = lTooltip.MetaText;
            lObject.FindProperty("_PowerText").objectReferenceValue = lTooltip.PowerText;
            lObject.FindProperty("_AdditionalEffectText").objectReferenceValue = lTooltip.AdditionalEffectText;
            lObject.FindProperty("_DescriptionText").objectReferenceValue = lTooltip.DescriptionText;
            lObject.ApplyModifiedPropertiesWithoutUndo();
            return lTooltipView;
        }

        private static TooltipRefs CreateNativeTooltipRefs(Transform pTooltip) => new TooltipRefs
        {
            Panel = pTooltip.gameObject,
            LayoutRoot = FindChildTransform(pTooltip, "VBox_Content") as RectTransform,
            NameText = FindChildText(pTooltip.gameObject, "Txt_SkillName"),
            MetaText = FindChildText(pTooltip.gameObject, "Txt_EnergyCostAndRange"),
            PowerText = FindChildText(pTooltip.gameObject, "Txt_Power"),
            AdditionalEffectText = FindChildText(pTooltip.gameObject, "Txt_AdditionalEffect"),
            DescriptionText = FindChildText(pTooltip.gameObject, "Txt_Description")
        };

        private static TooltipRefs CreateTooltipPanel(Transform pParent, Vector2 pSize, Vector2 pPosition)
        {
            GameObject lPanel = CreatePanel("UI_Panel_DeckbuildingTooltip", pParent, pSize, pPosition, new Color(0.06f, 0.09f, 0.16f, 0.96f));
            AddOutline(lPanel, new Color(0.3f, 0.36f, 0.46f, 1f), new Vector2(1f, -1f));
            Image lPanelImage = lPanel.GetComponent<Image>();
            if (lPanelImage != null)
                lPanelImage.raycastTarget = false;

            TMP_Text lDescription = CreateText("Txt_Description", lPanel.transform, string.Empty, 18f, FontStyles.Normal, Color.white, pSize - new Vector2(28f, 26f), Vector2.zero);
            lDescription.alignment = TextAlignmentOptions.TopLeft;
            lDescription.textWrappingMode = TextWrappingModes.Normal;
            lDescription.overflowMode = TextOverflowModes.Overflow;

            ConfigureTooltipPanel(lPanel, pSize, pPosition);
            lPanel.SetActive(false);
            return new TooltipRefs
            {
                Panel = lPanel,
                LayoutRoot = lPanel.GetComponent<RectTransform>(),
                DescriptionText = lDescription
            };
        }

        private static void ConfigureTooltipPanel(GameObject pTooltip, Vector2 pSize, Vector2 pPosition)
        {
            if (pTooltip == null)
                return;

            RectTransform lRect = pTooltip.GetComponent<RectTransform>();
            if (lRect != null)
                SetChildRect(lRect, pSize, pPosition);

            Graphic[] lGraphics = pTooltip.GetComponentsInChildren<Graphic>(true);
            for (int lIndex = 0; lIndex < lGraphics.Length; lIndex++)
                lGraphics[lIndex].raycastTarget = false;
        }

        private static DeckbuildingPanelController.CoreValueView CreateCoreValueBadge(string pName, Transform pParent, Vector2 pPosition)
        {
            GameObject lRoot = CreatePanel(pName, pParent, new Vector2(58f, 58f), pPosition, Color.white);
            AddOutline(lRoot, new Color(0.42f, 0.46f, 0.54f, 1f), new Vector2(1.5f, -1.5f));
            TMP_Text lText = CreateText("Txt_Value", lRoot.transform, "0", 18f, FontStyles.Bold, TextColor, new Vector2(52f, 32f), Vector2.zero);
            lText.alignment = TextAlignmentOptions.Center;

            return new DeckbuildingPanelController.CoreValueView
            {
                Value = lText
            };
        }

        private static GameObject CreateModalRoot(string pName, Transform pParent)
        {
            GameObject lRoot = CreatePanel(pName, pParent, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
            RectTransform lRect = lRoot.GetComponent<RectTransform>();
            SetFullStretch(lRect);
            return lRoot;
        }

        private static Button CreateButton(string pName, Transform pParent, string pLabel, Vector2 pSize, Vector2 pPosition, Color pColor, Color pTextColor)
        {
            GameObject lRoot = CreatePanel(pName, pParent, pSize, pPosition, pColor);
            AddOutline(lRoot, BorderColor, new Vector2(1f, -1f));
            Button lButton = lRoot.AddComponent<Button>();
            lButton.targetGraphic = lRoot.GetComponent<Image>();
            TMP_Text lText = CreateText("Txt_Label", lRoot.transform, pLabel, 16f, FontStyles.Bold, pTextColor, pSize - new Vector2(12f, 10f), Vector2.zero);
            lText.alignment = TextAlignmentOptions.Center;
            return lButton;
        }

        private static TMP_Text CreateText(string pName, Transform pParent, string pText, float pSize, FontStyles pStyle, Color pColor, Vector2 pRectSize, Vector2 pPosition)
        {
            GameObject lObject = CreateEmpty(pName, pParent, pRectSize, pPosition);
            TextMeshProUGUI lText = lObject.AddComponent<TextMeshProUGUI>();
            lText.text = pText;
            lText.fontSize = pSize;
            lText.fontStyle = pStyle;
            lText.color = pColor;
            lText.alignment = TextAlignmentOptions.Left;
            lText.textWrappingMode = TextWrappingModes.Normal;
            lText.overflowMode = TextOverflowModes.Ellipsis;
            lText.raycastTarget = false;
            return lText;
        }

        private static Image CreateImage(string pName, Transform pParent, Vector2 pSize, Vector2 pPosition, Color pColor)
        {
            GameObject lObject = CreateEmpty(pName, pParent, pSize, pPosition);
            Image lImage = lObject.AddComponent<Image>();
            lImage.color = pColor;
            lImage.preserveAspect = true;
            return lImage;
        }

        private static RawImage CreateRawImage(string pName, Transform pParent, Vector2 pSize, Vector2 pPosition, Color pColor)
        {
            GameObject lObject = CreateEmpty(pName, pParent, pSize, pPosition);
            RawImage lImage = lObject.AddComponent<RawImage>();
            lImage.color = pColor;
            return lImage;
        }

        private static TMP_Text FindChildText(GameObject pRoot, string pName)
        {
            TMP_Text[] lTexts = pRoot.GetComponentsInChildren<TMP_Text>(true);
            for (int lIndex = 0; lIndex < lTexts.Length; lIndex++)
            {
                TMP_Text lText = lTexts[lIndex];
                if (lText != null && lText.name == pName)
                    return lText;
            }

            return null;
        }

        private static Transform FindChildTransform(Transform pRoot, string pName)
        {
            if (pRoot == null)
                return null;
            if (pRoot.name == pName)
                return pRoot;

            for (int lIndex = 0; lIndex < pRoot.childCount; lIndex++)
            {
                Transform lResult = FindChildTransform(pRoot.GetChild(lIndex), pName);
                if (lResult != null)
                    return lResult;
            }

            return null;
        }

        private static Image FindChildImage(GameObject pRoot, string pName)
        {
            Image[] lImages = pRoot.GetComponentsInChildren<Image>(true);
            for (int lIndex = 0; lIndex < lImages.Length; lIndex++)
            {
                Image lImage = lImages[lIndex];
                if (lImage != null && lImage.name == pName)
                    return lImage;
            }

            return null;
        }

        private static void SetChildRect(RectTransform pRect, Vector2 pSize, Vector2 pPosition)
        {
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = pSize;
            pRect.anchoredPosition = pPosition;
        }

        private static GameObject CreatePanel(string pName, Transform pParent, Vector2 pSize, Vector2 pPosition, Color pColor)
        {
            GameObject lObject = CreateEmpty(pName, pParent, pSize, pPosition);
            Image lImage = lObject.AddComponent<Image>();
            lImage.color = pColor;
            return lObject;
        }

        private static GameObject CreateEmpty(string pName, Transform pParent, Vector2 pSize, Vector2 pPosition)
        {
            GameObject lObject = new GameObject(pName, typeof(RectTransform));
            lObject.transform.SetParent(pParent, false);
            RectTransform lRect = lObject.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 0.5f);
            lRect.anchorMax = new Vector2(0.5f, 0.5f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.sizeDelta = pSize;
            lRect.anchoredPosition = pPosition;
            return lObject;
        }

        private static void CreateDivider(Transform pParent)
        {
            Image lDivider = CreateImage("Divider", pParent, new Vector2(2f, 520f), new Vector2(-190f, 10f), new Color(0.82f, 0.84f, 0.88f, 1f));
            lDivider.raycastTarget = false;
        }

        private static void AddOutline(GameObject pObject, Color pColor, Vector2 pDistance)
        {
            Outline lOutline = pObject.AddComponent<Outline>();
            lOutline.effectColor = pColor;
            lOutline.effectDistance = pDistance;
            lOutline.useGraphicAlpha = true;
        }

        private static RectTransform EnsureRectTransform(GameObject pObject)
        {
            if (pObject.TryGetComponent(out RectTransform lRect))
                return lRect;

            return pObject.AddComponent<RectTransform>();
        }

        private static T EnsureComponent<T>(GameObject pObject) where T : Component
        {
            if (pObject.TryGetComponent(out T lComponent))
                return lComponent;

            return pObject.AddComponent<T>();
        }

        private static void SetFullStretch(RectTransform pRect)
        {
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = Vector2.zero;
        }

        private static void ClearChildren(Transform pRoot)
        {
            for (int lIndex = pRoot.childCount - 1; lIndex >= 0; lIndex--)
                Object.DestroyImmediate(pRoot.GetChild(lIndex).gameObject);
        }

        private static Transform FindByName(Transform pRoot, string pName)
        {
            if (pRoot == null)
                return null;
            if (pRoot.name == pName)
                return pRoot;

            for (int lIndex = 0; lIndex < pRoot.childCount; lIndex++)
            {
                Transform lResult = FindByName(pRoot.GetChild(lIndex), pName);
                if (lResult != null)
                    return lResult;
            }

            return null;
        }

        #endregion

        #region _____________________________/ REFS

        private sealed class CharacterRefs
        {
            public TMP_Text Name;
            public Image Portrait;
            public DeckbuildingUnitPreviewView Preview;
            public Button ChangeButton;
        }

        private sealed class BuildRefs
        {
            public DeckbuildingPanelController.CoreValueView Energy;
            public DeckbuildingPanelController.CoreValueView Health;
            public DeckbuildingPanelController.CoreValueView MobilityPerTurn;
            public DeckbuildingPanelController.PassiveSlotView[] PassiveSlots;
            public DeckbuildingPanelController.SkillSlotView[] SkillSlots;
            public DeckbuildingPanelController.StatCellView[] Stats;
            public Button EditStatsButton;
            public Button ValidateButton;
            public Button CloseButton;
        }

        private sealed class CharacterPickerRefs
        {
            public GameObject Popup;
            public RectTransform Root;
            public RectTransform Template;
            public Button CloseButton;
        }

        private sealed class SkillPickerRefs
        {
            public GameObject Popup;
            public RectTransform Root;
            public RectTransform Template;
            public Button CloseButton;
        }

        private sealed class StatsPlaceholderRefs
        {
            public GameObject Popup;
            public Button CloseButton;
        }

        private sealed class TooltipRefs
        {
            public GameObject Panel;
            public RectTransform LayoutRoot;
            public TMP_Text NameText;
            public TMP_Text MetaText;
            public TMP_Text PowerText;
            public TMP_Text AdditionalEffectText;
            public TMP_Text DescriptionText;
        }

        #endregion
    }
}
