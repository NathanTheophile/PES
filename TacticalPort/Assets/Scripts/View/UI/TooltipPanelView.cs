#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Shared tooltip presentation for combat and menu UI.
#endregion

using System.Text;
using TacticalPort.Data;
using TacticalPort.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("TacticalPort/UI/Tooltip Panel View")]
    public sealed class TooltipPanelView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Header("Layout")]
        [FormerlySerializedAs("_LayoutRoot")]
        [SerializeField] private RectTransform _ContentRoot;
        [SerializeField] private RectTransform _DetailsRoot;
        [SerializeField] private RectTransform _DetailsBackground;
        [SerializeField] private RectTransform _DescriptionRoot;
        [SerializeField] private RectTransform _DescriptionBackground;
        [SerializeField] private GameObject[] _SkillOnlySections;
        [SerializeField, Min(1f)] private float _FallbackWidth = 480f;

        [Header("Content")]
        [SerializeField] private TMP_Text _NameText;
        [SerializeField] private TMP_Text _EnergyCostText;
        [SerializeField] private TMP_Text _RangeText;
        [SerializeField] private TMP_Text _VisibilityText;
        [SerializeField] private TMP_Text _TargetAlignmentText;
        [SerializeField] private TMP_Text _AoeText;
        [SerializeField] private TMP_Text _UsePerTurnText;
        [SerializeField] private TMP_Text _UsePerTargetText;
        [SerializeField] private TMP_Text _CooldownText;
        [SerializeField] private TMP_Text _DetailsText;
        [SerializeField] private TMP_Text _DescriptionText;

        private readonly StringBuilder _DetailsBuilder = new StringBuilder(256);
        private float _FixedWidth;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ResolveReferences();
            CacheFixedWidth();
            ConfigureVerticalLayout();
            SetTopPivotPreservingPosition(_ContentRoot);
        }

        private void OnEnable() => RefreshLayout();

#if UNITY_EDITOR
        private void OnValidate() => ResolveReferences();
