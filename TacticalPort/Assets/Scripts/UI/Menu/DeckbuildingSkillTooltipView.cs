#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Menu UI
#endregion

using System.Text;
using TacticalPort.Data;
using TacticalPort.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    [DisallowMultipleComponent]
    public sealed class DeckbuildingSkillTooltipView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region _____________________________/ VALUES

        [SerializeField] private GameObject _TooltipPanel;
        [SerializeField] private RectTransform _LayoutRoot;
        [SerializeField] private TMP_Text _NameText;
        [SerializeField] private TMP_Text _MetaText;
        [SerializeField] private TMP_Text _PowerText;
        [SerializeField] private TMP_Text _AdditionalEffectText;
        [SerializeField] private TMP_Text _DescriptionText;
        [SerializeField, Min(12f)] private float _FontSize = 18f;

        private SkillDefinition _Skill;
        private PassiveDefinition _Passive;
        private RectTransform _TooltipRect;
        private Transform _OriginalTooltipParent;
        private int _OriginalTooltipSiblingIndex;
        private Vector2 _OriginalAnchorMin;
        private Vector2 _OriginalAnchorMax;
        private Vector2 _OriginalAnchoredPosition;
        private Vector2 _OriginalSizeDelta;
        private Vector2 _OriginalPivot;
        private Vector3 _OriginalLocalScale;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheTooltipTransform();
            ConfigureDynamicLayout();
            HideTooltip();
        }

        private void OnDisable() => SetTooltipActive(false);

        private void OnDestroy()
        {
            if (_TooltipPanel != null && _OriginalTooltipParent != null && _TooltipPanel.transform.parent != _OriginalTooltipParent)
                Destroy(_TooltipPanel);
        }

        #endregion

        #region _____________________________| API

        public void Bind(SkillDefinition pSkill)
        {
            _Skill = pSkill;
            _Passive = null;
            HideTooltip();
        }

        public void Bind(PassiveDefinition pPassive)
        {
            _Skill = null;
            _Passive = pPassive;
            HideTooltip();
        }

        public void PrepareForOwnerDisable() => HideTooltip();

        public void OnPointerEnter(PointerEventData pEventData)
        {
            if ((_Skill == null && _Passive == null) || _TooltipPanel == null)
                return;

            RefreshContent();

            MoveTooltipToOverlay();
            _TooltipPanel.SetActive(true);
            RefreshLayout();
        }

        public void OnPointerExit(PointerEventData pEventData) => HideTooltip();

        #endregion

        #region _____________________________| HELPERS

        private void HideTooltip()
        {
            SetTooltipActive(false);
            RestoreTooltipParent();
        }

        private void SetTooltipActive(bool pActive)
        {
            if (_TooltipPanel != null && _TooltipPanel.activeSelf != pActive)
                _TooltipPanel.SetActive(pActive);
        }

        private void CacheTooltipTransform()
        {
            if (_TooltipPanel == null || _TooltipRect != null)
                return;

            _TooltipRect = _TooltipPanel.transform as RectTransform;
            _OriginalTooltipParent = _TooltipPanel.transform.parent;
            _OriginalTooltipSiblingIndex = _TooltipPanel.transform.GetSiblingIndex();

            if (_TooltipRect == null)
                return;

            _OriginalAnchorMin = _TooltipRect.anchorMin;
            _OriginalAnchorMax = _TooltipRect.anchorMax;
            _OriginalAnchoredPosition = _TooltipRect.anchoredPosition;
            _OriginalSizeDelta = _TooltipRect.sizeDelta;
            _OriginalPivot = _TooltipRect.pivot;
            _OriginalLocalScale = _TooltipRect.localScale;
        }

        private void MoveTooltipToOverlay()
        {
            CacheTooltipTransform();

            Canvas lCanvas = GetComponentInParent<Canvas>();
            if (lCanvas == null || _TooltipPanel == null || _TooltipPanel.transform.parent == lCanvas.transform)
                return;

            _TooltipPanel.transform.SetParent(lCanvas.transform, true);
            _TooltipPanel.transform.SetAsLastSibling();
        }

        private void RestoreTooltipParent()
        {
            if (_TooltipPanel == null || _OriginalTooltipParent == null || _TooltipPanel.transform.parent == _OriginalTooltipParent)
                return;

            _TooltipPanel.transform.SetParent(_OriginalTooltipParent, false);
            _TooltipPanel.transform.SetSiblingIndex(Mathf.Min(_OriginalTooltipSiblingIndex, _OriginalTooltipParent.childCount - 1));

            if (_TooltipRect == null)
                return;

            _TooltipRect.anchorMin = _OriginalAnchorMin;
            _TooltipRect.anchorMax = _OriginalAnchorMax;
            _TooltipRect.anchoredPosition = _OriginalAnchoredPosition;
            _TooltipRect.sizeDelta = _OriginalSizeDelta;
            _TooltipRect.pivot = _OriginalPivot;
            _TooltipRect.localScale = _OriginalLocalScale;
        }

        private void RefreshLayout()
        {
            if (_LayoutRoot == null)
                return;

            ConfigureDynamicLayout();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_LayoutRoot);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_LayoutRoot);
        }

        private void ConfigureDynamicLayout()
        {
            if (_LayoutRoot == null)
                return;

            if (_LayoutRoot.TryGetComponent(out VerticalLayoutGroup lLayout))
            {
                lLayout.childControlWidth = true;
                lLayout.childControlHeight = true;
                lLayout.childForceExpandWidth = false;
                lLayout.childForceExpandHeight = false;
            }

            if (_LayoutRoot.TryGetComponent(out ContentSizeFitter lFitter))
            {
                lFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                lFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void RefreshContent()
        {
            bool lHasNativeLayout = _NameText != null || _MetaText != null || _PowerText != null || _AdditionalEffectText != null;
            if (!lHasNativeLayout)
            {
                if (_DescriptionText != null)
                {
                    _DescriptionText.enableAutoSizing = false;
                    _DescriptionText.fontSize = _FontSize;
                    _DescriptionText.textWrappingMode = TextWrappingModes.Normal;
                    _DescriptionText.text = _Skill != null ? BuildTooltipText(_Skill) : BuildTooltipText(_Passive);
                }
                return;
            }

            if (_Skill != null)
            {
                SetLine(_NameText, _Skill.DisplayName);
                SetLine(_MetaText, $"Energy {_Skill.EnergyCost} - Range {_Skill.Range}");
                SetLine(_PowerText, ResolvePowerText(_Skill));
                SetLine(_AdditionalEffectText, ResolveAdditionalEffectText(_Skill));
                SetLine(_DescriptionText, BuildSkillDetails(_Skill), false);
                return;
            }

            SetLine(_NameText, _Passive != null ? _Passive.DisplayName : string.Empty);
            SetLine(_MetaText, string.Empty);
            SetLine(_PowerText, string.Empty);
            SetLine(_AdditionalEffectText, string.Empty);
            SetLine(_DescriptionText, _Passive != null ? _Passive.Description : string.Empty, false);
        }

        private static string ResolvePowerText(SkillDefinition pSkill) => pSkill.PrimaryEffectType switch
        {
            SkillPrimaryEffectType.Damage => BuildDamagePowerText(pSkill),
            SkillPrimaryEffectType.Heal => $"Heals {pSkill.Power}",
            _ => string.Empty
        };

        private static string BuildDamagePowerText(SkillDefinition pSkill)
        {
            string lDamage = pSkill.UseAoeDamageFalloff
                ? $"Deals {pSkill.Power} (-{pSkill.AoeDamageFalloffPercentPerCell}%/cell)"
                : $"Deals {pSkill.Power}";
            return pSkill.HasLifeSteal ? $"{lDamage} | Life Steal 50%" : lDamage;
        }

        private static string ResolveAdditionalEffectText(SkillDefinition pSkill) => pSkill.AdditionalEffectType switch
        {
            SkillAdditionalEffectType.Push => $"Push {pSkill.PushDistance}",
            SkillAdditionalEffectType.Teleport => "Teleport",
            SkillAdditionalEffectType.SwitchPositions => "Switch Position",
            SkillAdditionalEffectType.Summon => pSkill.SummonUnit != null ? $"Summon {pSkill.SummonUnit.DisplayName}" : "Summon",
            SkillAdditionalEffectType.CreateGlyph => $"Create Glyph ({pSkill.GlyphDurationTurns}t)",
            _ => string.Empty
        };

        private static string BuildSkillDetails(SkillDefinition pSkill)
        {
            StringBuilder lBuilder = new StringBuilder(192);
            if (!string.IsNullOrWhiteSpace(pSkill.Description))
                AppendLine(lBuilder, pSkill.Description);
            if (pSkill.RequiresLineOfSight)
                AppendLine(lBuilder, "Ligne de vue requise");
            if (pSkill.HasLifeSteal)
                AppendLine(lBuilder, "Life Steal: 50% of enemy Health removed");
            AppendAreaOfEffect(lBuilder, pSkill);
            AppendUsageLimits(lBuilder, pSkill);
            return lBuilder.ToString().TrimEnd();
        }

        private static void SetLine(TMP_Text pText, string pValue, bool pHideWhenEmpty = true)
        {
            if (pText == null)
                return;

            pText.text = pValue ?? string.Empty;
            if (pHideWhenEmpty)
                pText.gameObject.SetActive(!string.IsNullOrWhiteSpace(pValue));
        }

        private static string BuildTooltipText(SkillDefinition pSkill)
        {
            StringBuilder lBuilder = new StringBuilder(256);

            AppendLine(lBuilder, pSkill.DisplayName);
            AppendLine(lBuilder, $"Energy {pSkill.EnergyCost} | Range {pSkill.RangeMin}-{pSkill.RangeMax}");

            if (pSkill.RequiresLineOfSight)
                AppendLine(lBuilder, "Ligne de vue requise");

            AppendPrimaryEffect(lBuilder, pSkill);
            AppendAdditionalEffect(lBuilder, pSkill);
            AppendAreaOfEffect(lBuilder, pSkill);
            AppendUsageLimits(lBuilder, pSkill);

            if (!string.IsNullOrWhiteSpace(pSkill.Description))
            {
                AppendLine(lBuilder, string.Empty);
                AppendLine(lBuilder, pSkill.Description);
            }

            return lBuilder.ToString().TrimEnd();
        }

        private static string BuildTooltipText(PassiveDefinition pPassive)
        {
            if (pPassive == null)
                return string.Empty;

            return string.IsNullOrWhiteSpace(pPassive.Description)
                ? pPassive.DisplayName
                : $"{pPassive.DisplayName}\n\n{pPassive.Description}";
        }

        private static void AppendPrimaryEffect(StringBuilder pBuilder, SkillDefinition pSkill)
        {
            switch (pSkill.PrimaryEffectType)
            {
                case SkillPrimaryEffectType.Damage:
                    AppendLine(pBuilder, $"Damage: {pSkill.Power}");
                    if (pSkill.HasLifeSteal)
                        AppendLine(pBuilder, "Life Steal: 50% of enemy Health removed");
                    break;

                case SkillPrimaryEffectType.Heal:
                    AppendLine(pBuilder, $"Healing: {pSkill.Power}");
                    break;
            }
        }

        private static void AppendAdditionalEffect(StringBuilder pBuilder, SkillDefinition pSkill)
        {
            switch (pSkill.AdditionalEffectType)
            {
                case SkillAdditionalEffectType.Push:
                    AppendLine(pBuilder, $"Push: {pSkill.PushDistance}");
                    break;

                case SkillAdditionalEffectType.Teleport:
                    AppendLine(pBuilder, "Effect: Teleportation");
                    break;

                case SkillAdditionalEffectType.SwitchPositions:
                    AppendLine(pBuilder, "Effect: Position swap");
                    break;

                case SkillAdditionalEffectType.Summon:
                    AppendLine(pBuilder, pSkill.SummonUnit != null ? $"Summon: {pSkill.SummonUnit.DisplayName}" : "Effect: Summon");
                    break;

                case SkillAdditionalEffectType.CreateGlyph:
                    AppendLine(pBuilder, $"Glyph: {pSkill.GlyphDurationTurns} turn(s)");
                    break;
            }
        }

        private static void AppendAreaOfEffect(StringBuilder pBuilder, SkillDefinition pSkill)
        {
            if (pSkill.AoeShape == SkillAoeShape.Single)
                return;

            AppendLine(pBuilder, $"AoE : {pSkill.AoeShape} {pSkill.AoeSize}");
            if (pSkill.UseAoeDamageFalloff)
                AppendLine(pBuilder, $"AoE falloff: -{pSkill.AoeDamageFalloffPercentPerCell}% / cell");
        }

        private static void AppendUsageLimits(StringBuilder pBuilder, SkillDefinition pSkill)
        {
            if (pSkill.CooldownTurns > 0)
                AppendLine(pBuilder, $"Cooldown: {pSkill.CooldownTurns} turn(s)");
            if (pSkill.UsePerTurn > 0)
                AppendLine(pBuilder, $"Limit: {pSkill.UsePerTurn} / turn");
            if (pSkill.UsePerTarget > 0)
                AppendLine(pBuilder, $"Target limit: {pSkill.UsePerTarget} / target");
        }

        private static void AppendLine(StringBuilder pBuilder, string pText) => pBuilder.AppendLine(pText ?? string.Empty);

        #endregion
    }
}
