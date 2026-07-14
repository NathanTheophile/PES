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

            _DisplayName = EditorGUILayout.TextField("Display Name", _DisplayName);
            _Id = EditorGUILayout.TextField("Id", _Id);
            _Description = EditorGUILayout.TextField("Description", _Description);

            DrawSectionHeader("Gameplay");
            _Trigger = (PassiveTrigger)EditorGUILayout.EnumPopup("Trigger", _Trigger);
            _StateProgression = (StateProgressionDefinition)EditorGUILayout.ObjectField(
                "State Progression",
                _StateProgression,
                typeof(StateProgressionDefinition),
                false);
            _ProgressionStepsPerTrigger = Mathf.Max(1, EditorGUILayout.IntField("Progression Steps / Trigger", _ProgressionStepsPerTrigger));
            _TriggerOnOwnerVariantExecutions = EditorGUILayout.Toggle("Trigger On Owner Variant", _TriggerOnOwnerVariantExecutions);

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
            ApplyAndSave(lSerializedObject);
            Creator_FileSaver.CreateAsset(lAsset, PassivesFolder, BuildFileName("Passive", lId, _DisplayName, "passive"));
            Close();
        }

        #endregion
    }
}
