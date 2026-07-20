#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Central runtime access to localized presentation strings with English fallbacks.
#endregion

using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace TacticalPort.Data
{
    public static class GameLocalization
    {
        public const string CharactersTable = "Characters";
        public const string SkillsTable = "Skills";
        public const string PassivesTable = "Passives";
        public const string StatesTable = "States";
        public const string UiTable = "UI";
        public const string CombatLogTable = "CombatLog";

        public static bool TrySelectLocale(string pLocaleCode)
        {
            if (string.IsNullOrWhiteSpace(pLocaleCode) || !LocalizationSettings.HasSettings)
                return false;

            Locale lLocale = LocalizationSettings.AvailableLocales?.GetLocale(new LocaleIdentifier(pLocaleCode));
            if (lLocale == null)
                return false;

            LocalizationSettings.SelectedLocale = lLocale;
            return true;
        }

        public static string GetContentName(string pTable, string pId, string pEnglishFallback) =>
            Get(pTable, pId + ".name", pEnglishFallback);

        public static string GetContentDescription(string pTable, string pId, string pEnglishFallback) =>
            Get(pTable, pId + ".description", pEnglishFallback);

        public static string Get(string pTable, string pKey, string pEnglishFallback, params object[] pArguments)
        {
            if (string.IsNullOrWhiteSpace(pTable) || string.IsNullOrWhiteSpace(pKey) || !LocalizationSettings.HasSettings)
                return FormatFallback(pEnglishFallback, pArguments);

            try
            {
                LocalizedString lLocalizedString = new LocalizedString(pTable, pKey);
                string lLocalizedValue = pArguments != null && pArguments.Length > 0
                    ? lLocalizedString.GetLocalizedString(pArguments)
                    : lLocalizedString.GetLocalizedString();
                return string.IsNullOrWhiteSpace(lLocalizedValue)
                    ? FormatFallback(pEnglishFallback, pArguments)
                    : lLocalizedValue;
            }
            catch (Exception)
            {
                return FormatFallback(pEnglishFallback, pArguments);
            }
        }

        private static string FormatFallback(string pFallback, object[] pArguments)
        {
            string lFallback = pFallback ?? string.Empty;
            if (pArguments == null || pArguments.Length == 0)
                return lFallback;

            try
            {
                return string.Format(lFallback, pArguments);
            }
            catch (FormatException)
            {
                return lFallback;
            }
        }
    }
}
