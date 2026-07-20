#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using UnityEditor;
using UnityEngine;
using static TacticalPort.EditorTools.Creator_FileSaver;

namespace TacticalPort.EditorTools
{
    public sealed class Creator_Passive : EditorWindow
    {
        #region _____________________________/ VALUES

        private const string PassivesFolder = "Assets/Data/Passives";

        private string _Id = string.Empty;
        private string _DisplayName = "New Passive";
        private string _Description = string.Empty;
        private Sprite _Icon;
        private PassiveTrigger _Trigger = PassiveTrigger.None;
        private StateProgressionDefinition _StateProgression;
        private int _ProgressionStepsPerTrigger = 1;
        private bool _TriggerOnOwnerVariantExecutions = true;
        private PassiveAutomaticTargetSelection _AutomaticTargetSelection;
        private StateDefinition _AutomaticTargetMarker;
        private UnitTargetType _AutomaticTargetUnitType = UnitTargetType.CharactersOnly;
        private PassiveHealthLossSourceRule _HealthLossSourceRule = PassiveHealthLossSourceRule.EnemiesOnly;
        private PassiveHealthLossReactionTarget _HealthLossReactionTarget;
        private StateDefinition _HealthLossReactionState;
        private int _HealthLossReactionStateStacks = 1;
        private bool _IncludeSummonsAsReactionBeneficiaries;
        private bool _ClearReactionStateOnOwnerBeginTurn;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Create/Passive Definition...")]
        public static void Open()
        {
            Creator_Passive lWindow = GetWindow<Creator_Passive>("Create Passive");
            lWindow.minSize = new Vector2(360f, 320f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            DrawTitle("Create Passive Definition", PassivesFolder);

            _DisplayName = EditorGUILayout.TextField("English Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("English Description", _Description);

            DrawSectionHeader("Gameplay");
            _Trigger = (PassiveTrigger)EditorGUILayout.EnumPopup("Trigger", _Trigger);
            _StateProgression = (StateProgressionDefinition)EditorGUILayout.ObjectField(
                "State Progression",
                _StateProgression,
                typeof(StateProgressionDefinition),
                false);
            _ProgressionStepsPerTrigger = Mathf.Max(1, EditorGUILayout.IntField("Progression Steps / Trigger", _ProgressionStepsPerTrigger));
            _TriggerOnOwnerVariantExecutions = EditorGUILayout.Toggle("Trigger On Owner Variant", _TriggerOnOwnerVariantExecutions);

            DrawSectionHeader("Observed Health Loss");
            _AutomaticTargetSelection = (PassiveAutomaticTargetSelection)EditorGUILayout.EnumPopup(
                "Automatic Target Selection", _AutomaticTargetSelection);
            if (_AutomaticTargetSelection != PassiveAutomaticTargetSelection.None)
            {
                _AutomaticTargetMarker = (StateDefinition)EditorGUILayout.ObjectField(
                    "Target Marker", _AutomaticTargetMarker, typeof(StateDefinition), false);
                _AutomaticTargetUnitType = (UnitTargetType)EditorGUILayout.EnumPopup(
                    "Target Unit Type", _AutomaticTargetUnitType);
                _HealthLossSourceRule = (PassiveHealthLossSourceRule)EditorGUILayout.EnumPopup(
                    "Damage Source Rule", _HealthLossSourceRule);
                _HealthLossReactionTarget = (PassiveHealthLossReactionTarget)EditorGUILayout.EnumPopup(
                    "Reaction Targets", _HealthLossReactionTarget);
                _HealthLossReactionState = (StateDefinition)EditorGUILayout.ObjectField(
                    "Reaction State", _HealthLossReactionState, typeof(StateDefinition), false);
                _HealthLossReactionStateStacks = Mathf.Max(
                    1, EditorGUILayout.IntField("Reaction Stacks", _HealthLossReactionStateStacks));
                _IncludeSummonsAsReactionBeneficiaries = EditorGUILayout.Toggle(
                    "Include Summon Beneficiaries", _IncludeSummonsAsReactionBeneficiaries);
                _ClearReactionStateOnOwnerBeginTurn = EditorGUILayout.Toggle(
                    "Clear Reaction On Owner Turn", _ClearReactionStateOnOwnerBeginTurn);
            }

            DrawSectionHeader("Presentation");
            _Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _Icon, typeof(Sprite), false);

            if (DrawCreateButton("Create Passive Definition"))
                CreateAsset();
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = CreateSerializedAsset(out PassiveDefinition lAsset);
            string lId = SanitizeId(_Id, SanitizeId(_DisplayName, "passive"));

            SetString(lSerializedObject, "_Id", lId);
            SetString(lSerializedObject, "_DisplayName", string.IsNullOrWhiteSpace(_DisplayName) ? lId : _DisplayName.Trim());
            SetString(lSerializedObject, "_Description", _Description);
            SetObject(lSerializedObject, "_Icon", _Icon);
            SetEnum(lSerializedObject, "_Trigger", (int)_Trigger);
            SetObject(lSerializedObject, "_StateProgression", _StateProgression);
            SetInt(lSerializedObject, "_ProgressionStepsPerTrigger", Mathf.Max(1, _ProgressionStepsPerTrigger));
            SetBool(lSerializedObject, "_TriggerOnOwnerVariantExecutions", _TriggerOnOwnerVariantExecutions);
            SetEnum(lSerializedObject, "_AutomaticTargetSelection", (int)_AutomaticTargetSelection);
            SetObject(lSerializedObject, "_AutomaticTargetMarker", _AutomaticTargetMarker);
            SetEnum(lSerializedObject, "_AutomaticTargetUnitType", (int)_AutomaticTargetUnitType);
            SetEnum(lSerializedObject, "_HealthLossSourceRule", (int)_HealthLossSourceRule);
            SetEnum(lSerializedObject, "_HealthLossReactionTarget", (int)_HealthLossReactionTarget);
            SetObject(lSerializedObject, "_HealthLossReactionState", _HealthLossReactionState);
            SetInt(lSerializedObject, "_HealthLossReactionStateStacks", Mathf.Max(1, _HealthLossReactionStateStacks));
            SetBool(lSerializedObject, "_IncludeSummonsAsReactionBeneficiaries", _IncludeSummonsAsReactionBeneficiaries);
            SetBool(lSerializedObject, "_ClearReactionStateOnOwnerBeginTurn", _ClearReactionStateOnOwnerBeginTurn);
            ApplyAndSave(lSerializedObject);
            Creator_FileSaver.CreateAsset(lAsset, PassivesFolder, BuildFileName("Passive", lId, _DisplayName, "passive"));
            ContentLocalizationEditorUtility.EnsureEntries(GameLocalization.PassivesTable, lId, lAsset.EnglishDisplayName, lAsset.EnglishDescription);
            Close();
        }

        #endregion
    }
}
