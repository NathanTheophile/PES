#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Project typography asset browser.
#endregion

using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public sealed class TypographyBrowserWindow : EditorWindow
    {
        private readonly List<TMP_FontAsset> _Fonts = new List<TMP_FontAsset>();
        private Vector2 _ScrollPosition;

        [MenuItem("Project/Browse/Typography...", priority = 130)]
        private static void Open()
        {
            TypographyBrowserWindow lWindow = GetWindow<TypographyBrowserWindow>("Typography");
            lWindow.minSize = new Vector2(420f, 280f);
            lWindow.Refresh();
            lWindow.Show();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => EditorApplication.projectChanged -= Refresh;

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"{_Fonts.Count} TMP font asset(s)", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    Refresh();
            }

            _ScrollPosition = EditorGUILayout.BeginScrollView(_ScrollPosition);
            for (int lIndex = 0; lIndex < _Fonts.Count; lIndex++)
                DrawFont(_Fonts[lIndex]);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawFont(TMP_FontAsset pFont)
        {
            if (pFont == null)
                return;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(AssetPreview.GetMiniThumbnail(pFont), GUILayout.Width(32f), GUILayout.Height(32f));
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(pFont.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(pFont), EditorStyles.miniLabel);
                }

                if (GUILayout.Button("Select", GUILayout.Width(58f)))
                    Selection.activeObject = pFont;

                if (GUILayout.Button("Ping", GUILayout.Width(48f)))
                    EditorGUIUtility.PingObject(pFont);
            }
        }

        private void Refresh()
        {
            _Fonts.Clear();
            string[] lGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            Array.Sort(lGuids, CompareFontPaths);

            for (int lIndex = 0; lIndex < lGuids.Length; lIndex++)
            {
                TMP_FontAsset lFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(lGuids[lIndex]));
                if (lFont != null)
                    _Fonts.Add(lFont);
            }

            Repaint();
        }

        private static int CompareFontPaths(string pLeftGuid, string pRightGuid) =>
            string.Compare(
                AssetDatabase.GUIDToAssetPath(pLeftGuid),
                AssetDatabase.GUIDToAssetPath(pRightGuid),
                StringComparison.OrdinalIgnoreCase);
    }
}
