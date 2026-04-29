#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using TacticalPort.Data;
using TacticalPort.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class SkillButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region _____________________________/ VALUES

        [SerializeField] private Button _Button;
        [SerializeField] private GameObject _PanelTooltip;
        [SerializeField] private TMP_Text _TxtSkillName;
        [FormerlySerializedAs("_TxtEffectType")]
        [SerializeField] private TMP_Text _TxtAdditionalEffect;
        [SerializeField] private TMP_Text _TxtActionPointCostAndRange;
        [SerializeField] private TMP_Text _TxtPower;
        [SerializeField] private TMP_Text _TxtDescription;
        [SerializeField] private CanvasGroup _CanvasGroup;

        private SkillDefinition _Skill;
        private Action _OnClick;
        private bool _IsInteractable = true;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ValidateReferences();
            ConfigureButton();
            Refresh();
            HideTooltip();
        }

        private void OnEnable() => HideTooltip();
        private void OnDisable() => HideTooltip();

        private void OnDestroy()
        {
            if (_Button != null)
                _Button.onClick.RemoveListener(OnButtonClicked);
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(SkillDefinition pSkill, Action pOnClick)
        {
            _Skill = pSkill;
            _OnClick = pOnClick;

            Refresh();
            HideTooltip();
        }

        public void SetInteractable(bool pValue)
        {
            _IsInteractable = pValue;
            Refresh();
        }

        #endregion

        #region _____________________________| POINTER

        public void OnPointerEnter(PointerEventData pEventData)
        {
            if (_Skill == null)
                return;

            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData pEventData) => HideTooltip();

        #endregion

        #region _____________________________| DISPLAY

        private void Refresh()
        {
            SetLabel(_TxtSkillName, ResolveSkillNameText());
            SetTooltipLine(_TxtAdditionalEffect, ResolveAdditionalEffectText());
            SetLabel(_TxtActionPointCostAndRange, ResolveActionPointCostAndRangeText());
            SetTooltipLine(_TxtPower, ResolvePowerText());
            SetLabel(_TxtDescription, ResolveDescriptionText());
            RefreshTooltipLayout();

            bool lIsButtonInteractable = _Skill != null && _IsInteractable;
            if (_Button != null)
                _Button.interactable = lIsButtonInteractable;

            if (_CanvasGroup != null)
            {
                _CanvasGroup.alpha = lIsButtonInteractable ? 1f : 0.7f;
                _CanvasGroup.interactable = lIsButtonInteractable;
                _CanvasGroup.blocksRaycasts = lIsButtonInteractable;
            }
        }

        private string ResolveSkillNameText() => _Skill != null ? $"{_Skill.DisplayName} ({_Skill.Id})" : string.Empty;

        private string ResolveAdditionalEffectText()
        {
            if (_Skill == null)
                return string.Empty;

            return _Skill.AdditionalEffectType switch
            {
                SkillAdditionalEffectType.Push => $"Push {_Skill.PushDistance}",
                SkillAdditionalEffectType.Teleport => "Teleport",
                SkillAdditionalEffectType.SwitchPositions => "Switch Position",
                SkillAdditionalEffectType.Summon => _Skill.SummonUnit != null ? $"Summon {_Skill.SummonUnit.DisplayName}" : "Summon",
                SkillAdditionalEffectType.CreateGlyph => $"Create Glyph ({_Skill.GlyphDurationTurns}t)",
                _ => string.Empty
            };
        }

        private string ResolveActionPointCostAndRangeText() => _Skill != null ? $"AP {_Skill.ActionPointCost} - Range {_Skill.Range}" : string.Empty;

        private string ResolvePowerText()
        {
            if (_Skill == null)
                return string.Empty;

            switch (_Skill.PrimaryEffectType)
            {
                case SkillPrimaryEffectType.Damage:
                    return _Skill.AoeDamageFalloffPercentPerCell > 0
                        ? $"Deals {_Skill.Power} (-{_Skill.AoeDamageFalloffPercentPerCell}%/cell)"
                        : $"Deals {_Skill.Power}";

                case SkillPrimaryEffectType.Heal:
                    return $"Heals {_Skill.Power}";

                default:
                    return string.Empty;
            }
        }

        private string ResolveDescriptionText() => _Skill != null ? _Skill.Description : string.Empty;

        private void ShowTooltip()
        {
            if (_PanelTooltip == null)
                return;

            _PanelTooltip.transform.SetAsLastSibling();
            _PanelTooltip.SetActive(true);
            RefreshTooltipLayout();
        }

        private void HideTooltip()
        {
            if (_PanelTooltip != null)
                _PanelTooltip.SetActive(false);
        }

        #endregion

        #region _____________________________| HELPERS

        private void ConfigureButton()
        {
            if (_Button == null)
                return;

            _Button.onClick.RemoveListener(OnButtonClicked);
            _Button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked() => _OnClick?.Invoke();

        private void RefreshTooltipLayout()
        {
            RectTransform lTooltipRect = _PanelTooltip != null ? _PanelTooltip.transform as RectTransform : null;
            if (lTooltipRect == null || !lTooltipRect.gameObject.activeInHierarchy)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(lTooltipRect);
        }

        private bool ValidateReferences()
        {
            bool lIsValid = true;

            lIsValid &= ValidateReference(_Button, nameof(_Button));
            lIsValid &= ValidateReference(_PanelTooltip, nameof(_PanelTooltip));
            lIsValid &= ValidateReference(_TxtSkillName, nameof(_TxtSkillName));
            lIsValid &= ValidateReference(_TxtAdditionalEffect, nameof(_TxtAdditionalEffect));
            lIsValid &= ValidateReference(_TxtActionPointCostAndRange, nameof(_TxtActionPointCostAndRange));
            lIsValid &= ValidateReference(_TxtPower, nameof(_TxtPower));
            lIsValid &= ValidateReference(_TxtDescription, nameof(_TxtDescription));
            lIsValid &= ValidateReference(_CanvasGroup, nameof(_CanvasGroup));

            return lIsValid;
        }

        private bool ValidateReference(UnityEngine.Object pReference, string pReferenceName)
        {
            if (pReference != null)
                return true;

            Debug.LogError($"[{nameof(SkillButtonView)}] Missing prefab reference '{pReferenceName}' on '{name}'.", this);
            return false;
        }

        private static void SetLabel(TMP_Text pLabel, string pValue)
        {
            if (pLabel != null)
                pLabel.text = pValue ?? string.Empty;
        }

        private static void SetTooltipLine(TMP_Text pLabel, string pValue)
        {
            if (pLabel == null)
                return;

            pLabel.text = pValue ?? string.Empty;
            pLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(pValue));
        }

        #endregion
    }
}
