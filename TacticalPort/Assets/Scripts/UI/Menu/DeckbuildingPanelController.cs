#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Menu UI
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.App;
using TacticalPort.Data;
using TacticalPort.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class DeckbuildingPanelController : MonoBehaviour, ITeamSelectionPopupView
    {
        #region _____________________________/ TYPES

        [Serializable]
        public sealed class CharacterPreviewView
        {
            public TMP_Text Name;
            public Image Portrait;
            public TMP_Text Description;
            public DeckbuildingUnitPreviewView Preview;
        }

        [Serializable]
        public sealed class CoreValueView
        {
            public TMP_Text Value;
        }

        [Serializable]
        public sealed class PassiveSlotView
        {
            public Button Button;
            public Image Background;
            public Image Icon;
            public TMP_Text Name;
            public DeckbuildingSkillTooltipView Tooltip;
            public GameObject SelectedIndicator;
            [NonSerialized] public Image Ornament;
            [NonSerialized] public Image OrnamentSecondary;
            [NonSerialized] public Material IconMaterialInstance;
        }

        [Serializable]
        public sealed class SkillSlotView
        {
            public Button Button;
            public Image Background;
            [NonSerialized] public Image Circle;
            [NonSerialized] public Image Ornament;
            [NonSerialized] public Image OrnamentSecondary;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text Meta;
            public DeckbuildingSkillTooltipView Tooltip;
            [NonSerialized] public Material IconMaterialInstance;
        }

        [Serializable]
        public sealed class StatCellView
        {
            public TMP_Text Label;
            public TMP_Text Value;
        }

        private sealed class CharacterPickerItemView
        {
            public Button Button;
            public Image Background;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text State;
        }

        private sealed class SkillPickerItemView
        {
            public Button Button;
            public Image Background;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text Meta;
            public TMP_Text State;
            public DeckbuildingSkillTooltipView Tooltip;
        }

        #endregion

        #region _____________________________/ VALUES

        private const int DefaultTeamSize = 3;
        private const int MaxPassives = 2;
        private const int EquippedSkillCount = 6;

        [Header("Data")]
        [SerializeField] private PlayableRosterDefinition _PlayableRoster;
        [SerializeField, HideInInspector] private List<UnitDefinition> _AvailableUnits = new List<UnitDefinition>();
        [SerializeField, Min(1)] private int _TeamSize = DefaultTeamSize;
        [SerializeField, Min(0)] private int _CurrentSlotIndex;

        [Header("Main Panel")]
        [SerializeField] private CharacterPreviewView _Character = new CharacterPreviewView();
        [SerializeField] private CoreValueView _EnergyValue = new CoreValueView();
        [SerializeField] private CoreValueView _HealthValue = new CoreValueView();
        [SerializeField] private CoreValueView _MobilityValue = new CoreValueView();
        [SerializeField] private List<PassiveSlotView> _PassiveSlots = new List<PassiveSlotView>(MaxPassives);
        [SerializeField] private List<SkillSlotView> _SkillSlots = new List<SkillSlotView>(EquippedSkillCount);
        [SerializeField] private List<StatCellView> _Stats = new List<StatCellView>(5);
        [SerializeField] private List<DeckbuildingStatAllocationView> _StatAllocationViews = new List<DeckbuildingStatAllocationView>(9);
        [SerializeField] private Button _ChangeCharacterButton;
        [SerializeField] private Button _EditStatsButton;
        [SerializeField] private Button _ValidateButton;
        [SerializeField] private Button _CloseButton;

        [Header("Character Picker")]
        [SerializeField] private GameObject _CharacterPickerPopup;
        [SerializeField] private RectTransform _CharacterPickerRoot;
        [SerializeField] private RectTransform _CharacterPickerItemTemplate;
        [SerializeField] private Button _CloseCharacterPickerButton;

        [Header("Skill Picker")]
        [SerializeField] private GameObject _SkillPickerPopup;
        [SerializeField] private RectTransform _SkillPickerRoot;
        [SerializeField] private RectTransform _SkillPickerItemTemplate;
        [SerializeField] private Button _CloseSkillPickerButton;

        [Header("Stats Edit Placeholder")]
        [SerializeField] private GameObject _StatsEditPlaceholderPopup;
        [SerializeField] private Button _CloseStatsPlaceholderButton;

        [Header("Visual States")]
        [SerializeField] private Color _NormalCellColor = new Color(0.95f, 0.96f, 0.98f, 1f);
        [SerializeField] private Color _SelectedCellColor = new Color(0.75f, 0.9f, 1f, 1f);
        [SerializeField] private Color _DisabledCellColor = new Color(0.72f, 0.75f, 0.8f, 1f);

        private TeamPresetDraft _Draft;
        private int _PendingSkillSlotIndex = -1;
        private bool _HasHookedStaticButtons;

        private static readonly int SkillColorPropertyId = Shader.PropertyToID("_ColorB");
        private static readonly int PassiveColorPropertyId = Shader.PropertyToID("_ColorA");

        public event Action TeamSelectionChanged;
        public event Action Closed;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            EnsureStaticButtonsHooked();
            RefreshSelectionView();
        }

        private void OnEnable() => RefreshSelectionView();

        private void OnDestroy()
        {
            for (int lIndex = 0; lIndex < _PassiveSlots.Count; lIndex++)
                DestroyRuntimeMaterial(_PassiveSlots[lIndex]?.IconMaterialInstance);

            for (int lIndex = 0; lIndex < _SkillSlots.Count; lIndex++)
                DestroyRuntimeMaterial(_SkillSlots[lIndex]?.IconMaterialInstance);
        }

        #endregion

        #region _____________________________| ITeamSelectionPopupView

        public void RefreshSelectionView()
        {
            InitializeSelection();
            HideAllPopups();
            RefreshMainPanel();
        }

        public void ShowForTeamSlot(int pSlotIndex)
        {
            _CurrentSlotIndex = Mathf.Max(0, pSlotIndex);
            SetActive(gameObject, true);
            EnsureStaticButtonsHooked();
            InitializeSelection();
            _CurrentSlotIndex = Mathf.Clamp(_CurrentSlotIndex, 0, Mathf.Max(0, (_Draft?.SelectedUnits.Count ?? 0) - 1));
            HideAllPopups();
            RefreshMainPanel();
        }

        public void HidePanel()
        {
            HideAllPopups();
            PrepareTooltipsForDisable(gameObject);
            SetActive(gameObject, false);
            Closed?.Invoke();
        }

        #endregion

        #region _____________________________| SETUP

        private void EnsureStaticButtonsHooked()
        {
            if (_HasHookedStaticButtons)
                return;

            HookStaticButtons();
            _HasHookedStaticButtons = true;
        }

        private void HookStaticButtons()
        {
            HookButton(_ChangeCharacterButton, ShowCharacterPicker);
            HookButton(_EditStatsButton, ShowStatsPlaceholder);
            HookButton(_ValidateButton, ValidateChanges);
            HookButton(_CloseButton, HidePanel);
            HookButton(_CloseCharacterPickerButton, HideCharacterPicker);
            HookButton(_CloseSkillPickerButton, HideSkillPicker);
            HookButton(_CloseStatsPlaceholderButton, HideStatsPlaceholder);
        }

        private void InitializeSelection()
        {
            TeamPresetState.EnsureInitializedForLocalUse();
            if (!TeamSelectionState.HasSelection)
                TeamSelectionState.LoadSavedUnitIds();

            _Draft = new TeamPresetDraft(
                _TeamSize,
                _PlayableRoster != null ? _PlayableRoster.Units : _AvailableUnits,
                TeamSelectionState.SelectedUnits,
                TeamSelectionState.SelectedUnitIds,
                TeamPresetState.ActivePreset);
            _CurrentSlotIndex = Mathf.Clamp(_CurrentSlotIndex, 0, Mathf.Max(0, _Draft.SelectedUnits.Count - 1));
            UpdateValidateButtonState();
        }

        #endregion

        #region _____________________________| MAIN PANEL

        private void RefreshMainPanel()
        {
            UnitDefinition lUnit = CurrentUnit;
            BindCharacter(lUnit);
            BindCoreValues(lUnit);
            BindPassives(lUnit);
            BindSkills(lUnit);
            BindStats(lUnit);
        }

        private void BindCharacter(UnitDefinition pUnit)
        {
            if (_Character.Name != null)
                _Character.Name.text = pUnit != null ? pUnit.DisplayName : Localize("common.empty", "Empty");

            if (_Character.Description != null)
            {
                _Character.Description.text = pUnit != null
                    ? $"{pUnit.Description}\nGeneral Damage: {pUnit.GeneralDamagePercent}% | General Resistance: {pUnit.GeneralResistancePercent}%"
                    : string.Empty;
            }

            if (_Character.Preview != null)
            {
                _Character.Preview.Bind(pUnit);
                return;
            }

            if (_Character.Portrait != null)
            {
                Sprite lSprite = pUnit != null ? pUnit.DisplaySprite : null;
                _Character.Portrait.sprite = lSprite;
                _Character.Portrait.preserveAspect = true;
                _Character.Portrait.enabled = lSprite != null;
                _Character.Portrait.color = Color.white;
            }
        }

        private void BindCoreValues(UnitDefinition pUnit)
        {
            UnitStatAllocationPreset lStats = EnsureDraftBuild(pUnit)?.StatAllocations;
            SetCoreValue(_EnergyValue, pUnit != null
                ? UnitStatAllocationRules.GetEffectiveValue(pUnit, lStats, UnitStatType.Energy).ToString()
                : "-");
            SetCoreValue(_HealthValue, pUnit != null
                ? UnitStatAllocationRules.GetEffectiveValue(pUnit, lStats, UnitStatType.Health).ToString()
                : "-");
            SetCoreValue(_MobilityValue, pUnit != null
                ? UnitStatAllocationRules.GetEffectiveValue(pUnit, lStats, UnitStatType.Mobility).ToString()
                : "-");
        }

        private static void SetCoreValue(CoreValueView pView, string pValue)
        {
            if (pView?.Value != null)
                pView.Value.text = pValue;
        }

        private void BindPassives(UnitDefinition pUnit)
        {
            PassiveDefinition lSelectedPassive = GetSelectedPassive(pUnit);

            for (int lIndex = 0; lIndex < _PassiveSlots.Count; lIndex++)
            {
                PassiveDefinition lPassive = pUnit != null && pUnit.Passives != null && lIndex < pUnit.Passives.Count
                    ? pUnit.Passives[lIndex]
                    : null;

                PassiveSlotView lSlot = _PassiveSlots[lIndex];
                bool lHasPassive = lPassive != null;
                bool lSelected = lHasPassive && lPassive == lSelectedPassive;

                if (lSlot.Name != null)
                lSlot.Name.text = lHasPassive ? lPassive.DisplayName : Localize("common.empty", "Empty");

                SetIconSprite(lSlot.Icon, lHasPassive ? lPassive.Icon : null);
                ApplyPassiveSelectionVisuals(lSlot, lSelected);
                lSlot.Tooltip?.Bind(lPassive);
                SetActive(lSlot.SelectedIndicator, lSelected);

                if (lSlot.Button != null)
                {
                    int lPassiveIndex = lIndex;
                    lSlot.Button.onClick.RemoveAllListeners();
                    lSlot.Button.interactable = lHasPassive;
                    lSlot.Button.onClick.AddListener(() => SelectPassive(lPassiveIndex));
                }
            }
        }

        private void BindSkills(UnitDefinition pUnit)
        {
            List<SkillDefinition> lSkills = GetEquippedSkills(pUnit);

            for (int lIndex = 0; lIndex < _SkillSlots.Count; lIndex++)
            {
                SkillDefinition lSkill = lIndex < lSkills.Count ? lSkills[lIndex] : null;
                SkillSlotView lSlot = _SkillSlots[lIndex];

                if (lSlot.Name != null)
                    lSlot.Name.text = lSkill != null ? lSkill.DisplayName : Localize("common.empty", "Empty");

                if (lSlot.Meta != null)
                    lSlot.Meta.text = lSkill != null ? BuildShortSkillMeta(lSkill) : string.Empty;

                SetIconSprite(lSlot.Icon, lSkill != null ? lSkill.Icon : null);
                ApplySkillColor(lSlot, lSkill);
                lSlot.Tooltip?.Bind(lSkill);

                if (lSlot.Button != null)
                {
                    int lSkillIndex = lIndex;
                    lSlot.Button.onClick.RemoveAllListeners();
                    lSlot.Button.interactable = pUnit != null;
                    lSlot.Button.onClick.AddListener(() => ShowSkillPicker(lSkillIndex));
                }
            }
        }

        private void BindStats(UnitDefinition pUnit)
        {
            TeamPresetDraft.UnitBuild lBuild = EnsureDraftBuild(pUnit);
            UnitStatAllocationPreset lStats = lBuild?.StatAllocations;
            for (int lIndex = 0; lIndex < _StatAllocationViews.Count; lIndex++)
            {
                DeckbuildingStatAllocationView lView = _StatAllocationViews[lIndex];
                if (lView == null)
                    continue;

                UnitStatType lStat = lView.Stat;
                if (lStat == UnitStatType.RemainingPoints)
                {
                    lView.Bind(
                        UnitStatAllocationRules.GetRemainingPoints(pUnit, lStats),
                        false,
                        false,
                        null,
                        null);
                    continue;
                }

                int lDisplayedValue = UnitStatAllocationRules.GetEffectiveValue(pUnit, lStats, lStat);
                bool lCanDecrease = UnitStatAllocationRules.GetValue(lStats, lStat) > 0;
                bool lCanIncrease = CanChangeStat(pUnit, lStats, lStat, 1);
                lView.Bind(
                    lDisplayedValue,
                    lCanDecrease,
                    lCanIncrease,
                    () => ChangeStat(lStat, -1),
                    () => ChangeStat(lStat, 1));
            }

            if (_StatAllocationViews.Count > 0)
                return;

            BindStat(0, "Dmg Melee", pUnit != null ? pUnit.MeleeDamagePercent.ToString() : "-");
            BindStat(1, "Res Melee", pUnit != null ? pUnit.MeleeResistancePercent.ToString() : "-");
            BindStat(2, "Dmg Distance", pUnit != null ? pUnit.RangedDamagePercent.ToString() : "-");
            BindStat(3, "Res Distance", pUnit != null ? pUnit.RangedResistancePercent.ToString() : "-");
            BindStat(4, "Velocity", pUnit != null ? pUnit.Velocity.ToString() : "-");
            BindStat(5, "Dmg General", pUnit != null ? pUnit.GeneralDamagePercent.ToString() : "-");
            BindStat(6, "Res General", pUnit != null ? pUnit.GeneralResistancePercent.ToString() : "-");
        }

        private bool CanChangeStat(
            UnitDefinition pUnit,
            UnitStatAllocationPreset pAllocations,
            UnitStatType pStat,
            int pDelta)
        {
            return pAllocations != null && _Draft != null && _Draft.CanChangeStat(pUnit, pStat, pDelta);
        }

        private void ChangeStat(UnitStatType pStat, int pDelta)
        {
            UnitDefinition lUnit = CurrentUnit;
            if (_Draft == null || !_Draft.TryChangeStat(lUnit, pStat, pDelta))
                return;

            UpdateValidateButtonState();
            BindCoreValues(lUnit);
            BindStats(lUnit);
        }

        private void BindStat(int pIndex, string pLabel, string pValue)
        {
            if (pIndex < 0 || pIndex >= _Stats.Count)
                return;

            StatCellView lView = _Stats[pIndex];
            if (lView.Label != null)
                lView.Label.text = pLabel;
            if (lView.Value != null)
                lView.Value.text = pValue;
        }

        #endregion

        #region _____________________________| CHARACTER PICKER

        private void ShowCharacterPicker()
        {
            BuildCharacterPicker();
            SetActive(_CharacterPickerPopup, true);
        }

        private void HideCharacterPicker() => SetActive(_CharacterPickerPopup, false);

        private void BuildCharacterPicker()
        {
            ClearChildren(_CharacterPickerRoot, _CharacterPickerItemTemplate);

            IReadOnlyList<UnitDefinition> lAvailableUnits = _Draft?.AvailableUnits;
            for (int lIndex = 0; lAvailableUnits != null && lIndex < lAvailableUnits.Count; lIndex++)
            {
                RectTransform lInstance = CreateItem(_CharacterPickerItemTemplate, _CharacterPickerRoot, $"Item_Character_{lIndex + 1}");
                if (lInstance == null)
                    continue;

                CharacterPickerItemView lItem = CreateCharacterPickerItemView(lInstance);
                BindCharacterPickerItem(lItem, lAvailableUnits[lIndex]);
            }
        }

        private void BindCharacterPickerItem(CharacterPickerItemView pItem, UnitDefinition pUnit)
        {
            bool lSelectedCurrent = pUnit != null && pUnit == CurrentUnit;
            bool lUsedElsewhere = IsUnitSelectedInAnotherSlot(pUnit);

            if (pItem.Name != null)
                pItem.Name.text = pUnit != null ? pUnit.DisplayName : Localize("common.empty", "Empty");

            if (pItem.State != null)
                pItem.State.text = lSelectedCurrent
                    ? Localize("common.selected", "Selected")
                    : lUsedElsewhere ? Localize("common.already_used", "Already used") : string.Empty;

            SetImage(pItem.Icon, pUnit != null ? pUnit.DisplaySprite : null);
            SetBackground(pItem.Background, lSelectedCurrent ? _SelectedCellColor : lUsedElsewhere ? _DisabledCellColor : _NormalCellColor);

            if (pItem.Button != null)
            {
                pItem.Button.onClick.RemoveAllListeners();
                pItem.Button.interactable = pUnit != null && !lUsedElsewhere;
                pItem.Button.onClick.AddListener(() => SelectCharacter(pUnit));
            }
        }

        private void SelectCharacter(UnitDefinition pUnit)
        {
            if (_Draft == null || !_Draft.TrySelectUnit(_CurrentSlotIndex, pUnit))
                return;

            UpdateValidateButtonState();
            HideCharacterPicker();
            RefreshMainPanel();
        }

        #endregion

        #region _____________________________| PASSIVES

        private void SelectPassive(int pPassiveIndex)
        {
            UnitDefinition lUnit = CurrentUnit;
            if (lUnit == null || lUnit.Passives == null || pPassiveIndex < 0 || pPassiveIndex >= lUnit.Passives.Count)
                return;

            PassiveDefinition lPassive = lUnit.Passives[pPassiveIndex];
            if (lPassive == null)
                return;

            if (_Draft == null || !_Draft.TrySelectPassive(lUnit, lPassive))
                return;

            UpdateValidateButtonState();
            BindPassives(lUnit);
        }

        #endregion

        #region _____________________________| SKILL PICKER

        private void ShowSkillPicker(int pSkillSlotIndex)
        {
            if (CurrentUnit == null)
                return;

            _PendingSkillSlotIndex = Mathf.Clamp(pSkillSlotIndex, 0, EquippedSkillCount - 1);
            BuildSkillPicker();
            SetActive(_SkillPickerPopup, true);
        }

        private void HideSkillPicker()
        {
            _PendingSkillSlotIndex = -1;
            PrepareTooltipsForDisable(_SkillPickerPopup);
            SetActive(_SkillPickerPopup, false);
        }

        private void BuildSkillPicker()
        {
            ClearChildren(_SkillPickerRoot, _SkillPickerItemTemplate);

            UnitDefinition lUnit = CurrentUnit;
            if (lUnit == null || lUnit.Skills == null)
                return;

            for (int lIndex = 0; lIndex < lUnit.Skills.Count; lIndex++)
            {
                RectTransform lInstance = CreateItem(_SkillPickerItemTemplate, _SkillPickerRoot, $"Item_Skill_{lIndex + 1}");
                if (lInstance == null)
                    continue;

                SkillPickerItemView lItem = CreateSkillPickerItemView(lInstance);
                BindSkillPickerItem(lItem, lUnit.Skills[lIndex]);
            }
        }

        private void BindSkillPickerItem(SkillPickerItemView pItem, SkillDefinition pSkill)
        {
            bool lEquippedHere = IsSkillEquippedAtPendingSlot(pSkill);
            bool lEquippedElsewhere = IsSkillEquippedInAnotherSlot(pSkill);

            if (pItem.Name != null)
                pItem.Name.text = pSkill != null ? pSkill.DisplayName : Localize("common.empty", "Empty");

            if (pItem.Meta != null)
                pItem.Meta.text = pSkill != null ? BuildFullSkillMeta(pSkill) : string.Empty;

            if (pItem.State != null)
                pItem.State.text = lEquippedHere
                    ? Localize("common.equipped", "Equipped")
                    : lEquippedElsewhere ? Localize("common.already_equipped", "Already equipped") : string.Empty;

            SetIconSprite(pItem.Icon, pSkill != null ? pSkill.Icon : null);
            SetBackground(pItem.Background, lEquippedHere ? _SelectedCellColor : lEquippedElsewhere ? _DisabledCellColor : _NormalCellColor);
            pItem.Tooltip?.Bind(pSkill);

            if (pItem.Button != null)
            {
                pItem.Button.onClick.RemoveAllListeners();
                pItem.Button.interactable = pSkill != null && !lEquippedElsewhere;
                pItem.Button.onClick.AddListener(() => SelectSkill(pSkill));
            }
        }

        private void SelectSkill(SkillDefinition pSkill)
        {
            UnitDefinition lUnit = CurrentUnit;
            int lSlotIndex = _PendingSkillSlotIndex;
            if (lUnit == null || pSkill == null || lSlotIndex < 0 || lSlotIndex >= EquippedSkillCount || IsSkillEquippedInAnotherSlot(pSkill))
                return;

            if (_Draft == null || !_Draft.TrySelectSkill(lUnit, lSlotIndex, pSkill))
            {
                return;
            }

            UpdateValidateButtonState();
            HideSkillPicker();
            BindSkills(lUnit);
        }

        #endregion

        #region _____________________________| STATS PLACEHOLDER

        private void ShowStatsPlaceholder() => SetActive(_StatsEditPlaceholderPopup, true);
        private void HideStatsPlaceholder() => SetActive(_StatsEditPlaceholderPopup, false);

        #endregion

        #region _____________________________| BUILD STATE

        private TeamPresetDraft.UnitBuild EnsureDraftBuild(UnitDefinition pUnit) => _Draft?.GetBuild(pUnit);

        private PassiveDefinition GetSelectedPassive(UnitDefinition pUnit)
        {
            if (pUnit == null)
                return null;

            return EnsureDraftBuild(pUnit)?.Passive;
        }

        private List<SkillDefinition> GetEquippedSkills(UnitDefinition pUnit)
        {
            if (pUnit == null)
                return new List<SkillDefinition>(EquippedSkillCount);

            TeamPresetDraft.UnitBuild lBuild = EnsureDraftBuild(pUnit);
            return lBuild != null
                ? new List<SkillDefinition>(lBuild.Skills)
                : new List<SkillDefinition>(EquippedSkillCount);
        }

        #endregion

        #region _____________________________| HELPERS

        private UnitDefinition CurrentUnit => _Draft?.GetUnit(_CurrentSlotIndex);

        private void ValidateChanges()
        {
            string lFailure = "The team preset draft is missing.";
            if (_Draft == null || !_Draft.Validate(out lFailure))
            {
                Debug.LogWarning($"Deckbuilding preset validation failed: {lFailure}", this);
                return;
            }

            if (!_Draft.HasPendingChanges)
            {
                HidePanel();
                return;
            }

            if (!TeamSelectionState.TryApplyDraftAndSave(_Draft, out lFailure))
            {
                Debug.LogWarning($"Deckbuilding preset persistence failed: {lFailure}", this);
                return;
            }

            TeamSelectionChanged?.Invoke();
            HidePanel();
        }

        private void UpdateValidateButtonState()
        {
            if (_ValidateButton != null)
                _ValidateButton.interactable = IsDraftValid();
        }

        private bool IsDraftValid() => _Draft?.IsValid == true;

        private bool IsUnitSelectedInAnotherSlot(UnitDefinition pUnit)
        {
            return _Draft?.IsUnitSelectedElsewhere(pUnit, _CurrentSlotIndex) == true;
        }

        private bool IsSkillEquippedAtPendingSlot(SkillDefinition pSkill)
        {
            IReadOnlyList<SkillDefinition> lSkills = EnsureDraftBuild(CurrentUnit)?.Skills;
            return pSkill != null && lSkills != null && _PendingSkillSlotIndex >= 0 && _PendingSkillSlotIndex < lSkills.Count && lSkills[_PendingSkillSlotIndex] == pSkill;
        }

        private bool IsSkillEquippedInAnotherSlot(SkillDefinition pSkill)
        {
            return _Draft?.IsSkillEquipped(CurrentUnit, pSkill, _PendingSkillSlotIndex) == true;
        }

        private static string BuildShortSkillMeta(SkillDefinition pSkill) =>
            pSkill != null ? $"Energy {pSkill.EnergyCost} | {pSkill.RangeMin}-{pSkill.RangeMax}" : string.Empty;

        private static string BuildFullSkillMeta(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return string.Empty;

            string lMeta = $"Energy {pSkill.EnergyCost} | Range {pSkill.RangeMin}-{pSkill.RangeMax}";
            lMeta += pSkill.RequiresVisibility ? " | Visibility" : " | No Visibility";
            if (pSkill.CooldownTurns > 0)
                lMeta += $" | CD {pSkill.CooldownTurns}";
            if (pSkill.UsePerTurn > 0)
                lMeta += $" | {pSkill.UsePerTurn}/turn";
            if (pSkill.UsePerTarget > 0)
                lMeta += $" | {pSkill.UsePerTarget}/target";
            return lMeta;
        }

        private static void HookButton(Button pButton, UnityEngine.Events.UnityAction pAction)
        {
            if (pButton == null)
                return;

            pButton.onClick.RemoveAllListeners();
            pButton.onClick.AddListener(pAction);
        }

        private void HideAllPopups()
        {
            HideCharacterPicker();
            HideSkillPicker();
            HideStatsPlaceholder();
        }

        private static void SetActive(GameObject pTarget, bool pActive)
        {
            if (pTarget != null && pTarget.activeSelf != pActive)
                pTarget.SetActive(pActive);
        }

        private static void PrepareTooltipsForDisable(GameObject pRoot)
        {
            if (pRoot == null || !pRoot.activeInHierarchy)
                return;

            DeckbuildingSkillTooltipView[] lTooltips = pRoot.GetComponentsInChildren<DeckbuildingSkillTooltipView>(true);
            for (int lIndex = 0; lIndex < lTooltips.Length; lIndex++)
                lTooltips[lIndex]?.PrepareForOwnerDisable();
        }

        private static RectTransform CreateItem(RectTransform pTemplate, RectTransform pRoot, string pName)
        {
            if (pTemplate == null || pRoot == null)
                return null;

            RectTransform lInstance = Instantiate(pTemplate, pRoot);
            lInstance.name = pName;
            lInstance.gameObject.SetActive(true);
            return lInstance;
        }

        private static void ClearChildren(RectTransform pRoot, RectTransform pTemplate)
        {
            if (pRoot == null)
                return;

            for (int lIndex = pRoot.childCount - 1; lIndex >= 0; lIndex--)
            {
                Transform lChild = pRoot.GetChild(lIndex);
                if (lChild == null || lChild == pTemplate)
                    continue;

                if (Application.isPlaying)
                {
                    lChild.gameObject.SetActive(false);
                    Destroy(lChild.gameObject);
                }
                else
                    DestroyImmediate(lChild.gameObject);
            }
        }

        private static CharacterPickerItemView CreateCharacterPickerItemView(Transform pRoot) =>
            new CharacterPickerItemView
            {
                Button = pRoot.GetComponent<Button>(),
                Background = pRoot.GetComponent<Image>(),
                Icon = FindChildComponent<Image>(pRoot, "Img_Icon"),
                Name = FindChildComponent<TMP_Text>(pRoot, "Txt_Name"),
                State = FindChildComponent<TMP_Text>(pRoot, "Txt_State")
            };

        private static SkillPickerItemView CreateSkillPickerItemView(Transform pRoot) =>
            new SkillPickerItemView
            {
                Button = pRoot.GetComponent<Button>(),
                Background = pRoot.GetComponent<Image>(),
                Icon = FindChildComponent<Image>(pRoot, "Img_Icon"),
                Name = FindChildComponent<TMP_Text>(pRoot, "Txt_Name"),
                Meta = FindChildComponent<TMP_Text>(pRoot, "Txt_Meta"),
                State = FindChildComponent<TMP_Text>(pRoot, "Txt_State"),
                Tooltip = pRoot.GetComponent<DeckbuildingSkillTooltipView>()
            };

        private static T FindChildComponent<T>(Transform pRoot, string pChildName) where T : Component
        {
            if (pRoot == null)
                return null;

            Transform lChild = pRoot.Find(pChildName);
            return lChild != null && lChild.TryGetComponent(out T lComponent) ? lComponent : null;
        }

        private static void SetImage(Image pImage, Sprite pSprite)
        {
            if (pImage == null)
                return;

            pImage.sprite = pSprite;
            pImage.preserveAspect = true;
            pImage.enabled = pSprite != null;
            pImage.color = Color.white;
        }

        private static void SetIconSprite(Image pImage, Sprite pSprite)
        {
            if (pImage == null)
                return;

            pImage.sprite = pSprite;
            pImage.preserveAspect = true;
            pImage.enabled = pSprite != null;
        }

        private void ApplySkillColor(SkillSlotView pSlot, SkillDefinition pSkill)
        {
            if (pSlot == null)
                return;

            Transform lRoot = pSlot.Button != null ? pSlot.Button.transform : null;
            pSlot.Circle ??= FindDescendantComponent<Image>(lRoot, "Btn_Skill_Circle");
            pSlot.Ornament ??= FindDescendantComponent<Image>(lRoot, "Btn_Skill_Ornament_L");
            pSlot.OrnamentSecondary ??= FindDescendantComponent<Image>(lRoot, "Btn_Skill_Ornament_R");

            Color lColor = pSkill != null ? ThemeManager.GetSkillColor(pSkill) : _DisabledCellColor;
            SetBackground(pSlot.Background, lColor);
            SetBackground(pSlot.Circle, lColor);
            Color lOrnamentColor = WithHsvValue(lColor, 1f);
            SetBackground(pSlot.Ornament, lOrnamentColor);
            SetBackground(pSlot.OrnamentSecondary, lOrnamentColor);
            SetIconGradientColor(pSlot.Icon, ref pSlot.IconMaterialInstance, SkillColorPropertyId, lColor);
        }

        private static void ApplyPassiveSelectionVisuals(PassiveSlotView pSlot, bool pSelected)
        {
            if (pSlot == null)
                return;

            Transform lRoot = pSlot.Button != null ? pSlot.Button.transform : null;
            pSlot.Ornament ??= FindDescendantComponent<Image>(lRoot, "Btn_Skill_Ornament_L");
            pSlot.OrnamentSecondary ??= FindDescendantComponent<Image>(lRoot, "Btn_Skill_Ornament_R");

            Color lColor = pSelected ? ThemeManager.GoldColor : ThemeManager.SilverColor;
            SetBackground(pSlot.Ornament, lColor);
            SetBackground(pSlot.OrnamentSecondary, lColor);
            SetIconGradientColor(pSlot.Icon, ref pSlot.IconMaterialInstance, PassiveColorPropertyId, lColor);
        }

        private static void SetIconGradientColor(Image pIcon, ref Material pMaterialInstance, int pColorPropertyId, Color pColor)
        {
            if (pIcon == null)
                return;

            Material lSourceMaterial = pIcon.material;
            if (lSourceMaterial == null || !lSourceMaterial.HasProperty(pColorPropertyId))
                return;

            if (pMaterialInstance == null)
            {
                pMaterialInstance = new Material(lSourceMaterial)
                {
                    name = $"{lSourceMaterial.name} ({pIcon.name} Runtime)",
                    hideFlags = HideFlags.DontSave
                };
                pIcon.material = pMaterialInstance;
            }

            pMaterialInstance.SetColor(pColorPropertyId, pColor);
        }

        private static Color WithHsvValue(Color pColor, float pValue)
        {
            Color.RGBToHSV(pColor, out float lHue, out float lSaturation, out _);
            Color lResult = Color.HSVToRGB(lHue, lSaturation, Mathf.Clamp01(pValue));
            lResult.a = pColor.a;
            return lResult;
        }

        private static void DestroyRuntimeMaterial(Material pMaterial)
        {
            if (pMaterial != null)
                UnityEngine.Object.Destroy(pMaterial);
        }

        private static T FindDescendantComponent<T>(Transform pRoot, string pName) where T : Component
        {
            if (pRoot == null || string.IsNullOrWhiteSpace(pName))
                return null;

            T[] lComponents = pRoot.GetComponentsInChildren<T>(true);
            for (int lIndex = 0; lIndex < lComponents.Length; lIndex++)
            {
                T lComponent = lComponents[lIndex];
                if (lComponent != null && lComponent.name == pName)
                    return lComponent;
            }

            return null;
        }

        private static void SetBackground(Image pImage, Color pColor)
        {
            if (pImage != null)
                pImage.color = pColor;
        }

        private static string Localize(string pKey, string pEnglishFallback) =>
            GameLocalization.Get(GameLocalization.UiTable, pKey, pEnglishFallback);

        #endregion
    }
}
