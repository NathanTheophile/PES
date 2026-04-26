using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public sealed class CreateStateDefinitionWindow : EditorWindow
    {
        #region _____________________________| VALUES

        private string _Id = string.Empty;
        private string _DisplayName = "New State";
        private string _Description = string.Empty;
        private int _DurationTurns = 0;
        private int _MaxStacks = 1;
        private bool _IsPassiveMarker;
        private int _DamageModifierPerStack = 0;
        private int _DamageReductionPerStack = 0;
        private int _RangeModifierPerStack = 0;

        #endregion

        #region _____________________________| MENU

        [MenuItem("TacticalPort/Create/State Definition...")]
        public static void Open()
        {
            CreateStateDefinitionWindow lWindow = GetWindow<CreateStateDefinitionWindow>("Create State");
            lWindow.minSize = new Vector2(360f, 360f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            TacticalAssetEditorUtility.DrawTitle("Create State Definition", TacticalAssetEditorUtility.StatesFolder);

            _DisplayName = EditorGUILayout.TextField("Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("Description", _Description);

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);
            _DurationTurns = Mathf.Max(0, EditorGUILayout.IntField("Duration Turns", _DurationTurns));
            _MaxStacks = Mathf.Max(1, EditorGUILayout.IntField("Max Stacks", _MaxStacks));
            _IsPassiveMarker = EditorGUILayout.Toggle("Passive Marker", _IsPassiveMarker);

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Modifiers", EditorStyles.boldLabel);
            _DamageModifierPerStack = EditorGUILayout.IntField("Damage / Stack", _DamageModifierPerStack);
            _DamageReductionPerStack = Mathf.Max(0, EditorGUILayout.IntField("Reduction / Stack", _DamageReductionPerStack));
            _RangeModifierPerStack = EditorGUILayout.IntField("Range / Stack", _RangeModifierPerStack);

            if (TacticalAssetEditorUtility.DrawCreateButton("Create State Definition"))
                CreateAsset();
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = TacticalAssetEditorUtility.CreateSerializedAsset(out StateDefinition lAsset);

            string lId = TacticalAssetEditorUtility.SanitizeId(_Id, TacticalAssetEditorUtility.SanitizeId(_DisplayName, "state"));

            lSerializedObject.FindProperty("_Id").stringValue = lId;
            lSerializedObject.FindProperty("_DisplayName").stringValue = string.IsNullOrWhiteSpace(_DisplayName) ? lId : _DisplayName.Trim();
            lSerializedObject.FindProperty("_Description").stringValue = _Description ?? string.Empty;
            lSerializedObject.FindProperty("_DurationTurns").intValue = Mathf.Max(0, _DurationTurns);
            lSerializedObject.FindProperty("_MaxStacks").intValue = Mathf.Max(1, _MaxStacks);
            lSerializedObject.FindProperty("_IsPassiveMarker").boolValue = _IsPassiveMarker;
            lSerializedObject.FindProperty("_DamageModifierPerStack").intValue = _DamageModifierPerStack;
            lSerializedObject.FindProperty("_DamageReductionPerStack").intValue = Mathf.Max(0, _DamageReductionPerStack);
            lSerializedObject.FindProperty("_RangeModifierPerStack").intValue = _RangeModifierPerStack;

            TacticalAssetEditorUtility.ApplyAndSave(lSerializedObject);

            string lFileName = TacticalAssetEditorUtility.BuildFileName("State", lId, _DisplayName, "state");
            TacticalAssetEditorUtility.CreateAsset(lAsset, TacticalAssetEditorUtility.StatesFolder, lFileName);
            Close();
        }

        #endregion
    }
}
