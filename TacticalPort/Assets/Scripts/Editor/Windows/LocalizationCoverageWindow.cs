#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Read-only localization coverage dashboard for string table collections.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace TacticalPort.EditorTools
{
    public sealed class LocalizationCoverageWindow : EditorWindow
    {
        private sealed class EntryCoverage
        {
            public string Key;
            public readonly List<string> MissingLocales = new List<string>();
        }

        private sealed class CollectionCoverage
        {
            public StringTableCollection Collection;
            public string Name;
            public int EntryCount;
            public int TotalCells;
            public int MissingCells;
            public readonly Dictionary<string, int> MissingByLocale = new Dictionary<string, int>();
            public readonly List<EntryCoverage> Entries = new List<EntryCoverage>();
            public float Completion => TotalCells == 0 ? 1f : 1f - MissingCells / (float)TotalCells;
        }

        private readonly List<CollectionCoverage> _Reports = new List<CollectionCoverage>();
        private readonly List<Locale> _Locales = new List<Locale>();
        private Vector2 _CollectionsScroll;
        private Vector2 _EntriesScroll;
        private int _SelectedIndex;
        private bool _MissingOnly = true;
        private string _Search = string.Empty;

        [MenuItem("Project/Browse/Localization Coverage...", priority = 120)]
        private static void Open()
        {
            LocalizationCoverageWindow lWindow = GetWindow<LocalizationCoverageWindow>("Localization Coverage");
            lWindow.minSize = new Vector2(720f, 480f);
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
            DrawToolbar();

            if (_Reports.Count == 0)
            {
                EditorGUILayout.HelpBox("No string table collection was found.", MessageType.Warning);
                return;
            }

            DrawGlobalSummary();
            DrawCollections();
            DrawSelectedCollection();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    Refresh();

                GUILayout.Space(8f);
                GUILayout.Label("Entry filter", GUILayout.Width(70f));
                _Search = GUILayout.TextField(_Search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(160f));
                _MissingOnly = GUILayout.Toggle(_MissingOnly, "Missing only", EditorStyles.toolbarButton, GUILayout.Width(95f));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Unity Tables", EditorStyles.toolbarButton, GUILayout.Width(125f)))
                    LocalizationTablesWindow.ShowWindow();
            }
        }

        private void DrawGlobalSummary()
        {
            int lTotal = _Reports.Sum(pReport => pReport.TotalCells);
            int lMissing = _Reports.Sum(pReport => pReport.MissingCells);
            float lCompletion = lTotal == 0 ? 1f : 1f - lMissing / (float)lTotal;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                $"{_Reports.Count} collections  •  {_Locales.Count} locales  •  {lMissing} missing / {lTotal} translations",
                EditorStyles.boldLabel);

            Rect lRect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.ProgressBar(lRect, lCompletion, $"Overall coverage {lCompletion:P0}");
            EditorGUILayout.Space(4f);
        }

        private void DrawCollections()
        {
            EditorGUILayout.LabelField("Collections", EditorStyles.boldLabel);
            _CollectionsScroll = EditorGUILayout.BeginScrollView(_CollectionsScroll, GUILayout.Height(Mathf.Min(190f, 30f + _Reports.Count * 28f)));

            for (int lIndex = 0; lIndex < _Reports.Count; lIndex++)
            {
                CollectionCoverage lReport = _Reports[lIndex];
                GUIStyle lRowStyle = lIndex == _SelectedIndex ? EditorStyles.helpBox : GUIStyle.none;
                using (new EditorGUILayout.HorizontalScope(lRowStyle))
                {
                    if (GUILayout.Button(lReport.Name, EditorStyles.linkLabel, GUILayout.Width(150f)))
                        _SelectedIndex = lIndex;

                    GUILayout.Label($"{lReport.EntryCount} entries", GUILayout.Width(78f));
                    Rect lProgressRect = GUILayoutUtility.GetRect(150f, 18f, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(lProgressRect, lReport.Completion, $"{lReport.Completion:P0}");
                    GUILayout.Label(BuildLocaleSummary(lReport), GUILayout.Width(Mathf.Max(120f, _Locales.Count * 72f)));

                    if (GUILayout.Button("Open", GUILayout.Width(50f)))
                        LocalizationTablesWindow.ShowWindow(lReport.Collection);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedCollection()
        {
            _SelectedIndex = Mathf.Clamp(_SelectedIndex, 0, _Reports.Count - 1);
            CollectionCoverage lReport = _Reports[_SelectedIndex];

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{lReport.Name} entries", EditorStyles.boldLabel);
                if (GUILayout.Button("Select collection asset", GUILayout.Width(145f)))
                {
                    Selection.activeObject = lReport.Collection;
                    EditorGUIUtility.PingObject(lReport.Collection);
                }
            }

            IEnumerable<EntryCoverage> lEntries = lReport.Entries;
            if (_MissingOnly)
                lEntries = lEntries.Where(pEntry => pEntry.MissingLocales.Count > 0);
            if (!string.IsNullOrWhiteSpace(_Search))
                lEntries = lEntries.Where(pEntry => pEntry.Key.IndexOf(_Search, StringComparison.OrdinalIgnoreCase) >= 0);

            List<EntryCoverage> lVisibleEntries = lEntries.ToList();
            EditorGUILayout.LabelField($"{lVisibleEntries.Count} displayed", EditorStyles.miniLabel);
            _EntriesScroll = EditorGUILayout.BeginScrollView(_EntriesScroll);

            foreach (EntryCoverage lEntry in lVisibleEntries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(lEntry.Key, GUILayout.Height(18f), GUILayout.MinWidth(260f));
                    string lStatus = lEntry.MissingLocales.Count == 0
                        ? "Complete"
                        : "Missing: " + string.Join(", ", lEntry.MissingLocales);
                    GUILayout.Label(lStatus, lEntry.MissingLocales.Count == 0 ? EditorStyles.miniLabel : EditorStyles.boldLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void Refresh()
        {
            string lSelectedName = _Reports.Count > 0 && _SelectedIndex < _Reports.Count
                ? _Reports[_SelectedIndex].Name
                : string.Empty;

            _Reports.Clear();
            _Locales.Clear();
            _Locales.AddRange(LocalizationEditorSettings.GetLocales().OrderBy(pLocale => pLocale.Identifier.Code));

            foreach (StringTableCollection lCollection in LocalizationEditorSettings.GetStringTableCollections()
                         .OrderBy(pCollection => pCollection.TableCollectionName))
            {
                _Reports.Add(BuildReport(lCollection));
            }

            _SelectedIndex = Mathf.Max(0, _Reports.FindIndex(pReport => pReport.Name == lSelectedName));
            Repaint();
        }

        private CollectionCoverage BuildReport(StringTableCollection pCollection)
        {
            CollectionCoverage lReport = new CollectionCoverage
            {
                Collection = pCollection,
                Name = pCollection.TableCollectionName,
                EntryCount = pCollection.SharedData != null ? pCollection.SharedData.Entries.Count : 0
            };

            lReport.TotalCells = lReport.EntryCount * _Locales.Count;
            foreach (Locale lLocale in _Locales)
                lReport.MissingByLocale[lLocale.Identifier.Code] = 0;

            if (pCollection.SharedData == null)
                return lReport;

            foreach (SharedTableData.SharedTableEntry lSharedEntry in pCollection.SharedData.Entries.OrderBy(pEntry => pEntry.Key))
            {
                EntryCoverage lEntry = new EntryCoverage { Key = lSharedEntry.Key };
                foreach (Locale lLocale in _Locales)
                {
                    StringTable lTable = pCollection.GetTable(lLocale.Identifier) as StringTable;
                    string lValue = lTable?.GetEntry(lSharedEntry.Id)?.Value;
                    if (!string.IsNullOrWhiteSpace(lValue))
                        continue;

                    string lLocaleCode = lLocale.Identifier.Code;
                    lEntry.MissingLocales.Add(lLocaleCode);
                    lReport.MissingByLocale[lLocaleCode]++;
                    lReport.MissingCells++;
                }

                lReport.Entries.Add(lEntry);
            }

            return lReport;
        }

        private string BuildLocaleSummary(CollectionCoverage pReport) => string.Join(
            "  ",
            _Locales.Select(pLocale =>
            {
                string lCode = pLocale.Identifier.Code;
                return $"{lCode}: {pReport.MissingByLocale[lCode]} missing";
            }));
    }
}
