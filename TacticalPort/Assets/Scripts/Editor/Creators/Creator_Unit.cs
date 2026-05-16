#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.View;
using UnityEditor;
using UnityEngine;
using static TacticalPort.EditorTools.Creator_FileSaver;

namespace TacticalPort.EditorTools
{
    public sealed class Creator_Unit : EditorWindow
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _Id = string.Empty;
        [SerializeField] private string _DisplayName = "New Unit";
        [SerializeField] private string _Description = string.Empty;
        [SerializeField] private Team _Team = Team.TeamA;
        [SerializeField] private int _MaxHealth = 10;
        [SerializeField] private int _MoveRange = 4;
        [SerializeField] private int _ActionPointsPerTurn = 1;
        [SerializeField] private int _Initiative = 10;
        [SerializeField] private int _PushDamageBonus = 0;
        [SerializeField] private int _FootprintWidth = 1;
        [SerializeField] private int _FootprintHeight = 1;
        [SerializeField] private UnitView _UnitViewPrefab;
        [SerializeField] private Sprite _Portrait;
        [SerializeField] private Color _Tint = Color.white;
        [SerializeField] private EnemyAiProfileDefinition _EnemyAiProfile;
        [SerializeField] private List<SkillDefinition> _Skills = new List<SkillDefinition>();
        [SerializeField] private List<UnitStateEntry> _BaseStates = new List<UnitStateEntry>();
        [SerializeField] private List<UnitPhaseStateDefinition> _PhaseStates = new List<UnitPhaseStateDefinition>();

        private SerializedObject _SerializedObject;
        private UnitDefinition _AiOverrideBuffer;
        private SerializedObject _AiOverrideSerializedObject;

