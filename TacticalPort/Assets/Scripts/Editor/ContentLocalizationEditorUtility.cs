#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Keeps localization table entries in sync when gameplay content is created.
#endregion

using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace TacticalPort.EditorTools
{
    internal static class ContentLocalizationEditorUtility
    {
        public static void EnsureEntries(string pTable, string pId, string pEnglishName, string pEnglishDescription)
        {
            if (string.IsNullOrWhiteSpace(pId))
                return;

            StringTableCollection lCollection = LocalizationEditorSettings.GetStringTableCollection(pTable);
            if (lCollection == null)
            {
                Debug.LogWarning($"Localization table '{pTable}' is missing. Content will use its English fallback.");
                return;
            }

            string lNameKey = pId + ".name";
            string lDescriptionKey = pId + ".description";
            SetEntry(lCollection, "en", lNameKey, pEnglishName);
            SetEntry(lCollection, "en", lDescriptionKey, pEnglishDescription);
            EnsureEntry(lCollection, "fr", lNameKey);
            EnsureEntry(lCollection, "fr", lDescriptionKey);
            AssetDatabase.SaveAssets();
        }

        private static void SetEntry(StringTableCollection pCollection, string pLocale, string pKey, string pValue)
        {
            if (pCollection.GetTable(pLocale) is not StringTable lTable)
                return;

            StringTableEntry lEntry = lTable.GetEntry(pKey) ?? lTable.AddEntry(pKey, string.Empty);
            lEntry.Value = pValue ?? string.Empty;
            EditorUtility.SetDirty(lTable);
            EditorUtility.SetDirty(pCollection.SharedData);
        }

        private static void EnsureEntry(StringTableCollection pCollection, string pLocale, string pKey)
        {
            if (pCollection.GetTable(pLocale) is not StringTable lTable || lTable.GetEntry(pKey) != null)
                return;

            lTable.AddEntry(pKey, string.Empty);
            EditorUtility.SetDirty(lTable);
            EditorUtility.SetDirty(pCollection.SharedData);
        }
    }
}
