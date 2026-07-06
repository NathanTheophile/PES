#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.IO;
using GiantGrey.TileWorldCreator;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TacticalPort.EditorTools.Creator_FileSaver;

namespace TacticalPort.EditorTools
{
    public sealed class Creator_Map : EditorWindow
    {
        #region _____________________________/ CONSTANTS

        private const string TemplateScenePath = "Assets/Scenes/Maps/S_Map_Base.unity";
        private const string MapsFolder = "Assets/Scenes/Maps";
        private const string TileWorldCreatorConfigsFolder = "Assets/Data/Maps/TileWorldCreator";
        private const string MapPrefix = "S_Map_";

        #endregion

        #region _____________________________/ VALUES

        private string _MapName = "NewMap";
        private Configuration _TileWorldCreatorTemplateConfiguration;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Project/Create/Map...")]
        public static void Open()
        {
            Creator_Map lWindow = GetWindow<Creator_Map>("Create Map");
            lWindow.minSize = new Vector2(420f, 190f);
        }

        #endregion

        #region _____________________________| GUI

        private void OnGUI()
        {
            DrawTitle("Create Map", MapsFolder);
            EditorGUILayout.HelpBox($"TWC configs saved automatically to {TileWorldCreatorConfigsFolder}", MessageType.Info);

            _MapName = EditorGUILayout.TextField("Map Name", _MapName);
            _TileWorldCreatorTemplateConfiguration = (Configuration)EditorGUILayout.ObjectField(
                "TWC Config Template",
                _TileWorldCreatorTemplateConfiguration,
                typeof(Configuration),
                false);

            string lFileName = BuildSceneFileName(_MapName);
            string lScenePath = $"{MapsFolder}/{lFileName}.unity";
            string lConfigPath = $"{TileWorldCreatorConfigsFolder}/{lFileName}.asset";

            EditorGUILayout.LabelField("Scene", lFileName);
            EditorGUILayout.LabelField("TWC Config", lFileName);

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScenePath))
            {
                EditorGUILayout.HelpBox($"Template scene not found: {TemplateScenePath}", MessageType.Error);
                return;
            }

            bool lCanCreate = !string.IsNullOrWhiteSpace(GetMapSuffix(_MapName));
            if (!lCanCreate)
                EditorGUILayout.HelpBox("Map Name is required.", MessageType.Warning);
            else if (_TileWorldCreatorTemplateConfiguration == null)
            {
                lCanCreate = false;
                EditorGUILayout.HelpBox("TWC Config Template is required.", MessageType.Warning);
            }
            else if (File.Exists(lScenePath))
            {
                lCanCreate = false;
                EditorGUILayout.HelpBox($"Scene already exists: {lScenePath}", MessageType.Warning);
            }
            else if (File.Exists(lConfigPath))
            {
                lCanCreate = false;
                EditorGUILayout.HelpBox($"TWC config already exists: {lConfigPath}", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!lCanCreate))
            {
                if (DrawCreateButton("Create Map"))
                    CreateMap(lScenePath, lConfigPath, _TileWorldCreatorTemplateConfiguration);
            }
        }

        #endregion

        #region _____________________________| CREATE

        private static void CreateMap(string pScenePath, string pConfigPath, Configuration pTemplateConfiguration)
        {
            EnsureFolder(MapsFolder);
            EnsureFolder(TileWorldCreatorConfigsFolder);

            if (!AssetDatabase.CopyAsset(TemplateScenePath, pScenePath))
            {
                EditorUtility.DisplayDialog("Create Map", $"Failed to create scene at:\n{pScenePath}", "OK");
                return;
            }

            string lTemplateConfigPath = AssetDatabase.GetAssetPath(pTemplateConfiguration);
            if (string.IsNullOrEmpty(lTemplateConfigPath) || !AssetDatabase.CopyAsset(lTemplateConfigPath, pConfigPath))
            {
                AssetDatabase.DeleteAsset(pScenePath);
                EditorUtility.DisplayDialog("Create Map", $"Failed to create TWC config at:\n{pConfigPath}", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Configuration lMapConfiguration = AssetDatabase.LoadAssetAtPath<Configuration>(pConfigPath);
            AssignConfigurationToScene(pScenePath, lMapConfiguration);

            SceneAsset lScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(pScenePath);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = lScene;
            EditorGUIUtility.PingObject(lScene);
        }

        private static void AssignConfigurationToScene(string pScenePath, Configuration pConfiguration)
        {
            if (pConfiguration == null)
                return;

            Scene lScene = EditorSceneManager.OpenScene(pScenePath, OpenSceneMode.Additive);
            try
            {
                int lAssignedManagers = 0;
                GameObject[] lRootObjects = lScene.GetRootGameObjects();
                for (int lRootIndex = 0; lRootIndex < lRootObjects.Length; lRootIndex++)
                {
                    TileWorldCreatorManager[] lManagers = lRootObjects[lRootIndex].GetComponentsInChildren<TileWorldCreatorManager>(true);
                    for (int lManagerIndex = 0; lManagerIndex < lManagers.Length; lManagerIndex++)
                    {
                        TileWorldCreatorManager lManager = lManagers[lManagerIndex];
                        if (lManager == null)
                            continue;

                        lManager.configuration = pConfiguration;
                        EditorUtility.SetDirty(lManager);
                        lAssignedManagers++;
                    }
                }

                if (lAssignedManagers == 0)
                    Debug.LogWarning($"Created map scene '{pScenePath}', but no TileWorldCreatorManager was found to assign the duplicated configuration.");

                EditorSceneManager.MarkSceneDirty(lScene);
                EditorSceneManager.SaveScene(lScene);
            }
            finally
            {
                EditorSceneManager.CloseScene(lScene, true);
            }
        }

        private static string BuildSceneFileName(string pMapName) =>
            $"{MapPrefix}{GetMapSuffix(pMapName)}";

        private static string GetMapSuffix(string pMapName)
        {
            string lSuffix = string.IsNullOrWhiteSpace(pMapName) ? string.Empty : pMapName.Trim();
            if (lSuffix.StartsWith(MapPrefix))
                lSuffix = lSuffix.Substring(MapPrefix.Length);

            return SanitizeSceneName(lSuffix);
        }

        private static string SanitizeSceneName(string pValue)
        {
            if (string.IsNullOrWhiteSpace(pValue))
                return string.Empty;

            char[] lChars = pValue.Trim().Replace(' ', '_').ToCharArray();
            for (int lIndex = 0; lIndex < lChars.Length; lIndex++)
            {
                char lChar = lChars[lIndex];
                if ((lChar >= 'a' && lChar <= 'z') || (lChar >= 'A' && lChar <= 'Z') || (lChar >= '0' && lChar <= '9') || lChar == '_')
                    continue;

                lChars[lIndex] = '_';
            }

            return new string(lChars).Trim('_');
        }

        #endregion
    }
}
