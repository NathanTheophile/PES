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
    [CustomEditor(typeof(MatchRuntimeSessionLifecycle))]
    public sealed class CustomInspector_MatchRuntimeSessionLifecycle : UnityEditor.Editor
    {
        #region _____________________________/ VALUES

        private const float TitleSpacing = 8f;
        private const float ItemSpacing = 1f;
        private const float SectionSpacing = 20f;

        private static readonly string[] Tabs = { "References", "Cleanup", "Frontend", "Debug" };

        private int _CurrentTabIndex;

        #endregion

        #region _____________________________| GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHelp("Match session cleanup owner. It stops PurrNet, clears MatchRuntimeContext, resets quick match state, and releases external resources when needed.");

            _CurrentTabIndex = GUILayout.Toolbar(_CurrentTabIndex, Tabs);

            switch (_CurrentTabIndex)
            {
                case 0:
                    DrawReferencesTab();
                    break;

                case 1:
                    DrawCleanupTab();
                    break;

                case 2:
                    DrawFrontendTab();
                    break;

                case 3:
                    DrawDebugTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawReferencesTab() =>
            DrawSection("Runtime References", "_MatchContext", "_QuickMatchFlowController", "_PurrNetMatchConnector");

        private void DrawCleanupTab()
        {
            DrawSection("Cleanup Services", "_GameServerAllocatorSource", "_PartyLobbyServiceSource");
            DrawSection("Cleanup Policy", "_ReleaseServerAllocationOnSessionEnd");
        }

        private void DrawFrontendTab() =>
            DrawSection("Main Menu Return", "_MainMenuSceneName", "_ClearMatchOnMainMenuReturn");

        private void DrawDebugTab() =>
            DrawSection("Logging", "_LogEvents");

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
