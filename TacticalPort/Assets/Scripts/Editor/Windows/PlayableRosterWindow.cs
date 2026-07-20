#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Editor tooling
#endregion

using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public sealed class PlayableRosterWindow : EditorWindow
    {
        #region _____________________________/ VALUES

        private const string AssetPath = "Assets/Data/PlayableRoster.asset";

        private PlayableRosterDefinition _Roster;
        private SerializedObject _SerializedRoster;
        private SerializedProperty _Units;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Browse/Playable Characters...", priority = 110)]
        private static void Open()
        {
            PlayableRosterWindow lWindow = GetWindow<PlayableRosterWindow>("Characters List");
            lWindow.minSize = new Vector2(420f, 260f);
            lWindow.Show();
        }

        #endregion

        #region _____________________________| UNITY

        private void OnEnable() => LoadRoster();

        private void OnGUI()
        {
            if (_Roster == null)
            {
                EditorGUILayout.HelpBox($"Playable roster asset not found at '{AssetPath}'.", MessageType.Error);
                if (GUILayout.Button("Reload"))
                    LoadRoster();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This ordered list is the source of truth for playable characters. Summons and test units must not be added.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Roster Asset", _Roster, typeof(PlayableRosterDefinition), false);

            EditorGUILayout.Space();
            _SerializedRoster.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_Units, new GUIContent("Playable Characters"), true);
            if (EditorGUI.EndChangeCheck())
            {
                _SerializedRoster.ApplyModifiedProperties();
                EditorUtility.SetDirty(_Roster);
            }

            if (_Roster.TryGetValidationError(out string lError))
                EditorGUILayout.HelpBox(lError, MessageType.Error);
        }

        #endregion

        #region _____________________________| HELPERS

        private void LoadRoster()
        {
            _Roster = AssetDatabase.LoadAssetAtPath<PlayableRosterDefinition>(AssetPath);
            _SerializedRoster = _Roster != null ? new SerializedObject(_Roster) : null;
            _Units = _SerializedRoster?.FindProperty("_Units");
        }

        #endregion
    }
}
