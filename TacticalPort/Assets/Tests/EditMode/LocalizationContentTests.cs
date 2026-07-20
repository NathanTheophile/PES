using NUnit.Framework;
using TacticalPort.Data;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace TacticalPort.State.Tests
{
    public sealed class LocalizationContentTests
    {
        private const string Root = "Assets/Localization";

        [Test]
        public void ProjectDefinesEnglishFrenchAndAllExpectedStringTables()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<LocalizationSettings>($"{Root}/LocalizationSettings.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Locale>($"{Root}/Locales/English.asset")?.Identifier.Code, Is.EqualTo("en"));
            Assert.That(AssetDatabase.LoadAssetAtPath<Locale>($"{Root}/Locales/French.asset")?.Identifier.Code, Is.EqualTo("fr"));

            string[] lTables =
            {
                GameLocalization.CharactersTable,
                GameLocalization.SkillsTable,
                GameLocalization.PassivesTable,
                GameLocalization.StatesTable,
                GameLocalization.UiTable,
                GameLocalization.CombatLogTable
            };

            for (int lIndex = 0; lIndex < lTables.Length; lIndex++)
            {
                string lTable = lTables[lIndex];
                Assert.That(LoadTable(lTable, "en"), Is.Not.Null, $"Missing English table '{lTable}'.");
                Assert.That(LoadTable(lTable, "fr"), Is.Not.Null, $"Missing French table '{lTable}'.");
            }
        }

        [Test]
        public void FreebooterUsesEnglishTechnicalDataAndFrenchLocalizedEntries()
        {
            UnitDefinition lUnit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(
                "Assets/AITests/Freebooter/Data/Units/Freebooter.asset");

            Assert.That(lUnit, Is.Not.Null);
            Assert.That(lUnit.Id, Is.EqualTo("freebooter"));
            Assert.That(lUnit.EnglishDisplayName, Is.EqualTo("Freebooter"));
            Assert.That(LoadTable(GameLocalization.CharactersTable, "en").GetEntry("freebooter.name")?.Value, Is.EqualTo("Freebooter"));
            Assert.That(LoadTable(GameLocalization.CharactersTable, "fr").GetEntry("freebooter.name")?.Value, Is.EqualTo("Forban"));
        }

        [Test]
        public void LegacyPersistedIdentifiersResolveToCanonicalEnglishIdentifiers()
        {
            Assert.That(ContentIdAliases.Matches("freebooter", "forban"), Is.True);
            Assert.That(ContentIdAliases.Matches("boarding_harpoon", "harpon_abordage"), Is.True);
            Assert.That(ContentIdAliases.Matches("carpenter", "Carpenter"), Is.True);
            Assert.That(ContentIdAliases.Matches("solidarity_tide", "maree_solidaire"), Is.True);
        }

        private static StringTable LoadTable(string pCollection, string pLocale) =>
            AssetDatabase.LoadAssetAtPath<StringTable>($"{Root}/Tables/{pCollection}_{pLocale}.asset");
    }
}
