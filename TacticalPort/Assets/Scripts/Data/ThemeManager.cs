#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  UI
#endregion

using UnityEngine;

namespace TacticalPort.Data
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class ThemeManager : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private ThemeDefinition _CurrentTheme;
        [SerializeField] private bool _KeepAliveAcrossScenes = true;

        private static ThemeManager _Instance;

        #endregion

        #region _____________________________/ ACCESSORS

        public static ThemeManager Instance => _Instance;
        public ThemeDefinition CurrentTheme => _CurrentTheme;

        public static Color MainColor => _Instance != null ? _Instance.GetMainColor() : ThemeDefinition.DefaultMainColor;
        public static Color SecondColor => _Instance != null ? _Instance.GetSecondColor() : ThemeDefinition.DefaultSecondColor;
        public static Color AccentColor => _Instance != null ? _Instance.GetAccentColor() : ThemeDefinition.DefaultAccentColor;
        public static Color GoldColor => _Instance != null ? _Instance.GetGoldColor() : ThemeDefinition.DefaultGoldColor;
        public static Color BlackColor => _Instance != null ? _Instance.GetBlackColor() : ThemeDefinition.DefaultBlackColor;
        public static Color SilverColor => _Instance != null ? _Instance.GetSilverColor() : ThemeDefinition.DefaultSilverColor;
        public static Color DamageColor => _Instance != null ? _Instance.GetDamageColor() : ThemeDefinition.DefaultDamageColor;
        public static Color UtilityColor => _Instance != null ? _Instance.GetUtilityColor() : ThemeDefinition.DefaultUtilityColor;
        public static Color SummonColor => _Instance != null ? _Instance.GetSummonColor() : ThemeDefinition.DefaultSummonColor;
        public static Color HealColor => _Instance != null ? _Instance.GetHealColor() : ThemeDefinition.DefaultHealColor;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            if (_Instance != null && _Instance != this)
            {
                Destroy(this);
                return;
            }

            _Instance = this;

            if (_KeepAliveAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_Instance == this)
                _Instance = null;
        }

        #endregion

        #region _____________________________| API

        public void SetCurrentTheme(ThemeDefinition pTheme) => _CurrentTheme = pTheme;

        public Color GetMainColor() => _CurrentTheme != null ? _CurrentTheme.MainColor : ThemeDefinition.DefaultMainColor;
        public Color GetSecondColor() => _CurrentTheme != null ? _CurrentTheme.SecondColor : ThemeDefinition.DefaultSecondColor;
        public Color GetAccentColor() => _CurrentTheme != null ? _CurrentTheme.AccentColor : ThemeDefinition.DefaultAccentColor;
        public Color GetGoldColor() => _CurrentTheme != null ? _CurrentTheme.GoldColor : ThemeDefinition.DefaultGoldColor;
        public Color GetBlackColor() => _CurrentTheme != null ? _CurrentTheme.BlackColor : ThemeDefinition.DefaultBlackColor;
        public Color GetSilverColor() => _CurrentTheme != null ? _CurrentTheme.SilverColor : ThemeDefinition.DefaultSilverColor;
        public Color GetDamageColor() => _CurrentTheme != null ? _CurrentTheme.DamageColor : ThemeDefinition.DefaultDamageColor;
        public Color GetUtilityColor() => _CurrentTheme != null ? _CurrentTheme.UtilityColor : ThemeDefinition.DefaultUtilityColor;
        public Color GetSummonColor() => _CurrentTheme != null ? _CurrentTheme.SummonColor : ThemeDefinition.DefaultSummonColor;
        public Color GetHealColor() => _CurrentTheme != null ? _CurrentTheme.HealColor : ThemeDefinition.DefaultHealColor;

        public static Color GetSkillColor(SkillDefinition pSkill) =>
            _Instance != null
                ? _Instance.ResolveSkillColor(pSkill)
                : ThemeDefinition.ResolveDefaultSkillColor(pSkill);

        public Color ResolveSkillColor(SkillDefinition pSkill) =>
            _CurrentTheme != null
                ? _CurrentTheme.ResolveSkillColor(pSkill)
                : ThemeDefinition.ResolveDefaultSkillColor(pSkill);

        #endregion
    }
}
