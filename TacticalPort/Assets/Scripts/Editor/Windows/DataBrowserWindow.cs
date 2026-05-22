#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Editor
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    public sealed class DataBrowserWindow : EditorWindow
    {
        #region _____________________________/ TYPES

        private enum BrowserTab
        {
            Skills = 0,
            Units = 1,
            EnemyAi = 2
        }

        private readonly struct AssetEntry
        {
            public AssetEntry(ScriptableObject pAsset, string pPath, string pTitle, string pSubtitle)
            {
                Asset = pAsset;
                Path = pPath ?? string.Empty;
                Title = pTitle ?? string.Empty;
                Subtitle = pSubtitle ?? string.Empty;
            }

            public ScriptableObject Asset { get; }
            public string Path { get; }
            public string Title { get; }
            public string Subtitle { get; }
        }

        #endregion

        #region _____________________________/ VALUES

        private readonly List<AssetEntry> _Entries = new List<AssetEntry>();
        private readonly string[] _Tabs = new string[] { "Skills", "Units", "Enemy AI" };

        private BrowserTab _CurrentTab;
        private Vector2 _ScrollPosition;
        private string _Search = string.Empty;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Data Browser...")]
        public static void Open()
        {
            DataBrowserWindow lWindow = GetWindow<DataBrowserWindow>("Data Browser");
            lWindow.minSize = new Vector2(420f, 360f);
            lWindow.Refresh();
        }

        #endregion

        #region _____________________________| UNITY

        private void OnEnable()
        {
            Refresh();
        }

        private void OnProjectChange()
        {
            Refresh();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawCreateBar();
            DrawEntryList();
        }

        #endregion

        #region _____________________________| GUI

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            BrowserTab lNextTab = (BrowserTab)GUILayout.Toolbar((int)_CurrentTab, _Tabs, EditorStyles.toolbarButton, GUILayout.Width(260f));
            if (lNextTab != _CurrentTab)
            {
                _CurrentTab = lNextTab;
                _Search = string.Empty;
                _ScrollPosition = Vector2.zero;
                Refresh();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                Refresh();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search", GUILayout.Width(50f));
            _Search = EditorGUILayout.TextField(_Search);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
                _Search = string.Empty;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCreateBar()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{_Entries.Count} asset(s)", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ResolveCreateButtonLabel(), GUILayout.Width(140f)))
                OpenCreatorForCurrentTab();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private void DrawEntryList()
        {
            _ScrollPosition = EditorGUILayout.BeginScrollView(_ScrollPosition);

            int lVisibleCount = 0;
            for (int lIndex = 0; lIndex < _Entries.Count; lIndex++)
            {
                AssetEntry lEntry = _Entries[lIndex];
                if (!MatchesSearch(lEntry))
                    continue;

                lVisibleCount++;
                DrawEntry(lEntry);
            }

            if (lVisibleCount == 0)
                EditorGUILayout.HelpBox("No matching asset found.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawEntry(AssetEntry pEntry)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            Texture lIcon = AssetPreview.GetMiniThumbnail(pEntry.Asset);
            GUILayout.Label(lIcon, GUILayout.Width(20f), GUILayout.Height(20f));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(pEntry.Title, EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(pEntry.Subtitle))
                EditorGUILayout.LabelField(pEntry.Subtitle, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select", GUILayout.Width(58f)))
                SelectAsset(pEntry.Asset);

            if (GUILayout.Button("Ping", GUILayout.Width(45f)))
                PingAsset(pEntry.Asset);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            HandleEntryDoubleClick(pEntry);
        }

        private static void HandleEntryDoubleClick(AssetEntry pEntry)
        {
            Event lEvent = Event.current;
            if (lEvent == null || lEvent.type != EventType.MouseDown || lEvent.clickCount < 2)
                return;

            Rect lLastRect = GUILayoutUtility.GetLastRect();
            if (!lLastRect.Contains(lEvent.mousePosition))
                return;

            SelectAsset(pEntry.Asset);
            PingAsset(pEntry.Asset);
            lEvent.Use();
        }

        #endregion

        #region _____________________________| DATA

        private void Refresh()
        {
            _Entries.Clear();

            switch (_CurrentTab)
            {
                case BrowserTab.Units:
                    AddEntries<UnitDefinition>(BuildUnitEntry);
                    break;

                case BrowserTab.EnemyAi:
                    AddEntries<EnemyAiProfileDefinition>(BuildEnemyAiEntry);
                    break;

                default:
                    AddEntries<SkillDefinition>(BuildSkillEntry);
                    break;
            }

            _Entries.Sort((pLeft, pRight) => string.Compare(pLeft.Title, pRight.Title, StringComparison.OrdinalIgnoreCase));
        }

        private void AddEntries<T>(Func<T, string, AssetEntry> pBuildEntry) where T : ScriptableObject
        {
            string[] lGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int lIndex = 0; lIndex < lGuids.Length; lIndex++)
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuids[lIndex]);
                T lAsset = AssetDatabase.LoadAssetAtPath<T>(lPath);
                if (lAsset != null)
                    _Entries.Add(pBuildEntry(lAsset, lPath));
            }
        }

        private static AssetEntry BuildSkillEntry(SkillDefinition pSkill, string pPath)
        {
            string lSubtitle = $"{pSkill.Id} | {pSkill.PrimaryEffectType}";
            if (pSkill.AdditionalEffectType != SkillAdditionalEffectType.None)
                lSubtitle += $" + {pSkill.AdditionalEffectType}";

            lSubtitle += $" | Range {pSkill.RangeMin}-{pSkill.RangeMax} | AP {pSkill.ActionPointCost}";
            return new AssetEntry(pSkill, pPath, pSkill.DisplayName, lSubtitle);
        }

        private static AssetEntry BuildUnitEntry(UnitDefinition pUnit, string pPath)
        {
            string lSubtitle = $"{pUnit.Id} | {pUnit.Team} | HP {pUnit.MaxHealth} | AP {pUnit.ActionPointsPerTurn} | Skills {pUnit.Skills.Count}";
            return new AssetEntry(pUnit, pPath, pUnit.DisplayName, lSubtitle);
        }

        private static AssetEntry BuildEnemyAiEntry(EnemyAiProfileDefinition pProfile, string pPath)
        {
            string lSubtitle = $"{pProfile.TargetPriority} | {pProfile.MovementPolicy} | Distance {pProfile.PreferredDistance} | Threat {pProfile.ThreatRadius}";
            return new AssetEntry(pProfile, pPath, pProfile.name, lSubtitle);
        }

        private bool MatchesSearch(AssetEntry pEntry)
        {
            if (string.IsNullOrWhiteSpace(_Search))
                return true;

            string lSearch = _Search.Trim();
            return pEntry.Title.IndexOf(lSearch, StringComparison.OrdinalIgnoreCase) >= 0
                || pEntry.Subtitle.IndexOf(lSearch, StringComparison.OrdinalIgnoreCase) >= 0
                || pEntry.Path.IndexOf(lSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region _____________________________| ACTIONS

        private static void SelectAsset(UnityEngine.Object pAsset)
        {
            Selection.activeObject = pAsset;
        }

        private static void PingAsset(UnityEngine.Object pAsset)
        {
            EditorGUIUtility.PingObject(pAsset);
        }

        private string ResolveCreateButtonLabel()
        {
            switch (_CurrentTab)
            {
                case BrowserTab.Units:
                    return "Create Unit";

                case BrowserTab.EnemyAi:
                    return "Create Enemy AI";

                default:
                    return "Create Skill";
            }
        }

        private void OpenCreatorForCurrentTab()
        {
            switch (_CurrentTab)
            {
                case BrowserTab.Units:
                    Creator_Unit.Open();
                    break;

                case BrowserTab.EnemyAi:
                    Creator_EnemyAiProfile.Open();
                    break;

                default:
                    Creator_Skill.Open();
                    break;
            }
        }

        #endregion
    }
}
