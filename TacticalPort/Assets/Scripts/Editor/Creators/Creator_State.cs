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
        private int _MeleeDamageModifierPerStack = 0;
        private int _RangedDamageModifierPerStack = 0;
        private int _MeleeResistancePercentPerStack = 0;
        private int _RangedResistancePercentPerStack = 0;
        private int _GeneralResistancePercentPerStack = 0;
        private int _WearPercentPerStack = 0;
        private int _RangeModifierPerStack = 0;
        private int _EnergyModifierPerStack = 0;
        private int _MobilityModifierPerStack = 0;

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
            _DamageModifierPerStack = EditorGUILayout.IntField("General Damage % / Stack", _DamageModifierPerStack);
            _MeleeDamageModifierPerStack = EditorGUILayout.IntField("Melee Damage % / Stack", _MeleeDamageModifierPerStack);
            _RangedDamageModifierPerStack = EditorGUILayout.IntField("Ranged Damage % / Stack", _RangedDamageModifierPerStack);
            _MeleeResistancePercentPerStack = EditorGUILayout.IntField("Melee Resistance % / Stack", _MeleeResistancePercentPerStack);
            _RangedResistancePercentPerStack = EditorGUILayout.IntField("Ranged Resistance % / Stack", _RangedResistancePercentPerStack);
            _GeneralResistancePercentPerStack = EditorGUILayout.IntField("General Resistance % / Stack", _GeneralResistancePercentPerStack);
            _WearPercentPerStack = EditorGUILayout.IntField("Wear % / Stack", _WearPercentPerStack);
            _RangeModifierPerStack = EditorGUILayout.IntField("Range / Stack", _RangeModifierPerStack);
            _EnergyModifierPerStack = EditorGUILayout.IntField("Energy / Stack", _EnergyModifierPerStack);
            _MobilityModifierPerStack = EditorGUILayout.IntField("Mobility / Stack", _MobilityModifierPerStack);

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
            SetInt(lSerializedObject, "_MeleeDamageModifierPerStack", _MeleeDamageModifierPerStack);
            SetInt(lSerializedObject, "_RangedDamageModifierPerStack", _RangedDamageModifierPerStack);
            SetInt(lSerializedObject, "_MeleeResistancePercentPerStack", _MeleeResistancePercentPerStack);
            SetInt(lSerializedObject, "_RangedResistancePercentPerStack", _RangedResistancePercentPerStack);
            SetInt(lSerializedObject, "_GeneralResistancePercentPerStack", _GeneralResistancePercentPerStack);
            SetInt(lSerializedObject, "_WearPercentPerStack", _WearPercentPerStack);
            SetInt(lSerializedObject, "_RangeModifierPerStack", _RangeModifierPerStack);
            SetInt(lSerializedObject, "_EnergyModifierPerStack", _EnergyModifierPerStack);
            SetInt(lSerializedObject, "_MobilityModifierPerStack", _MobilityModifierPerStack);

            ApplyAndSave(lSerializedObject);

            string lFileName = BuildFileName("State", lId, _DisplayName, "state");
            Creator_FileSaver.CreateAsset(lAsset, StatesFolder, lFileName);
            Close();
        }

        #endregion
    }
}
