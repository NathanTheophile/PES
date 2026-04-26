using System.IO;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.EditorTools
{
    internal static class Creator_FileSaver
    {
        #region _____________________________| CONSTANTS

        public const string SkillsFolder = "Assets/Data/Skills";
        public const string StatesFolder = "Assets/Data/States";
        public const string UnitsFolder = "Assets/Data/Units";

        #endregion

        #region _____________________________| HELPERS

        public static void EnsureFolder(string pFolderPath)
        {
            if (AssetDatabase.IsValidFolder(pFolderPath))
                return;

            string[] lParts = pFolderPath.Split('/');
            string lCurrentPath = lParts[0];

            for (int lIndex = 1; lIndex < lParts.Length; lIndex++)
            {
                string lNextPath = $"{lCurrentPath}/{lParts[lIndex]}";
                if (!AssetDatabase.IsValidFolder(lNextPath))
                    AssetDatabase.CreateFolder(lCurrentPath, lParts[lIndex]);

                lCurrentPath = lNextPath;
            }
        }

        public static string SanitizeId(string pValue, string pFallback)
        {
            string lValue = string.IsNullOrWhiteSpace(pValue) ? pFallback : pValue;
            lValue = lValue.Trim().ToLowerInvariant().Replace(" ", "_");

            char[] lChars = lValue.ToCharArray();
            for (int lIndex = 0; lIndex < lChars.Length; lIndex++)
            {
                char lChar = lChars[lIndex];
                if ((lChar >= 'a' && lChar <= 'z') || (lChar >= '0' && lChar <= '9') || lChar == '_')
                    continue;

                lChars[lIndex] = '_';
            }

            string lSanitized = new string(lChars).Trim('_');
            return string.IsNullOrWhiteSpace(lSanitized) ? pFallback : lSanitized;
        }

        public static T CreateAsset<T>(T pAsset, string pFolderPath, string pFileName) where T : ScriptableObject
        {
            EnsureFolder(pFolderPath);

            string lAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{pFolderPath}/{pFileName}.asset");
            AssetDatabase.CreateAsset(pAsset, lAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = pAsset;
            EditorGUIUtility.PingObject(pAsset);

            return pAsset;
        }

        public static SerializedObject CreateSerializedAsset<T>(out T pAsset) where T : ScriptableObject
        {
            pAsset = ScriptableObject.CreateInstance<T>();
            return new SerializedObject(pAsset);
        }

        public static void ApplyAndSave(SerializedObject pSerializedObject)
        {
            pSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            pSerializedObject.Update();
        }

        public static string BuildFileName(string pPrefix, string pId, string pDisplayName, string pFallback)
        {
            string lId = SanitizeId(pId, SanitizeId(pDisplayName, pFallback));
            return $"{pPrefix}_{lId}";
        }

        public static void DrawTitle(string pLabel, string pFolderPath)
        {
            EditorGUILayout.LabelField(pLabel, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Saved automatically to {pFolderPath}", MessageType.Info);
        }

        public static bool DrawCreateButton(string pLabel)
        {
            GUILayout.Space(8f);
            return GUILayout.Button(pLabel, GUILayout.Height(30f));
        }

        public static string GetAssetFileName(Object pAsset)
        {
            if (pAsset == null)
                return string.Empty;

            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(pAsset));
        }

        #endregion
    }
}
