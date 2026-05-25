#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "ThemeDefinition", menuName = "Project/Data/Theme Definition")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        public static readonly Color DefaultMainColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        public static readonly Color DefaultSecondColor = new Color(0.16f, 0.18f, 0.22f, 1f);
        public static readonly Color DefaultAccentColor = new Color(0.35f, 0.55f, 1f, 1f);
        public static readonly Color DefaultDamageColor = new Color(0.9f, 0.22f, 0.18f, 1f);
        public static readonly Color DefaultUtilityColor = new Color(0.35f, 0.55f, 1f, 1f);
        public static readonly Color DefaultHealColor = new Color(0.2f, 0.78f, 0.38f, 1f);

        #region _____________________________/ VALUES

        [Header("Base UI")]
        [SerializeField] private Color _MainColor = DefaultMainColor;
        [SerializeField] private Color _SecondColor = DefaultSecondColor;
        [SerializeField] private Color _AccentColor = DefaultAccentColor;

        [Header("Skills")]
        [SerializeField] private Color _DamageColor = DefaultDamageColor;
        [SerializeField] private Color _UtilityColor = DefaultUtilityColor;
        [SerializeField] private Color _HealColor = DefaultHealColor;

        #endregion

        #region _____________________________/ ACCESSORS

        public Color MainColor => _MainColor;
        public Color SecondColor => _SecondColor;
        public Color AccentColor => _AccentColor;
        public Color DamageColor => _DamageColor;
        public Color UtilityColor => _UtilityColor;
        public Color HealColor => _HealColor;

        #endregion

        #region _____________________________| SKILLS

        public Color ResolveSkillColor(SkillDefinition pSkill) =>
            pSkill != null ? ResolveSkillColor(pSkill.PrimaryEffectType) : UtilityColor;

        public Color ResolveSkillColor(SkillPrimaryEffectType pEffectType) =>
            pEffectType switch
            {
                SkillPrimaryEffectType.Damage => DamageColor,
                SkillPrimaryEffectType.Heal => HealColor,
                _ => UtilityColor
            };

        public static Color ResolveDefaultSkillColor(SkillDefinition pSkill) =>
            pSkill != null ? ResolveDefaultSkillColor(pSkill.PrimaryEffectType) : DefaultUtilityColor;

        public static Color ResolveDefaultSkillColor(SkillPrimaryEffectType pEffectType) =>
            pEffectType switch
            {
                SkillPrimaryEffectType.Damage => DefaultDamageColor,
                SkillPrimaryEffectType.Heal => DefaultHealColor,
                _ => DefaultUtilityColor
            };

        #endregion
    }
}