#endif

        #endregion

        #region _____________________________| API

        public void Bind(SkillDefinition pSkill)
        {
            SetSkillSectionsActive(true);
            SetText(_NameText, pSkill != null ? pSkill.DisplayName : string.Empty);
            SetText(_EnergyCostText, pSkill != null ? $"Cost {pSkill.EnergyCost}" : string.Empty);
            SetText(_RangeText, pSkill != null ? $"{pSkill.RangeMin} - {pSkill.RangeMax}" : string.Empty);
            SetText(_VisibilityText, pSkill != null && pSkill.RequiresVisibility ? "Visibility" : "No Visibility");
            SetText(_TargetAlignmentText, pSkill != null ? ResolveAlignment(pSkill.TargetAlignment) : string.Empty);
            SetText(_AoeText, pSkill != null ? ResolveAoe(pSkill) : string.Empty);
            SetText(_UsePerTurnText, pSkill != null ? ResolveLimit(pSkill.UsePerTurn) : string.Empty);
            SetText(_UsePerTargetText, pSkill != null ? ResolveLimit(pSkill.UsePerTarget) : string.Empty);
            SetText(_CooldownText, pSkill != null ? pSkill.CooldownTurns.ToString() : string.Empty);
            SetDetails(pSkill != null ? BuildDetails(pSkill) : string.Empty);
            SetDescription(pSkill != null ? pSkill.Description : string.Empty, false);
            RefreshIfVisible();
        }

        public void Bind(PassiveDefinition pPassive)
        {
            SetSkillSectionsActive(false);
            SetText(_NameText, pPassive != null ? pPassive.DisplayName : string.Empty);
            SetDetails(string.Empty);
            SetDescription(pPassive != null ? pPassive.Description : string.Empty, false);
            RefreshIfVisible();
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            RefreshLayout();
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetDescription(string pText) => SetDescription(pText, true);

        public void RefreshLayout()
        {
            if (_ContentRoot == null)
                return;

            CacheFixedWidth();
            ConfigureVerticalLayout();
            SetWidth(_ContentRoot, _FixedWidth);

            Canvas.ForceUpdateCanvases();
            Rebuild(_ContentRoot);

            ResizeVariableSection(_DetailsRoot, _DetailsBackground, _DetailsText, _FixedWidth);
            ResizeVariableSection(_DescriptionRoot, _DescriptionBackground, _DescriptionText, _FixedWidth);
            Rebuild(_DetailsRoot);
            Rebuild(_DescriptionRoot);

            SetHeight(_ContentRoot, CalculateContentHeight(_ContentRoot));
            Rebuild(_ContentRoot);
        }

        #endregion

        #region _____________________________| CONTENT

        private string BuildDetails(SkillDefinition pSkill)
        {
            _DetailsBuilder.Clear();

            switch (pSkill.PrimaryEffectType)
            {
                case SkillPrimaryEffectType.Damage:
                    AppendDetail($"Damage: {pSkill.Power}");
                    break;
                case SkillPrimaryEffectType.Heal:
                    AppendDetail($"Healing: {pSkill.Power}");
                    break;
            }

            if (pSkill.HasLifeSteal)
                AppendDetail("Life Steal: 50% of enemy Health removed");
            if (pSkill.UseAoeDamageFalloff)
                AppendDetail($"AoE falloff: -{pSkill.AoeDamageFalloffPercentPerCell}% per cell");

            AppendAdditionalEffect(pSkill);
            AppendState(pSkill.AppliedState, pSkill.AppliedStateStacks, pSkill.AppliedStateDurationTurns, "Applies");
            AppendState(pSkill.CasterAppliedState, pSkill.CasterAppliedStateStacks, pSkill.CasterAppliedStateDurationTurns, "Caster gains");
            return _DetailsBuilder.ToString().TrimEnd();
        }

        private void AppendAdditionalEffect(SkillDefinition pSkill)
        {
            switch (pSkill.AdditionalEffectType)
            {
                case SkillAdditionalEffectType.Push:
                    AppendDetail($"Push: {pSkill.PushDistance} cell(s)");
                    break;
                case SkillAdditionalEffectType.Pull:
                    AppendDetail($"Pull: {pSkill.PushDistance} cell(s)");
                    break;
                case SkillAdditionalEffectType.Teleport:
                    AppendDetail("Teleport");
                    break;
                case SkillAdditionalEffectType.SwitchPositions:
                    AppendDetail("Switch positions");
                    break;
                case SkillAdditionalEffectType.Summon:
                    AppendDetail(pSkill.SummonUnit != null ? $"Summon: {pSkill.SummonUnit.DisplayName}" : "Summon");
                    break;
                case SkillAdditionalEffectType.CreateGlyph:
                    AppendDetail(pSkill.GlyphDefinition != null ? $"Glyph: {pSkill.GlyphDefinition.DisplayName}" : "Create glyph");
                    break;
                case SkillAdditionalEffectType.AdvanceActivePassiveProgression:
                    AppendDetail($"Advance passive progression: {pSkill.PassiveProgressionSteps} level(s)");
                    break;
                case SkillAdditionalEffectType.RepairAndToggleStates:
                    AppendDetail($"Repair: {pSkill.RepairAmount}");
                    break;
                case SkillAdditionalEffectType.ReduceStateDurations:
                    AppendDetail($"Reduce state durations: {pSkill.StateDurationReduction} turn(s)");
                    break;
            }
        }

        private void AppendState(StateDefinition pState, int pStacks, int pDuration, string pPrefix)
        {
            if (pState == null)
                return;

            string lStacks = pStacks > 1 ? $" x{pStacks}" : string.Empty;
            string lDuration = pDuration >= 0 ? $" for {pDuration} turn(s)" : string.Empty;
            AppendDetail($"{pPrefix} {pState.DisplayName}{lStacks}{lDuration}");
        }

        private void AppendDetail(string pText) => _DetailsBuilder.Append("• ").AppendLine(pText);

        private void SetDetails(string pText)
        {
            SetText(_DetailsText, pText);
            SetActive(_DetailsRoot, !string.IsNullOrWhiteSpace(pText));
        }

        private void SetDescription(string pText, bool pRefresh)
        {
            SetText(_DescriptionText, pText);
            SetActive(_DescriptionRoot, !string.IsNullOrWhiteSpace(pText));
            if (pRefresh)
                RefreshIfVisible();
        }

        #endregion

        #region _____________________________| LAYOUT

        private void CacheFixedWidth()
        {
            if (_FixedWidth > 0f)
                return;

            float lAuthoredWidth = _ContentRoot != null ? _ContentRoot.rect.width : 0f;
            _FixedWidth = lAuthoredWidth > 1f ? lAuthoredWidth : _FallbackWidth;
        }

        private void ResolveReferences()
        {
            Transform lLayout = transform.Find("VBox_Content") ?? transform.Find("VBoxContent");
            if (_ContentRoot == null)
                _ContentRoot = lLayout as RectTransform;
            if (_DetailsRoot == null)
                _DetailsRoot = lLayout?.Find("VBox_Details") as RectTransform;
            if (_DetailsBackground == null && _DetailsRoot != null)
                _DetailsBackground = _DetailsRoot.Find("Img_Details") as RectTransform;
            if (_DescriptionRoot == null)
                _DescriptionRoot = lLayout?.Find("VBox_Description") as RectTransform;
            if (_DescriptionBackground == null && _DescriptionRoot != null)
                _DescriptionBackground = _DescriptionRoot.Find("Img_Description") as RectTransform;

            if (_SkillOnlySections == null || _SkillOnlySections.Length == 0)
            {
                _SkillOnlySections = new[]
                {
                    lLayout?.Find("HBox1")?.gameObject,
                    lLayout?.Find("HBox2")?.gameObject,
                    lLayout?.Find("HBox3")?.gameObject
                };
            }

            if (_NameText == null)
                _NameText = FindText(lLayout, "VBox_Name/Img/Txt_Name");
            if (_EnergyCostText == null)
                _EnergyCostText = FindText(lLayout, "HBox1/HBox_Energy/Txt_Cost");
            if (_RangeText == null)
                _RangeText = FindText(lLayout, "HBox1/HBox_Range/Txt_Range");
            if (_VisibilityText == null)
                _VisibilityText = FindText(lLayout, "HBox1/HBoxVisibility/Txt_Visibility");
            if (_TargetAlignmentText == null)
                _TargetAlignmentText = FindText(lLayout, "HBox2/HBox_Type/Txt_Type");
            if (_AoeText == null)
                _AoeText = FindText(lLayout, "HBox2/HBox_PerUnitLimit/Txt_PerUnitLimit");
            if (_UsePerTurnText == null)
                _UsePerTurnText = FindText(lLayout, "HBox3/HBox_PerTurnLimit/HBox_C/Txt_Value");
            if (_UsePerTargetText == null)
                _UsePerTargetText = FindText(lLayout, "HBox3/HBox_PerUnitLimit/HBox_C/Txt_Value");
            if (_CooldownText == null)
                _CooldownText = FindText(lLayout, "HBox3/HBoxCooldown/HBox_C/Txt_Value");
            if (_DetailsText == null)
                _DetailsText = FindText(lLayout, "VBox_Details/Img/Txt_Details");
            if (_DescriptionText == null)
                _DescriptionText = FindText(lLayout, "VBox_Description/Img/Txt_Description");
        }

        private void ConfigureVerticalLayout()
        {
            if (_ContentRoot != null && _ContentRoot.TryGetComponent(out VerticalLayoutGroup lLayout))
            {
                lLayout.childControlHeight = false;
                lLayout.childForceExpandHeight = false;
                lLayout.childScaleHeight = false;
            }
        }

        private static void ResizeVariableSection(
            RectTransform pSection,
            RectTransform pBackground,
            TMP_Text pText,
            float pContentWidth)
        {
            if (pSection == null || pBackground == null || pText == null || !pSection.gameObject.activeSelf)
                return;

            RectTransform lTextRect = pText.rectTransform;
            float lAvailableWidth = Mathf.Max(
                1f,
                pContentWidth - GetHorizontalPadding(pSection) - GetHorizontalPadding(pBackground));
            float lTextHeight = Mathf.Ceil(pText.GetPreferredValues(pText.text, lAvailableWidth, 0f).y);
            SetHeight(lTextRect, lTextHeight);

            float lBackgroundHeight = lTextHeight + GetVerticalPadding(pBackground);
            SetHeight(pBackground, lBackgroundHeight);
            SetHeight(pSection, lBackgroundHeight + GetVerticalPadding(pSection));
        }

        private static float CalculateContentHeight(RectTransform pContent)
        {
            float lHeight = GetVerticalPadding(pContent);
            float lSpacing = pContent.TryGetComponent(out VerticalLayoutGroup lLayout) ? lLayout.spacing : 0f;
            int lIncludedChildren = 0;

            for (int lIndex = 0; lIndex < pContent.childCount; lIndex++)
            {
                if (pContent.GetChild(lIndex) is not RectTransform lChild ||
                    !lChild.gameObject.activeSelf ||
                    IsIgnoredByLayout(lChild))
                    continue;

                lHeight += lChild.rect.height;
                lIncludedChildren++;
            }

            return Mathf.Max(1f, lHeight + Mathf.Max(0, lIncludedChildren - 1) * lSpacing);
        }

        private static bool IsIgnoredByLayout(RectTransform pRect)
        {
            ILayoutIgnorer[] lIgnorers = pRect.GetComponents<ILayoutIgnorer>();
            for (int lIndex = 0; lIndex < lIgnorers.Length; lIndex++)
            {
                if (lIgnorers[lIndex].ignoreLayout)
                    return true;
            }

            return false;
        }

        private static float GetHorizontalPadding(RectTransform pRect) =>
            pRect != null && pRect.TryGetComponent(out HorizontalOrVerticalLayoutGroup lLayout)
                ? lLayout.padding.horizontal
                : 0f;

        private static float GetVerticalPadding(RectTransform pRect) =>
            pRect != null && pRect.TryGetComponent(out HorizontalOrVerticalLayoutGroup lLayout)
                ? lLayout.padding.vertical
                : 0f;

        private static void SetTopPivotPreservingPosition(RectTransform pRect)
        {
            if (pRect == null || Mathf.Approximately(pRect.pivot.y, 1f))
                return;

            Vector2 lPivot = pRect.pivot;
            float lHeight = pRect.rect.height;
            pRect.pivot = new Vector2(lPivot.x, 1f);
            pRect.anchoredPosition += Vector2.up * (1f - lPivot.y) * lHeight;
        }

        private void SetSkillSectionsActive(bool pActive)
        {
            if (_SkillOnlySections == null)
                return;

            for (int lIndex = 0; lIndex < _SkillOnlySections.Length; lIndex++)
            {
                GameObject lSection = _SkillOnlySections[lIndex];
                if (lSection != null && lSection.activeSelf != pActive)
                    lSection.SetActive(pActive);
            }
        }

        private void RefreshIfVisible()
        {
            if (isActiveAndEnabled)
                RefreshLayout();
        }

        private static void Rebuild(RectTransform pRect)
        {
            if (pRect != null && pRect.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(pRect);
        }

        private static void SetWidth(RectTransform pRect, float pWidth)
        {
            if (pRect != null)
                pRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pWidth);
        }

        private static void SetHeight(RectTransform pRect, float pHeight)
        {
            if (pRect != null)
                pRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pHeight);
        }

        private static void SetText(TMP_Text pText, string pValue)
        {
            if (pText != null)
                pText.text = pValue ?? string.Empty;
        }

        private static TMP_Text FindText(Transform pRoot, string pPath) =>
            pRoot != null ? pRoot.Find(pPath)?.GetComponent<TMP_Text>() : null;

        private static void SetActive(Component pComponent, bool pActive)
        {
            if (pComponent != null && pComponent.gameObject.activeSelf != pActive)
                pComponent.gameObject.SetActive(pActive);
        }

        private static string ResolveAlignment(SkillTargetAlignment pAlignment) => pAlignment switch
        {
            SkillTargetAlignment.Orthogonal => "Orthogonal",
            SkillTargetAlignment.Diagonal => "Diagonal",
            _ => "Any"
        };

        private static string ResolveAoe(SkillDefinition pSkill) => pSkill.AoeShape == SkillAoeShape.Single
            ? "Single"
            : $"{pSkill.AoeShape} {pSkill.AoeSize}";

        private static string ResolveLimit(int pLimit) => pLimit > 0 ? pLimit.ToString() : "∞";

        #endregion
    }
}
