#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public sealed class Creator_EnemyAiProfile : EditorWindow
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _Id = string.Empty;
        [SerializeField] private EnemyAiTargetPriority _TargetPriority = EnemyAiTargetPriority.WeakFirst;
        [SerializeField] private EnemyAiMovementPolicy _MovementPolicy = EnemyAiMovementPolicy.Auto;
        [SerializeField] private int _PreferredDistance = 0;
        [SerializeField] private int _ThreatRadius = 0;
        [SerializeField] private bool _KiteAfterSuccessfulAction;
        [SerializeField] private int _DamageWeight = 100;
        [SerializeField] private int _HealWeight = 100;
        [SerializeField] private int _KillConfirmWeight = 100;
        [SerializeField] private int _AoeWeight = 100;
        [SerializeField] private int _SafetyWeight = 100;
        [SerializeField] private bool _DebugDecisions;

        private SerializedObject _SerializedObject;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Create/Enemy AI Profile...")]
        public static void Open()
        {
            Creator_EnemyAiProfile lWindow = GetWindow<Creator_EnemyAiProfile>("Create Enemy AI Profile");
            lWindow.minSize = new Vector2(400f, 460f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            _SerializedObject ??= new SerializedObject(this);
            _SerializedObject.Update();

            Creator_FileSaver.DrawTitle("Create Enemy AI Profile", Creator_FileSaver.EnemyAiProfilesFolder);

            _Id = EditorGUILayout.TextField("Id", _Id);

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Global AI Behaviour", EditorStyles.boldLabel);
            _TargetPriority = (EnemyAiTargetPriority)EditorGUILayout.EnumPopup("Target Priority", _TargetPriority);
            _MovementPolicy = (EnemyAiMovementPolicy)EditorGUILayout.EnumPopup("Movement Policy", _MovementPolicy);
            _PreferredDistance = Mathf.Max(0, EditorGUILayout.IntField("Preferred Distance", _PreferredDistance));
            _ThreatRadius = Mathf.Max(0, EditorGUILayout.IntField("Threat Radius", _ThreatRadius));
            _KiteAfterSuccessfulAction = EditorGUILayout.Toggle("Kite After Action", _KiteAfterSuccessfulAction);

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Scoring Weights", EditorStyles.boldLabel);
            _DamageWeight = Mathf.Max(0, EditorGUILayout.IntField("Damage Weight", _DamageWeight));
            _HealWeight = Mathf.Max(0, EditorGUILayout.IntField("Heal Weight", _HealWeight));
            _KillConfirmWeight = Mathf.Max(0, EditorGUILayout.IntField("Kill Confirm Weight", _KillConfirmWeight));
            _AoeWeight = Mathf.Max(0, EditorGUILayout.IntField("AoE Weight", _AoeWeight));
            _SafetyWeight = Mathf.Max(0, EditorGUILayout.IntField("Safety Weight", _SafetyWeight));

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            _DebugDecisions = EditorGUILayout.Toggle("Debug Decisions", _DebugDecisions);
            EditorGUILayout.HelpBox("Skill loadouts and optional per-skill AI overrides are configured on Unit Definitions. Profile weights tune global AI tendencies without duplicating skill data.", MessageType.Info);

            _SerializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (Creator_FileSaver.DrawCreateButton("Create Enemy AI Profile"))
                CreateAsset();
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = Creator_FileSaver.CreateSerializedAsset(out EnemyAiProfileDefinition lAsset);

            string lId = Creator_FileSaver.SanitizeId(_Id, "enemy_ai_profile");

            lSerializedObject.FindProperty("_TargetPriority").enumValueIndex = (int)_TargetPriority;
            lSerializedObject.FindProperty("_MovementPolicy").enumValueIndex = (int)_MovementPolicy;
            lSerializedObject.FindProperty("_PreferredDistance").intValue = Mathf.Max(0, _PreferredDistance);
            lSerializedObject.FindProperty("_ThreatRadius").intValue = Mathf.Max(0, _ThreatRadius);
            lSerializedObject.FindProperty("_KiteAfterSuccessfulAction").boolValue = _KiteAfterSuccessfulAction;
            lSerializedObject.FindProperty("_DamageWeight").intValue = Mathf.Max(0, _DamageWeight);
            lSerializedObject.FindProperty("_HealWeight").intValue = Mathf.Max(0, _HealWeight);
            lSerializedObject.FindProperty("_KillConfirmWeight").intValue = Mathf.Max(0, _KillConfirmWeight);
            lSerializedObject.FindProperty("_AoeWeight").intValue = Mathf.Max(0, _AoeWeight);
            lSerializedObject.FindProperty("_SafetyWeight").intValue = Mathf.Max(0, _SafetyWeight);
            lSerializedObject.FindProperty("_DebugDecisions").boolValue = _DebugDecisions;

            Creator_FileSaver.ApplyAndSave(lSerializedObject);

            string lFileName = Creator_FileSaver.BuildFileName("EnemyAiProfile", lId, lId, "enemy_ai_profile");
            Creator_FileSaver.CreateAsset(lAsset, Creator_FileSaver.EnemyAiProfilesFolder, lFileName);
            Close();
        }

        #endregion
    }
}
