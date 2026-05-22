#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Editor
#endregion

using TacticalPort.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    [CustomEditor(typeof(RuntimeServicesBootstrap))]
    public sealed class CustomInspector_RuntimeServicesBootstrap : UnityEditor.Editor
    {
        #region _____________________________/ VALUES

        private const float TitleSpacing = 8f;
        private const float ItemSpacing = 1f;
        private const float SectionSpacing = 20f;

        private static readonly string[] Tabs = { "Scene Flow", "Services", "Optional" };

        private int _CurrentTabIndex;

        #endregion

        #region _____________________________| GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHelp("Persistent runtime root for S_Bootstrap. It configures matchmaking services, owns scene handoff to the frontend, and can survive scene loads.");

            _CurrentTabIndex = GUILayout.Toolbar(_CurrentTabIndex, Tabs);

            switch (_CurrentTabIndex)
            {
                case 0:
                    DrawSceneFlowTab();
                    break;

                case 1:
                    DrawServicesTab();
                    break;

                case 2:
                    DrawOptionalTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSceneFlowTab()
        {
            DrawSection("Scene Flow", "_MainMenuSceneName", "_LoadMenuOnStart", "_KeepAliveAcrossScenes");
            DrawSection("Transition", "_SceneTransitionController", "_InitialLoadMessage");
        }

        private void DrawServicesTab()
        {
            DrawSection("UGS Services", "_PlayerIdentityService", "_QuickMatchService");
            DrawSection("Runtime Wiring", "_QuickMatchFlowController", "_MatchRuntimeContext", "_MatchRuntimeSessionLifecycle");
        }

        private void DrawOptionalTab()
        {
            DrawSection("Server Allocation", "_GameServerAllocatorSource");
            DrawSection("Private Lobby", "_PartyLobbyServiceSource");
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
