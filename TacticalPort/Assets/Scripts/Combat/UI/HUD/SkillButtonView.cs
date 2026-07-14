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
        [SerializeField] private TooltipPanelView _TooltipPanel;
        [SerializeField] private TMP_Text _TxtSkillName;
        [FormerlySerializedAs("_TxtEffectType")]
        [SerializeField] private TMP_Text _TxtAdditionalEffect;
        [SerializeField] private TMP_Text _TxtEnergyCostAndRange;
        [SerializeField] private TMP_Text _TxtPower;
        [SerializeField] private TMP_Text _TxtDescription;
        [SerializeField] private CanvasGroup _CanvasGroup;
        [Header("Visual Type Feedback")]
        [SerializeField] private Image _SkillBackground;
        [SerializeField] private Image _SkillCircle;
        [SerializeField] private Image _SkillIcon;
        [SerializeField] private Image _SkillHighlight;
        [SerializeField] private Image _SkillOrnament;
        [SerializeField] private Image _SkillOrnamentSecondary;
        [Header("Tooltip Visual Feedback")]
        [SerializeField] private Image _TooltipOrnament;
        [SerializeField] private Image _TooltipOrnamentSecondary;
        [SerializeField] private UIInteractionAnimator _InteractionAnimator;

        private SkillDefinition _Skill;
        private Action _OnClick;
        private bool _IsInteractable = true;
        private bool _IsSelected;
        private bool _HasResolvedVisualReferences;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ResolveRuntimeReferences();
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

            SetSelected(false);
            Refresh();
            HideTooltip();
        }

        public void SetInteractable(bool pValue)
        {
            _IsInteractable = pValue;
            Refresh();
        }

        public void SetSelected(bool pValue)
        {
            if (_IsSelected == pValue)
                return;

            _IsSelected = pValue;
            ResolveInteractionAnimator();
            _InteractionAnimator?.SetSelected(_IsSelected);
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
            SetLabel(_TxtEnergyCostAndRange, ResolveEnergyCostAndRangeText());
            SetTooltipLine(_TxtPower, ResolvePowerText());
            SetDescription(ResolveDescriptionText());
            ApplySkillVisuals();

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

        private string ResolveEnergyCostAndRangeText() => _Skill != null ? $"Energy {_Skill.EnergyCost} - Range {_Skill.Range}" : string.Empty;

        private string ResolvePowerText()
        {
            if (_Skill == null)
                return string.Empty;

            switch (_Skill.PrimaryEffectType)
            {
                case SkillPrimaryEffectType.Damage:
                    string lDamage = _Skill.UseAoeDamageFalloff
                        ? $"Deals {_Skill.Power} (-{_Skill.AoeDamageFalloffPercentPerCell}%/cell)"
                        : $"Deals {_Skill.Power}";
                    return _Skill.HasLifeSteal ? $"{lDamage} | Life Steal 50%" : lDamage;

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

            return _Skill.HasLifeSteal
                ? $"{_Skill.Description}\nLife Steal: 50% of enemy health removed".Trim()
                : _Skill.Description;
        }

        private void ShowTooltip()
        {
            if (_TooltipPanel == null)
                return;

            _TooltipPanel.transform.SetAsLastSibling();
            _TooltipPanel.Show();
        }

        private void HideTooltip()
        {
            if (_TooltipPanel != null)
                _TooltipPanel.Hide();
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

        private void ApplySkillVisuals()
        {
            ResolveVisualReferences();

            if (_SkillIcon != null)
                _SkillIcon.sprite = _Skill != null ? _Skill.Icon : null;

            Color lColor = ResolveSkillColor(_Skill);
            ApplyColor(_SkillBackground, lColor);
            ApplyColor(_SkillCircle, lColor);
            ApplyColor(_SkillIcon, ResolveIconColor(lColor));
            ApplyColor(_SkillHighlight, lColor);
            ApplyColor(_SkillOrnament, lColor);
            ApplyColor(_SkillOrnamentSecondary, lColor);
            ApplyColor(_TooltipOrnament, lColor);
            ApplyColor(_TooltipOrnamentSecondary, lColor);
        }

        private void ResolveVisualReferences()
        {
            if (_HasResolvedVisualReferences)
                return;

            _SkillBackground ??= FindChildImage("Btn_Skill_Background");
            _SkillCircle ??= FindChildImage("Btn_Skill_Circle");
            _SkillIcon ??= FindChildImage("Btn_Skill_Icon");
            _SkillHighlight ??= FindChildImage("Btn_Skill_Highlight");
            _SkillOrnament ??= FindChildImage("Btn_Skill_Ornament_L") ?? FindChildImage("Btn_Skill_Ornament") ?? FindChildImage("Btn_Skill_Ornaments");
            _SkillOrnamentSecondary ??= FindChildImage("Btn_Skill_Ornament_R");
            _TooltipOrnament ??= FindChildImage("Img_LeftCorner") ?? FindChildImage("Tooltip_Ornament_L");
            _TooltipOrnamentSecondary ??= FindChildImage("Img_RightCorner") ?? FindChildImage("Tooltip_Ornament_R");
            _HasResolvedVisualReferences = true;
        }

        private void ResolveInteractionAnimator()
        {
            if (_InteractionAnimator == null)
                _InteractionAnimator = GetComponent<UIInteractionAnimator>();
        }

        private void ResolveRuntimeReferences()
        {
            _TooltipPanel ??= GetComponentInChildren<TooltipPanelView>(true);
        }

        private void SetDescription(string pValue)
        {
            if (_TxtDescription != null)
            {
                SetLabel(_TxtDescription, pValue);
                return;
            }

            _TooltipPanel?.SetDescription(pValue);
        }

        private Color ResolveSkillColor(SkillDefinition pSkill) =>
            ThemeManager.GetSkillColor(pSkill);

        private static Color ResolveIconColor(Color pBaseColor)
        {
            Color.RGBToHSV(pBaseColor, out float lHue, out float lSaturation, out float lValue);
            Color lIconColor = Color.HSVToRGB(lHue, lSaturation, Mathf.Clamp01(lValue + 0.1f));
            lIconColor.a = pBaseColor.a;
            return lIconColor;
        }

        private Image FindChildImage(string pChildName)
        {
            if (string.IsNullOrWhiteSpace(pChildName))
                return null;

            Image[] lImages = GetComponentsInChildren<Image>(true);
            for (int lIndex = 0; lIndex < lImages.Length; lIndex++)
            {
                Image lImage = lImages[lIndex];
                if (lImage != null && lImage.name == pChildName)
                    return lImage;
            }

            return null;
        }

        private static void ApplyColor(Image pImage, Color pColor)
        {
            if (pImage != null)
                pImage.color = pColor;
        }

        private bool ValidateReferences()
        {
            bool lIsValid = true;

            lIsValid &= ValidateReference(_Button, nameof(_Button));
            lIsValid &= ValidateReference(_TooltipPanel, nameof(_TooltipPanel));
            lIsValid &= ValidateReference(_TxtSkillName, nameof(_TxtSkillName));
            lIsValid &= ValidateReference(_TxtAdditionalEffect, nameof(_TxtAdditionalEffect));
            lIsValid &= ValidateReference(_TxtEnergyCostAndRange, nameof(_TxtEnergyCostAndRange));
            lIsValid &= ValidateReference(_TxtPower, nameof(_TxtPower));
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
