#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using UnityEditor;
using UnityEngine;
using static TacticalPort.EditorTools.Creator_FileSaver;

namespace TacticalPort.EditorTools
{
    public sealed class Creator_State : EditorWindow
    {
        #region _____________________________/ VALUES

        private string _Id = string.Empty;
        private string _DisplayName = "New State";
        private string _Description = string.Empty;
        private int _DurationTurns = 0;
        private int _MaxStacks = 1;
        private bool _IsPassiveMarker;
        private int _DamageModifierPerStack = 0;
        private int _DamageReductionPerStack = 0;
        private int _RangeModifierPerStack = 0;
        private int _ActionPointModifierPerStack = 0;
        private int _MovementModifierPerStack = 0;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Create/State Definition...")]
        public static void Open()
        {
            Creator_State lWindow = GetWindow<Creator_State>("Create State");
            lWindow.minSize = new Vector2(360f, 360f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            DrawTitle("Create State Definition", StatesFolder);

            _DisplayName = EditorGUILayout.TextField("Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("Description", _Description);

            DrawSectionHeader("Rules");
            _DurationTurns = Mathf.Max(0, EditorGUILayout.IntField("Duration Turns", _DurationTurns));
            _MaxStacks = Mathf.Max(1, EditorGUILayout.IntField("Max Stacks", _MaxStacks));
            _IsPassiveMarker = EditorGUILayout.Toggle("Passive Marker", _IsPassiveMarker);

            DrawSectionHeader("Modifiers");
            _DamageModifierPerStack = EditorGUILayout.IntField("Damage / Stack", _DamageModifierPerStack);
            _DamageReductionPerStack = Mathf.Max(0, EditorGUILayout.IntField("Reduction / Stack", _DamageReductionPerStack));
            _RangeModifierPerStack = EditorGUILayout.IntField("Range / Stack", _RangeModifierPerStack);
            _ActionPointModifierPerStack = EditorGUILayout.IntField("AP / Stack", _ActionPointModifierPerStack);
            _MovementModifierPerStack = EditorGUILayout.IntField("Move / Stack", _MovementModifierPerStack);

            if (DrawCreateButton("Create State Definition"))
                CreateAsset();
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = CreateSerializedAsset(out StateDefinition lAsset);

            string lId = SanitizeId(_Id, SanitizeId(_DisplayName, "state"));

            SetString(lSerializedObject, "_Id", lId);
            SetString(lSerializedObject, "_DisplayName", string.IsNullOrWhiteSpace(_DisplayName) ? lId : _DisplayName.Trim());
            SetString(lSerializedObject, "_Description", _Description);
            SetInt(lSerializedObject, "_DurationTurns", Mathf.Max(0, _DurationTurns));
            SetInt(lSerializedObject, "_MaxStacks", Mathf.Max(1, _MaxStacks));
            SetBool(lSerializedObject, "_IsPassiveMarker", _IsPassiveMarker);
            SetInt(lSerializedObject, "_DamageModifierPerStack", _DamageModifierPerStack);
            SetInt(lSerializedObject, "_DamageReductionPerStack", Mathf.Max(0, _DamageReductionPerStack));
            SetInt(lSerializedObject, "_RangeModifierPerStack", _RangeModifierPerStack);
            SetInt(lSerializedObject, "_ActionPointModifierPerStack", _ActionPointModifierPerStack);
            SetInt(lSerializedObject, "_MovementModifierPerStack", _MovementModifierPerStack);

            ApplyAndSave(lSerializedObject);

            string lFileName = BuildFileName("State", lId, _DisplayName, "state");
            Creator_FileSaver.CreateAsset(lAsset, StatesFolder, lFileName);
            Close();
        }

        #endregion
    }
}
