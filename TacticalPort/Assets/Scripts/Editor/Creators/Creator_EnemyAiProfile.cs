#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;
using static TacticalPort.EditorTools.Creator_FileSaver;
using TacticalPort.Core;

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
            DrawTitle("Create Enemy AI Profile", EnemyAiProfilesFolder);

            _Id = EditorGUILayout.TextField("Id", _Id);

            DrawSectionHeader("Global AI Behaviour");
            _TargetPriority = (EnemyAiTargetPriority)EditorGUILayout.EnumPopup("Target Priority", _TargetPriority);
            _MovementPolicy = (EnemyAiMovementPolicy)EditorGUILayout.EnumPopup("Movement Policy", _MovementPolicy);
            _PreferredDistance = Mathf.Max(0, EditorGUILayout.IntField("Preferred Distance", _PreferredDistance));
            _ThreatRadius = Mathf.Max(0, EditorGUILayout.IntField("Threat Radius", _ThreatRadius));
            _KiteAfterSuccessfulAction = EditorGUILayout.Toggle("Kite After Action", _KiteAfterSuccessfulAction);

            DrawSectionHeader("Scoring Weights");
            _DamageWeight = Mathf.Max(0, EditorGUILayout.IntField("Damage Weight", _DamageWeight));
            _HealWeight = Mathf.Max(0, EditorGUILayout.IntField("Heal Weight", _HealWeight));
            _KillConfirmWeight = Mathf.Max(0, EditorGUILayout.IntField("Kill Confirm Weight", _KillConfirmWeight));
            _AoeWeight = Mathf.Max(0, EditorGUILayout.IntField("AoE Weight", _AoeWeight));
            _SafetyWeight = Mathf.Max(0, EditorGUILayout.IntField("Safety Weight", _SafetyWeight));

            DrawSectionHeader("Debug");
            _DebugDecisions = EditorGUILayout.Toggle("Debug Decisions", _DebugDecisions);
            EditorGUILayout.HelpBox("Skill loadouts and optional per-skill AI overrides are configured on Unit Definitions. Profile weights tune global AI tendencies without duplicating skill data.", MessageType.Info);

            if (DrawCreateButton("Create Enemy AI Profile"))
                CreateAsset();
        }

        #endregion

        #region _____________________________| CREATE

        private void CreateAsset()
        {
            SerializedObject lSerializedObject = CreateSerializedAsset(out EnemyAiProfileDefinition lAsset);

            string lId = SanitizeId(_Id, "enemy_ai_profile");

            SetEnum(lSerializedObject, "_TargetPriority", (int)_TargetPriority);
            SetEnum(lSerializedObject, "_MovementPolicy", (int)_MovementPolicy);
            SetInt(lSerializedObject, "_PreferredDistance", Mathf.Max(0, _PreferredDistance));
            SetInt(lSerializedObject, "_ThreatRadius", Mathf.Max(0, _ThreatRadius));
            SetBool(lSerializedObject, "_KiteAfterSuccessfulAction", _KiteAfterSuccessfulAction);
            SetInt(lSerializedObject, "_DamageWeight", Mathf.Max(0, _DamageWeight));
            SetInt(lSerializedObject, "_HealWeight", Mathf.Max(0, _HealWeight));
            SetInt(lSerializedObject, "_KillConfirmWeight", Mathf.Max(0, _KillConfirmWeight));
            SetInt(lSerializedObject, "_AoeWeight", Mathf.Max(0, _AoeWeight));
            SetInt(lSerializedObject, "_SafetyWeight", Mathf.Max(0, _SafetyWeight));
            SetBool(lSerializedObject, "_DebugDecisions", _DebugDecisions);

            ApplyAndSave(lSerializedObject);

            string lFileName = BuildFileName("EnemyAiProfile", lId, lId, "enemy_ai_profile");
            Creator_FileSaver.CreateAsset(lAsset, EnemyAiProfilesFolder, lFileName);
            Close();
        }

        #endregion
    }
}
