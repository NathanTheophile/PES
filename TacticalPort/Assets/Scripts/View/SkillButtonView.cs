using System;
using TacticalPort.Data;
using TacticalPort.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalPort.View
{
    public sealed class SkillButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region _____________________________| VALUES

        [SerializeField] private Button _Button;
        [SerializeField] private GameObject _PanelTooltip;
        [SerializeField] private TMP_Text _TxtSkillName;
        [FormerlySerializedAs("_TxtEffectType")]
        [SerializeField] private TMP_Text _TxtAdditionalEffect;
        [SerializeField] private TMP_Text _TxtActionPointCostAndRange;
        [SerializeField] private TMP_Text _TxtPower;
        [SerializeField] private TMP_Text _TxtDescription;

        private SkillDefinition _Skill;
        private Action _OnClick;
        private bool _IsInteractable = true;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            ConfigureButton();
            Refresh();
            HideTooltip();
            ValidateReferences();
        }

        private void OnEnable() => HideTooltip();
        private void OnDisable() => HideTooltip();

        private void OnDestroy()
        {
            if (_Button != null)
                _Button.onClick.RemoveListener(OnButtonClicked);
        }

        private void OnValidate() => CacheMissingReferences();

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

            if (_Button != null)
                _Button.interactable = _Skill != null && _IsInteractable;
        }

        private string ResolveSkillNameText()
        {
            if (_Skill == null)
                return string.Empty;

            return $"{_Skill.DisplayName} ({_Skill.Id})";
        }

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

        private string ResolveActionPointCostAndRangeText()
        {
            if (_Skill == null)
                return string.Empty;

            return $"AP {_Skill.ActionPointCost} - Range {_Skill.Range}";
        }

        private string ResolvePowerText()
        {
            if (_Skill == null)
                return string.Empty;

            switch (_Skill.PrimaryEffectType)
            {
                case SkillPrimaryEffectType.Damage:
                    return $"Deals {_Skill.Power}";

                case SkillPrimaryEffectType.Heal:
                    return $"Heals {_Skill.Power}";

                default:
                    return string.Empty;
            }
        }

        private string ResolveDescriptionText()
        {
            if (_Skill == null)
                return string.Empty;

            return _Skill.Description;
        }

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

        private void CacheMissingReferences()
        {
            _Button ??= GetComponent<Button>();

            Transform lTooltipTransform = transform.Find("Panel_Tooltip");
            if (_PanelTooltip == null && lTooltipTransform != null)
                _PanelTooltip = lTooltipTransform.gameObject;

            if (_PanelTooltip == null)
                return;

            _TxtSkillName ??= FindTooltipLabel("Txt_SkillName");
            _TxtAdditionalEffect ??= FindTooltipLabel("Txt_AdditionalEffect") ?? FindTooltipLabel("Txt_EffectType");
            _TxtActionPointCostAndRange ??= FindTooltipLabel("Txt_ActionPointCostAndRange");
            _TxtPower ??= FindTooltipLabel("Txt_Power");
            _TxtDescription ??= FindTooltipLabel("Txt_Description");
        }

        private TMP_Text FindTooltipLabel(string pChildName)
        {
            Transform lChild = _PanelTooltip.transform.Find(pChildName);
            return lChild != null ? lChild.GetComponent<TMP_Text>() : null;
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
