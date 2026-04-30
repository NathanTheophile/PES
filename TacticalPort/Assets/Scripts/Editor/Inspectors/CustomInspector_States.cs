#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    [CustomEditor(typeof(StateDefinition))]
    public class CustomInspector_StateDefinition : UnityEditor.Editor
    {
        #region _____________________________/ VALUES

        private const float TitleSpacing = 8f;
        private const float ItemSpacing = 1f;
        private const float SectionSpacing = 25f;

        private static readonly string[] Tabs = { "Metadata", "Rules", "Modifiers" };

        private int _CurrentTabIndex;

        #endregion

        #region _____________________________| GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _CurrentTabIndex = GUILayout.Toolbar(_CurrentTabIndex, Tabs);

            switch (_CurrentTabIndex)
            {
                case 0:
                    DrawSection("Naming", "_DisplayName", "_Id");
                    DrawSection("Misc", "_Description", "_Icon");
                    break;

                case 1:
                    DrawSection("Rules", "_DurationTurns", "_MaxStacks", "_IsPassiveMarker");
                    break;

                case 2:
                    DrawSection(
                        "Modifiers",
                        "_DamageModifierPerStack",
                        "_DamageReductionPerStack",
                        "_RangeModifierPerStack",
                        "_ActionPointModifierPerStack",
                        "_MovementModifierPerStack");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(string pHeader, params string[] pPropertyNames)
        {
            EditorGUILayout.Space(SectionSpacing);
            EditorGUILayout.LabelField(pHeader, EditorStyles.toolbarButton);
            EditorGUILayout.Space(TitleSpacing);

            for (int lIndex = 0; lIndex < pPropertyNames.Length; lIndex++)
            {
                SerializedProperty lProperty = serializedObject.FindProperty(pPropertyNames[lIndex]);
                if (lProperty == null)
                    continue;

                EditorGUILayout.PropertyField(lProperty);
                EditorGUILayout.Space(ItemSpacing);
            }
        }

        #endregion
    }
}
