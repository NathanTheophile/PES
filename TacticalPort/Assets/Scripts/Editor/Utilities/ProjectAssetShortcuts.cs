#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Direct access to persistent project configuration assets.
#endregion

using TacticalPort.Data;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public static class ProjectAssetShortcuts
    {
        private const string DefaultThemePath = "Assets/Data/Themes/Theme_Default.asset";

        [MenuItem("Project/Browse/Active Theme", priority = 140)]
        private static void SelectActiveTheme()
        {
            ThemeDefinition lTheme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(DefaultThemePath);
            if (lTheme == null)
            {
                EditorUtility.DisplayDialog("Active Theme", $"Theme asset not found at '{DefaultThemePath}'.", "OK");
                return;
            }

            Selection.activeObject = lTheme;
            EditorGUIUtility.PingObject(lTheme);
        }
    }
}
