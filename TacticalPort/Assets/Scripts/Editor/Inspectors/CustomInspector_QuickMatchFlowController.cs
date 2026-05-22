#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Editor
#endregion

using TacticalPort.Matchmaking;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    [CustomEditor(typeof(QuickMatchFlowController))]
    public sealed class CustomInspector_QuickMatchFlowController : UnityEditor.Editor
    {
        #region _____________________________/ VALUES

        private const float TitleSpacing = 8f;
        private const float ItemSpacing = 1f;
        private const float SectionSpacing = 20f;

        private static readonly string[] Tabs = { "Services", "Request", "Policy", "Debug" };

        private int _CurrentTabIndex;

        #endregion

        #region _____________________________| GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHelp("Quick match orchestrator. It signs in the player, creates and polls a Matchmaker ticket, applies the connection policy, and fills MatchRuntimeContext.");

            _CurrentTabIndex = GUILayout.Toolbar(_CurrentTabIndex, Tabs);

            switch (_CurrentTabIndex)
            {
                case 0:
                    DrawServicesTab();
                    break;

                case 1:
                    DrawRequestTab();
                    break;

                case 2:
                    DrawPolicyTab();
                    break;

                case 3:
                    DrawDebugTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawServicesTab() =>
            DrawSection("Service Sources", "_PlayerIdentityServiceSource", "_QuickMatchServiceSource", "_GameServerAllocatorSource", "_MatchContext");

        private void DrawRequestTab()
        {
            DrawSection("Ticket Request", "_QueueName", "_TeamPresetId");
            DrawSection("Polling", "_PollIntervalSeconds", "_TimeoutSeconds");
        }

        private void DrawPolicyTab()
        {
            DrawSection("Connection Modes", "_ConnectionModeWithoutAllocator", "_ConnectionModeWithAllocator");
            DrawSection("Result Trust", "_ReportsRankedResultsWithAllocator");
            DrawSection("Cleanup", "_CancelPendingTicketOnDestroy");
        }

        private void DrawDebugTab()
        {
            DrawSection("Logging", "_LogEvents");
            DrawRuntimeStatus();
        }

        private void DrawRuntimeStatus()
        {
            QuickMatchFlowController lController = target as QuickMatchFlowController;
            if (lController == null)
                return;

            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TitleSpacing);
            EditorGUILayout.LabelField("Is Running", lController.IsRunning.ToString());
            EditorGUILayout.LabelField("Last Status", lController.LastSnapshot.Status.ToString());
            EditorGUILayout.LabelField("Last Message", lController.LastSnapshot.Message);
        }

        private static void DrawHelp(string pMessage)
        {
            EditorGUILayout.HelpBox(pMessage, MessageType.Info);
            EditorGUILayout.Space(TitleSpacing);
        }

        private void DrawSection(string pHeader, params string[] pPropertyNames)
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(pHeader, EditorStyles.toolbarButton);
            EditorGUILayout.Space(TitleSpacing);

            for (int lIndex = 0; lIndex < pPropertyNames.Length; lIndex++)
                DrawProperty(pPropertyNames[lIndex]);
        }

        private void DrawProperty(string pPropertyName)
        {
            SerializedProperty lProperty = serializedObject.FindProperty(pPropertyName);
            if (lProperty == null)
                return;

            EditorGUILayout.PropertyField(lProperty, true);
            EditorGUILayout.Space(ItemSpacing);
        }

        #endregion
    }
}