        private const string SkillAiOverridesPropertyName = "_SkillAiOverrides";

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Create/Unit Definition...")]
        public static void Open()
        {
            Creator_Unit lWindow = GetWindow<Creator_Unit>("Create Unit");
            lWindow.minSize = new Vector2(400f, 500f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            _SerializedObject ??= new SerializedObject(this);
            _SerializedObject.Update();

            DrawTitle("Create Unit Definition", UnitsFolder);

            _DisplayName = EditorGUILayout.TextField("Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("Description", _Description);

            DrawSectionHeader("Stats");
            _Team = (Team)EditorGUILayout.EnumPopup("Team", _Team);
            _MaxHealth = Mathf.Max(1, EditorGUILayout.IntField("Max Health", _MaxHealth));
            _MoveRange = Mathf.Max(0, EditorGUILayout.IntField("Move Range", _MoveRange));
            _ActionPointsPerTurn = Mathf.Max(0, EditorGUILayout.IntField("AP / Turn", _ActionPointsPerTurn));
            _Initiative = Mathf.Max(0, EditorGUILayout.IntField("Initiative", _Initiative));
            _PushDamageBonus = Mathf.Max(0, EditorGUILayout.IntField("Push Damage Bonus", _PushDamageBonus));
            _FootprintWidth = Mathf.Max(1, EditorGUILayout.IntField("Footprint Width", _FootprintWidth));
            _FootprintHeight = Mathf.Max(1, EditorGUILayout.IntField("Footprint Height", _FootprintHeight));

            DrawSectionHeader("Presentation");
            _UnitViewPrefab = (UnitView)EditorGUILayout.ObjectField("Unit View Prefab", _UnitViewPrefab, typeof(UnitView), false);
            _Portrait = (Sprite)EditorGUILayout.ObjectField("Portrait", _Portrait, typeof(Sprite), false);
            _Tint = EditorGUILayout.ColorField("Tint", _Tint);

            GUILayout.Space(6f);
            DrawSkills();

            DrawSectionHeader("AI");
            _EnemyAiProfile = (EnemyAiProfileDefinition)EditorGUILayout.ObjectField("AI Profile", _EnemyAiProfile, typeof(EnemyAiProfileDefinition), false);
            DrawSkillAiOverrides();

            GUILayout.Space(6f);
            DrawStateEntries("_BaseStates", "Base States", "Add Base State", "State");
            GUILayout.Space(6f);
            DrawStateEntries("_PhaseStates", "Phase States", "Add Phase State", "Phase");

            _SerializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (DrawCreateButton("Create Unit Definition"))
                CreateAsset();
        }

        private void DrawSkills()
        {
            EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);

            int lRemoveIndex = -1;
            for (int lIndex = 0; lIndex < _Skills.Count; lIndex++)
            {
                EditorGUILayout.BeginHorizontal();
                _Skills[lIndex] = (SkillDefinition)EditorGUILayout.ObjectField($"Skill {lIndex + 1}", _Skills[lIndex], typeof(SkillDefinition), false);
                if (GUILayout.Button("X", GUILayout.Width(24f)))
                    lRemoveIndex = lIndex;
                EditorGUILayout.EndHorizontal();
            }

            if (lRemoveIndex >= 0)
                _Skills.RemoveAt(lRemoveIndex);

            if (GUILayout.Button("Add Skill"))
                _Skills.Add(null);
        }

        private void DrawSkillAiOverrides()
        {
            EnsureAiOverrideBuffer();
            _AiOverrideSerializedObject?.Update();

            SerializedProperty lOverridesProperty = _AiOverrideSerializedObject?.FindProperty(SkillAiOverridesPropertyName);
            if (lOverridesProperty == null)
            {
                EditorGUILayout.HelpBox("Per-skill AI overrides will appear here once UnitDefinition exposes _SkillAiOverrides. Skills are still configured only in the Skills section above.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Skill AI Overrides", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Optional overrides are generated from this unit's Skills list, so each skill is selected only once.", MessageType.Info);

            if (GUILayout.Button("Sync AI Overrides From Skills"))
                SkillAiOverrideEditorUtility.SyncFromSkills(lOverridesProperty, _Skills);

            SkillAiOverrideEditorUtility.DrawSkillBoundOverrides(
                lOverridesProperty,
                _Skills,
                "Add skills above before configuring per-skill AI overrides.");
            _AiOverrideSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DrawStateEntries(string pPropertyName, string pLabel, string pAddButtonLabel, string pEntryLabel)
        {
            EditorGUILayout.LabelField(pLabel, EditorStyles.boldLabel);

            SerializedProperty lListProperty = _SerializedObject.FindProperty(pPropertyName);

            int lRemoveIndex = -1;
            for (int lIndex = 0; lIndex < lListProperty.arraySize; lIndex++)
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty lEntryProperty = lListProperty.GetArrayElementAtIndex(lIndex);
                EditorGUILayout.PropertyField(lEntryProperty, new GUIContent($"{pEntryLabel} {lIndex + 1}"), true);
                if (GUILayout.Button($"Remove {pEntryLabel}"))
                    lRemoveIndex = lIndex;
                EditorGUILayout.EndVertical();
            }

            if (lRemoveIndex >= 0)
                lListProperty.DeleteArrayElementAtIndex(lRemoveIndex);

            if (GUILayout.Button(pAddButtonLabel))
                lListProperty.InsertArrayElementAtIndex(lListProperty.arraySize);
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = CreateSerializedAsset(out UnitDefinition lAsset);

            string lId = SanitizeId(_Id, SanitizeId(_DisplayName, "unit"));

            SetString(lSerializedObject, "_Id", lId);
            SetString(lSerializedObject, "_DisplayName", string.IsNullOrWhiteSpace(_DisplayName) ? lId : _DisplayName.Trim());
            SetString(lSerializedObject, "_Description", _Description);
            SetEnum(lSerializedObject, "_Team", (int)_Team);
            SetInt(lSerializedObject, "_MaxHealth", Mathf.Max(1, _MaxHealth));
            SetInt(lSerializedObject, "_MoveRange", Mathf.Max(0, _MoveRange));
            SetInt(lSerializedObject, "_ActionPointsPerTurn", Mathf.Max(0, _ActionPointsPerTurn));
            SetInt(lSerializedObject, "_Initiative", Mathf.Max(0, _Initiative));
            SetInt(lSerializedObject, "_PushDamageBonus", Mathf.Max(0, _PushDamageBonus));
            SetInt(lSerializedObject, "_FootprintWidth", Mathf.Max(1, _FootprintWidth));
            SetInt(lSerializedObject, "_FootprintHeight", Mathf.Max(1, _FootprintHeight));
            SetObject(lSerializedObject, "_UnitViewPrefab", _UnitViewPrefab);
            SetObject(lSerializedObject, "_Portrait", _Portrait);
            SetColor(lSerializedObject, "_Tint", _Tint);
            SetObject(lSerializedObject, "_EnemyAiProfile", _EnemyAiProfile);

            SerializedProperty lSkillsProperty = lSerializedObject.FindProperty("_Skills");
            lSkillsProperty.arraySize = _Skills.Count;
            for (int lIndex = 0; lIndex < _Skills.Count; lIndex++)
                lSkillsProperty.GetArrayElementAtIndex(lIndex).objectReferenceValue = _Skills[lIndex];

            CopySerializableEntries(lSerializedObject.FindProperty("_BaseStates"), _SerializedObject.FindProperty("_BaseStates"));
            CopySerializableEntries(lSerializedObject.FindProperty("_PhaseStates"), _SerializedObject.FindProperty("_PhaseStates"));
            CopySerializableEntries(lSerializedObject.FindProperty(SkillAiOverridesPropertyName), _AiOverrideSerializedObject?.FindProperty(SkillAiOverridesPropertyName));

            ApplyAndSave(lSerializedObject);

            string lFileName = BuildFileName("Unit", lId, _DisplayName, "unit");
            Creator_FileSaver.CreateAsset(lAsset, UnitsFolder, lFileName);
            Close();
        }

        private static void CopySerializableEntries(SerializedProperty pTargetProperty, SerializedProperty pSourceProperty)
        {
            if (pTargetProperty == null || pSourceProperty == null)
                return;

            pTargetProperty.arraySize = pSourceProperty.arraySize;
            for (int lIndex = 0; lIndex < pSourceProperty.arraySize; lIndex++)
                pTargetProperty.GetArrayElementAtIndex(lIndex).boxedValue = pSourceProperty.GetArrayElementAtIndex(lIndex).boxedValue;
        }

        private void EnsureAiOverrideBuffer()
        {
            if (_AiOverrideBuffer != null)
                return;

            _AiOverrideBuffer = CreateInstance<UnitDefinition>();
            _AiOverrideBuffer.hideFlags = HideFlags.HideAndDontSave;
            _AiOverrideSerializedObject = new SerializedObject(_AiOverrideBuffer);
        }

        private void OnDestroy()
        {
            if (_AiOverrideBuffer == null)
                return;

            DestroyImmediate(_AiOverrideBuffer);
            _AiOverrideBuffer = null;
            _AiOverrideSerializedObject = null;
        }

        #endregion
    }
}
