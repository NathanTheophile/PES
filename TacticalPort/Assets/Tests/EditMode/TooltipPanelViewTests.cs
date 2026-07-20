using System.Linq;
using NUnit.Framework;
using TacticalPort.Data;
using TacticalPort.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class TooltipPanelViewTests
    {
        private const string PrefabPath = "Assets/Prefabs/UI/UI_Panel_Tooltip.prefab";

        [Test]
        public void SkillBindingOnlyGrowsContentVertically()
        {
            GameObject lPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(lPrefab, Is.Not.Null);

            GameObject lInstance = Object.Instantiate(lPrefab);
            SkillDefinition lSkill = ScriptableObject.CreateInstance<SkillDefinition>();

            try
            {
                SetSkillText(lSkill, "Short description.");
                TooltipPanelView lView = lInstance.GetComponent<TooltipPanelView>();
                RectTransform lLayout = (RectTransform)lInstance.transform.GetChild(0);
                RectTransform lTooltip = (RectTransform)lInstance.transform;
                RectTransform lName = (RectTransform)lLayout.Find("VBox_Name");
                RectTransform[] lFixedRows =
                {
                    (RectTransform)lLayout.Find("HBox1"),
                    (RectTransform)lLayout.Find("HBox2"),
                    (RectTransform)lLayout.Find("HBox3")
                };
                RectTransform lDescription = (RectTransform)lLayout.Find("VBox_Description");
                RectTransform lDescriptionBackground = (RectTransform)lDescription.Find("Img_Description");

                lView.Hide();
                lView.Bind(lSkill);
                lView.Show();
                lView.RefreshLayout();
                float lWidth = lLayout.rect.width;
                float lTooltipHeight = lTooltip.rect.height;
                float lShortContentHeight = lLayout.rect.height;
                float lNameHeight = lName.rect.height;
                float[] lFixedRowHeights = lFixedRows.Select(pRow => pRow.rect.height).ToArray();
                float lShortDescriptionHeight = lDescription.rect.height;
                float lShortDescriptionBackgroundHeight = lDescriptionBackground.rect.height;

                Assert.That(lInstance.GetComponentsInChildren<TMP_Text>(true).Any(pText => pText.text.Contains('{')), Is.False);
                Assert.That(lLayout.pivot.y, Is.EqualTo(1f).Within(0.01f));

                lView.Hide();
                lView.Show();
                Assert.That(lLayout.rect.height, Is.EqualTo(lShortContentHeight).Within(0.01f));

                SetSkillText(lSkill, string.Join(" ", Enumerable.Repeat("Long tooltip description used to verify fixed-width wrapping.", 20)));
                lView.Bind(lSkill);
                lView.RefreshLayout();

                Assert.That(lLayout.rect.width, Is.EqualTo(lWidth).Within(0.01f));
                Assert.That(lLayout.rect.height, Is.GreaterThan(lShortContentHeight));
                Assert.That(lTooltip.rect.height, Is.EqualTo(lTooltipHeight).Within(0.01f));
                Assert.That(lName.rect.height, Is.EqualTo(lNameHeight).Within(0.01f));
                Assert.That(lDescription.rect.height, Is.GreaterThan(lShortDescriptionHeight));
                Assert.That(lDescriptionBackground.rect.height, Is.GreaterThan(lShortDescriptionBackgroundHeight));
                for (int lIndex = 0; lIndex < lFixedRows.Length; lIndex++)
                    Assert.That(lFixedRows[lIndex].rect.height, Is.EqualTo(lFixedRowHeights[lIndex]).Within(0.01f));

                SetSkillText(lSkill, "Short description.");
                lView.Bind(lSkill);
                lView.RefreshLayout();
                Assert.That(lLayout.rect.height, Is.EqualTo(lShortContentHeight).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(lInstance);
                Object.DestroyImmediate(lSkill);
            }
        }

        private static void SetSkillText(SkillDefinition pSkill, string pDescription)
        {
            SerializedObject lSerializedSkill = new SerializedObject(pSkill);
            lSerializedSkill.FindProperty("_Id").stringValue = "tooltip_layout_test";
            lSerializedSkill.FindProperty("_DisplayName").stringValue = "Tooltip Test";
            lSerializedSkill.FindProperty("_Description").stringValue = pDescription;
            lSerializedSkill.FindProperty("_RangeMin").intValue = 1;
            lSerializedSkill.FindProperty("_RangeMax").intValue = 4;
            lSerializedSkill.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
