using System.IO;
using PES.Presentation.Configuration;
using UnityEditor;
using UnityEngine;

namespace PES.Presentation.Editor
{
    public sealed class ConfigurationAssetCreatorWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/_Project/Presentation/Configuration/Generated";

        private string _outputFolder = DefaultOutputFolder;
        private string _entityAssetName = "EntityArchetype_New";
        private string _skillAssetName = "SkillDefinition_New";

        [MenuItem("PES/Tools/Configuration Asset Creator")]
        private static void OpenWindow()
        {
            var window = GetWindow<ConfigurationAssetCreatorWindow>();
            window.titleContent = new GUIContent("PES Asset Creator");
            window.minSize = new Vector2(420f, 210f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create & Save Config Assets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Quick tool to create Entity Archetype and Skill Definition ScriptableObjects in the selected folder.",
                MessageType.Info);

            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _entityAssetName = EditorGUILayout.TextField("Entity Asset Name", _entityAssetName);
            _skillAssetName = EditorGUILayout.TextField("Skill Asset Name", _skillAssetName);

            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Entity Archetype", GUILayout.Height(32f)))
                {
                    CreateAndSaveAsset<EntityArchetypeAsset>(_entityAssetName);
                }

                if (GUILayout.Button("Create Skill Definition", GUILayout.Height(32f)))
                {
                    CreateAndSaveAsset<SkillDefinitionAsset>(_skillAssetName);
                }
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Create Both", GUILayout.Height(28f)))
            {
                CreateAndSaveAsset<EntityArchetypeAsset>(_entityAssetName);
                CreateAndSaveAsset<SkillDefinitionAsset>(_skillAssetName);
            }
        }

        private void CreateAndSaveAsset<TAsset>(string proposedName)
            where TAsset : ScriptableObject
        {
            if (string.IsNullOrWhiteSpace(proposedName))
            {
                Debug.LogWarning($"[{nameof(ConfigurationAssetCreatorWindow)}] Asset name is empty.");
                return;
            }

            if (!EnsureFolderExists(_outputFolder))
            {
                Debug.LogError($"[{nameof(ConfigurationAssetCreatorWindow)}] Invalid output folder: {_outputFolder}");
                return;
            }

            var asset = CreateInstance<TAsset>();
            var fileName = $"{SanitizeFileName(proposedName)}.asset";
            var relativePath = Path.Combine(_outputFolder, fileName).Replace("\\", "/");
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(relativePath);

            AssetDatabase.CreateAsset(asset, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;

            Debug.Log($"[{nameof(ConfigurationAssetCreatorWindow)}] Created {typeof(TAsset).Name} at {uniquePath}", asset);
        }

        private static bool EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !folderPath.StartsWith("Assets"))
            {
                return false;
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            var parts = folderPath.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }

            return AssetDatabase.IsValidFolder(folderPath);
        }

        private static string SanitizeFileName(string fileName)
        {
            var clean = fileName.Trim();

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                clean = clean.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(clean) ? "NewAsset" : clean;
        }
    }
}
