#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Editor
#endregion

using TacticalPort.Networking;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    [CustomEditor(typeof(PurrNetMatchConnector))]
    public sealed class CustomInspector_PurrNetMatchConnector : UnityEditor.Editor
    {
        #region _____________________________/ VALUES

        private const float TitleSpacing = 8f;
        private const float ItemSpacing = 1f;
        private const float SectionSpacing = 20f;

        private static readonly string[] Tabs = { "References", "Role", "Endpoint", "Lifecycle", "Debug" };

        private int _CurrentTabIndex;

        #endregion

        #region _____________________________| GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHelp("PurrNet connector driven by MatchRuntimeContext. It chooses the role, prepares UDP or Relay transport, and handles match manifest handshakes.");

            _CurrentTabIndex = GUILayout.Toolbar(_CurrentTabIndex, Tabs);

            switch (_CurrentTabIndex)
            {
                case 0:
                    DrawReferencesTab();
                    break;

                case 1:
                    DrawRoleTab();
                    break;

                case 2:
                    DrawEndpointTab();
                    break;

                case 3:
                    DrawLifecycleTab();
                    break;

                case 4:
                    DrawDebugTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawReferencesTab()
        {
            DrawSection("Runtime References", "_MatchContext", "_NetworkManager");
            DrawSection("Transports", "_UdpTransport", "_UtpTransport");
        }

        private void DrawRoleTab() =>
            DrawSection("Role Policy", "_ConnectionRole", "_AllowCloneServerRole", "_ConnectOnMatchFound", "_AllowServerWithoutMatchContext");

        private void DrawEndpointTab() =>
            DrawSection("Endpoint Policy", "_UseMatchEndpointWhenAvailable", "_UseEdgeGapInjectedServerPort", "_EdgeGapPortName");

        private void DrawLifecycleTab()
        {
            DrawSection("NetworkManager Ownership", "_DisableNetworkManagerAutoStart");
            DrawSection("Session Cleanup", "_StopWhenMatchContextClears", "_AllowServerMatchReplacementInMainMenu", "_MainMenuSceneName");
        }

        private void DrawDebugTab()
        {
            DrawSection("Retry", "_ConnectionRetrySeconds", "_CompositionHandshakeRetrySeconds");
            DrawSection("Logging", "_LogEvents");
            DrawConnectionState();
        }

        private void DrawConnectionState()
        {
            if (!Application.isPlaying)
                return;

            PurrNetMatchConnector lConnector = target as PurrNetMatchConnector;
            if (lConnector == null)
                return;

            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField("Runtime State", EditorStyles.toolbarButton);
            EditorGUILayout.Space(TitleSpacing);
            EditorGUILayout.HelpBox(lConnector.DescribeConnectionState(), MessageType.None);
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
