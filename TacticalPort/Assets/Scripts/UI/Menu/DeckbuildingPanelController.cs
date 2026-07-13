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
        }

        [Serializable]
        public sealed class SkillSlotView
        {
            public Button Button;
            public Image Background;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text Meta;
            public DeckbuildingSkillTooltipView Tooltip;
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

        private sealed class DraftUnitBuild
        {
            public PassiveDefinition Passive;
            public readonly List<SkillDefinition> Skills = new List<SkillDefinition>(EquippedSkillCount);
            public UnitStatAllocationPreset StatAllocations = new UnitStatAllocationPreset();
        }

        #endregion

        #region _____________________________/ VALUES

        private const int DefaultTeamSize = 3;
        private const int MaxPassives = 2;
        private const int EquippedSkillCount = 6;

        [Header("Data")]
        [SerializeField] private List<UnitDefinition> _AvailableUnits = new List<UnitDefinition>();
        [SerializeField, Min(1)] private int _TeamSize = DefaultTeamSize;
        [SerializeField, Min(0)] private int _CurrentSlotIndex;

        [Header("Main Panel")]
        [SerializeField] private CharacterPreviewView _Character = new CharacterPreviewView();
        [SerializeField] private CoreValueView _ActionPointsValue = new CoreValueView();
        [SerializeField] private CoreValueView _HealthValue = new CoreValueView();
        [SerializeField] private CoreValueView _MoveRangeValue = new CoreValueView();
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

        private readonly List<UnitDefinition> _SelectedUnits = new List<UnitDefinition>(DefaultTeamSize);
        private readonly Dictionary<UnitDefinition, DraftUnitBuild> _DraftBuilds = new Dictionary<UnitDefinition, DraftUnitBuild>();
        private int _PendingSkillSlotIndex = -1;
        private bool _HasHookedStaticButtons;
        private bool _HasPendingChanges;

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
            _CurrentSlotIndex = Mathf.Clamp(_CurrentSlotIndex, 0, Mathf.Max(0, _SelectedUnits.Count - 1));
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
            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();
            _SelectedUnits.Clear();
            _DraftBuilds.Clear();
            _HasPendingChanges = false;

            if (lAvailableUnits.Count == 0)
            {
                UpdateValidateButtonState();
                return;
            }

            if (!TryLoadCurrentSelection(lAvailableUnits, _SelectedUnits))
                FillDefaultSelection(lAvailableUnits, _SelectedUnits);

            FillMissingSlots(lAvailableUnits, _SelectedUnits);
            _CurrentSlotIndex = Mathf.Clamp(_CurrentSlotIndex, 0, Mathf.Max(0, _SelectedUnits.Count - 1));
            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
                EnsureDraftBuild(_SelectedUnits[lIndex]);

            _HasPendingChanges = !MatchesPersistedPreset();
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
                _Character.Name.text = pUnit != null ? pUnit.DisplayName : "Empty";

            if (_Character.Description != null)
                _Character.Description.text = string.Empty;

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
            SetCoreValue(_ActionPointsValue, pUnit != null
                ? UnitStatAllocationRules.GetEffectiveValue(pUnit, lStats, UnitStatType.ActionPoints).ToString()
                : "-");
            SetCoreValue(_HealthValue, pUnit != null
                ? UnitStatAllocationRules.GetEffectiveValue(pUnit, lStats, UnitStatType.Health).ToString()
                : "-");
            SetCoreValue(_MoveRangeValue, pUnit != null
                ? UnitStatAllocationRules.GetEffectiveValue(pUnit, lStats, UnitStatType.Movement).ToString()
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
                    lSlot.Name.text = lHasPassive ? lPassive.DisplayName : "Empty";

                SetImage(lSlot.Icon, lHasPassive ? lPassive.Icon : null);
                SetBackground(lSlot.Background, lHasPassive ? _NormalCellColor : _DisabledCellColor);
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
                    lSlot.Name.text = lSkill != null ? lSkill.DisplayName : "Empty";

                if (lSlot.Meta != null)
                    lSlot.Meta.text = lSkill != null ? BuildShortSkillMeta(lSkill) : string.Empty;

                SetImage(lSlot.Icon, lSkill != null ? lSkill.Icon : null);
                SetBackground(lSlot.Background, lSkill != null ? _NormalCellColor : _DisabledCellColor);
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
            DraftUnitBuild lBuild = EnsureDraftBuild(pUnit);
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
            BindStat(4, "Initiative", pUnit != null ? pUnit.Initiative.ToString() : "-");
        }

        private bool CanChangeStat(
            UnitDefinition pUnit,
            UnitStatAllocationPreset pAllocations,
            UnitStatType pStat,
            int pDelta)
        {
            if (pUnit == null || pAllocations == null || pStat == UnitStatType.RemainingPoints)
                return false;

            UnitStatAllocationPreset lCandidate = pAllocations.Clone();
            int lValue = UnitStatAllocationRules.GetValue(lCandidate, pStat) + pDelta;
            if (lValue < 0)
                return false;

            UnitStatAllocationRules.SetValue(lCandidate, pStat, lValue);
            return UnitStatAllocationRules.TryValidate(pUnit, lCandidate, out _, out _);
        }

        private void ChangeStat(UnitStatType pStat, int pDelta)
        {
            UnitDefinition lUnit = CurrentUnit;
            DraftUnitBuild lBuild = EnsureDraftBuild(lUnit);
            if (lBuild == null || !CanChangeStat(lUnit, lBuild.StatAllocations, pStat, pDelta))
                return;

            int lValue = UnitStatAllocationRules.GetValue(lBuild.StatAllocations, pStat) + pDelta;
            UnitStatAllocationRules.SetValue(lBuild.StatAllocations, pStat, lValue);
            MarkDraftDirty();
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

            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();
            for (int lIndex = 0; lIndex < lAvailableUnits.Count; lIndex++)
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
                pItem.Name.text = pUnit != null ? pUnit.DisplayName : "Empty";

            if (pItem.State != null)
                pItem.State.text = lSelectedCurrent ? "Selected" : lUsedElsewhere ? "Already used" : string.Empty;

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
            if (pUnit == null || IsUnitSelectedInAnotherSlot(pUnit))
                return;

            if (_SelectedUnits.Count == 0)
                _SelectedUnits.Add(pUnit);
            else
                _SelectedUnits[_CurrentSlotIndex] = pUnit;

            EnsureDraftBuild(pUnit);
            MarkDraftDirty();
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

            DraftUnitBuild lBuild = EnsureDraftBuild(lUnit);
            if (lBuild == null || lBuild.Passive == lPassive)
                return;

            lBuild.Passive = lPassive;
            MarkDraftDirty();
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
                pItem.Name.text = pSkill != null ? pSkill.DisplayName : "Empty";

            if (pItem.Meta != null)
                pItem.Meta.text = pSkill != null ? BuildFullSkillMeta(pSkill) : string.Empty;

            if (pItem.State != null)
                pItem.State.text = lEquippedHere ? "Equipped" : lEquippedElsewhere ? "Already equipped" : string.Empty;

            SetImage(pItem.Icon, pSkill != null ? pSkill.Icon : null);
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

            DraftUnitBuild lBuild = EnsureDraftBuild(lUnit);
            if (lBuild == null)
                return;

            while (lBuild.Skills.Count < EquippedSkillCount)
                lBuild.Skills.Add(null);

            if (lBuild.Skills[lSlotIndex] == pSkill)
            {
                HideSkillPicker();
                return;
            }

            lBuild.Skills[lSlotIndex] = pSkill;
            MarkDraftDirty();
            HideSkillPicker();
            BindSkills(lUnit);
        }

        #endregion

        #region _____________________________| STATS PLACEHOLDER

        private void ShowStatsPlaceholder() => SetActive(_StatsEditPlaceholderPopup, true);
        private void HideStatsPlaceholder() => SetActive(_StatsEditPlaceholderPopup, false);

        #endregion

        #region _____________________________| BUILD STATE

        private DraftUnitBuild EnsureDraftBuild(UnitDefinition pUnit)
        {
            if (pUnit == null)
                return null;

            if (_DraftBuilds.TryGetValue(pUnit, out DraftUnitBuild lExisting))
                return lExisting;

            DraftUnitBuild lBuild = new DraftUnitBuild();
            UnitBuildPreset lPersistedBuild = FindPersistedBuild(pUnit.Id);
            lBuild.Passive = FindPassive(pUnit.Passives, lPersistedBuild?.PassiveId) ?? pUnit.DefaultPassive;
            UnitStatAllocationPreset lPersistedStats = lPersistedBuild?.StatAllocations;
            lBuild.StatAllocations = UnitStatAllocationRules.TryValidate(pUnit, lPersistedStats, out _, out _)
                ? lPersistedStats?.Clone() ?? new UnitStatAllocationPreset()
                : new UnitStatAllocationPreset();

            IReadOnlyList<string> lSkillIds = lPersistedBuild?.SkillIds;
            if (lSkillIds != null)
            {
                for (int lIndex = 0; lIndex < lSkillIds.Count && lBuild.Skills.Count < EquippedSkillCount; lIndex++)
                {
                    SkillDefinition lSkill = FindSkill(pUnit.Skills, lSkillIds[lIndex]);
                    if (lSkill != null && !lBuild.Skills.Contains(lSkill))
                        lBuild.Skills.Add(lSkill);
                }
            }

            FillDefaultSkills(pUnit, lBuild.Skills);
            while (lBuild.Skills.Count < EquippedSkillCount)
                lBuild.Skills.Add(null);

            _DraftBuilds.Add(pUnit, lBuild);
            return lBuild;
        }

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

            DraftUnitBuild lBuild = EnsureDraftBuild(pUnit);
            return lBuild != null
                ? new List<SkillDefinition>(lBuild.Skills)
                : new List<SkillDefinition>(EquippedSkillCount);
        }

        private static UnitBuildPreset FindPersistedBuild(string pUnitId)
        {
            IReadOnlyList<UnitBuildPreset> lSlots = TeamPresetState.ActivePreset?.Slots;
            if (lSlots == null || string.IsNullOrWhiteSpace(pUnitId))
                return null;

            for (int lIndex = 0; lIndex < lSlots.Count; lIndex++)
            {
                UnitBuildPreset lBuild = lSlots[lIndex];
                if (lBuild != null && string.Equals(lBuild.UnitId, pUnitId, StringComparison.Ordinal))
                    return lBuild;
            }

            return null;
        }

        private static PassiveDefinition FindPassive(IReadOnlyList<PassiveDefinition> pPassives, string pPassiveId)
        {
            if (pPassives == null || string.IsNullOrWhiteSpace(pPassiveId))
                return null;

            for (int lIndex = 0; lIndex < pPassives.Count; lIndex++)
            {
                PassiveDefinition lPassive = pPassives[lIndex];
                if (lPassive != null && string.Equals(lPassive.Id, pPassiveId, StringComparison.Ordinal))
                    return lPassive;
            }

            return null;
        }

        private static SkillDefinition FindSkill(IReadOnlyList<SkillDefinition> pSkills, string pSkillId)
        {
            if (pSkills == null || string.IsNullOrWhiteSpace(pSkillId))
                return null;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                if (lSkill != null && string.Equals(lSkill.Id, pSkillId, StringComparison.Ordinal))
                    return lSkill;
            }

            return null;
        }

        private static void FillDefaultSkills(UnitDefinition pUnit, ICollection<SkillDefinition> pTarget)
        {
            if (pUnit?.Skills == null || pTarget == null)
                return;

            for (int lIndex = 0; lIndex < pUnit.Skills.Count && pTarget.Count < EquippedSkillCount; lIndex++)
            {
                SkillDefinition lSkill = pUnit.Skills[lIndex];
                if (lSkill != null && !pTarget.Contains(lSkill))
                    pTarget.Add(lSkill);
            }
        }

        private static int CountAvailableSkills(IReadOnlyList<SkillDefinition> pSkills)
        {
            if (pSkills == null)
                return 0;

            HashSet<SkillDefinition> lSkills = new HashSet<SkillDefinition>();
            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                if (pSkills[lIndex] != null)
                    lSkills.Add(pSkills[lIndex]);
            }

            return lSkills.Count;
        }

        #endregion

        #region _____________________________| HELPERS

        private UnitDefinition CurrentUnit =>
            _CurrentSlotIndex >= 0 && _CurrentSlotIndex < _SelectedUnits.Count ? _SelectedUnits[_CurrentSlotIndex] : null;

        private void ValidateChanges()
        {
            if (!IsDraftValid())
            {
                Debug.LogWarning("Deckbuilding preset validation failed. The draft was not saved.", this);
                return;
            }

            if (!_HasPendingChanges)
            {
                HidePanel();
                return;
            }

            TeamSelectionState.SetSelectedUnits(_SelectedUnits);
            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _SelectedUnits[lIndex];
                DraftUnitBuild lBuild = EnsureDraftBuild(lUnit);
                if (lBuild == null)
                    continue;

                if (lBuild.Passive != null)
                    TeamPresetState.SetPassive(lUnit, lBuild.Passive);
                TeamPresetState.SetSkills(lUnit, lBuild.Skills);
                if (!TeamPresetState.TrySetStatAllocations(lUnit, lBuild.StatAllocations, out string lFailure))
                {
                    Debug.LogWarning($"Deckbuilding stat validation failed: {lFailure}", this);
                    return;
                }
            }

            TeamSelectionState.SaveSelectedUnits();
            _HasPendingChanges = false;
            TeamSelectionChanged?.Invoke();
            HidePanel();
        }

        private void MarkDraftDirty()
        {
            _HasPendingChanges = true;
            UpdateValidateButtonState();
        }

        private void UpdateValidateButtonState()
        {
            if (_ValidateButton != null)
                _ValidateButton.interactable = IsDraftValid();
        }

        private bool IsDraftValid()
        {
            if (_SelectedUnits.Count != _TeamSize)
                return false;

            HashSet<UnitDefinition> lUniqueUnits = new HashSet<UnitDefinition>();
            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _SelectedUnits[lIndex];
                DraftUnitBuild lBuild = EnsureDraftBuild(lUnit);
                if (lUnit == null || !lUniqueUnits.Add(lUnit) || lBuild == null)
                    return false;

                if (lUnit.Passives != null && lUnit.Passives.Count > 0 && lBuild.Passive == null)
                    return false;

                if (!UnitStatAllocationRules.TryValidate(lUnit, lBuild.StatAllocations, out _, out _))
                    return false;

                int lRequiredSkillCount = Mathf.Min(EquippedSkillCount, CountAvailableSkills(lUnit.Skills));
                int lEquippedSkillCount = 0;
                HashSet<SkillDefinition> lUniqueSkills = new HashSet<SkillDefinition>();
                for (int lSkillIndex = 0; lSkillIndex < lBuild.Skills.Count; lSkillIndex++)
                {
                    SkillDefinition lSkill = lBuild.Skills[lSkillIndex];
                    if (lSkill != null && lUniqueSkills.Add(lSkill))
                        lEquippedSkillCount++;
                }

                if (lEquippedSkillCount != lRequiredSkillCount)
                    return false;
            }

            return true;
        }

        private bool MatchesPersistedPreset()
        {
            TeamPreset lPreset = TeamPresetState.ActivePreset;
            if (lPreset?.Slots == null || lPreset.Slots.Count < _SelectedUnits.Count)
                return false;

            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _SelectedUnits[lIndex];
                UnitBuildPreset lPersistedBuild = lPreset.Slots[lIndex];
                DraftUnitBuild lDraftBuild = EnsureDraftBuild(lUnit);
                if (lPersistedBuild?.UnitId != lUnit?.Id || lDraftBuild == null)
                    return false;

                string lDraftPassiveId = lDraftBuild.Passive != null ? lDraftBuild.Passive.Id : string.Empty;
                if (!string.Equals(lPersistedBuild.PassiveId ?? string.Empty, lDraftPassiveId, StringComparison.Ordinal))
                    return false;

                List<string> lDraftSkillIds = new List<string>(EquippedSkillCount);
                for (int lSkillIndex = 0; lSkillIndex < lDraftBuild.Skills.Count; lSkillIndex++)
                {
                    SkillDefinition lSkill = lDraftBuild.Skills[lSkillIndex];
                    if (lSkill != null)
                        lDraftSkillIds.Add(lSkill.Id);
                }

                if (!HaveSameIds(lPersistedBuild.SkillIds, lDraftSkillIds))
                    return false;

                if (!HaveSameStatAllocations(lPersistedBuild.StatAllocations, lDraftBuild.StatAllocations))
                    return false;
            }

            return true;
        }

        private static bool HaveSameIds(IReadOnlyList<string> pLeft, IReadOnlyList<string> pRight)
        {
            int lLeftCount = pLeft?.Count ?? 0;
            int lRightCount = pRight?.Count ?? 0;
            if (lLeftCount != lRightCount)
                return false;

            for (int lIndex = 0; lIndex < lLeftCount; lIndex++)
            {
                if (!string.Equals(pLeft[lIndex], pRight[lIndex], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static bool HaveSameStatAllocations(
            UnitStatAllocationPreset pLeft,
            UnitStatAllocationPreset pRight)
        {
            pLeft ??= new UnitStatAllocationPreset();
            pRight ??= new UnitStatAllocationPreset();
            return pLeft.Health == pRight.Health
                && pLeft.Power == pRight.Power
                && pLeft.Movement == pRight.Movement
                && pLeft.MeleeDamage == pRight.MeleeDamage
                && pLeft.MeleeResistance == pRight.MeleeResistance
                && pLeft.RangedDamage == pRight.RangedDamage
                && pLeft.RangedResistance == pRight.RangedResistance
                && pLeft.Initiative == pRight.Initiative;
        }

        private List<UnitDefinition> GetAvailableUnits()
        {
            List<UnitDefinition> lUnits = new List<UnitDefinition>(_AvailableUnits.Count);
            for (int lIndex = 0; lIndex < _AvailableUnits.Count; lIndex++)
            {
                if (_AvailableUnits[lIndex] != null && !lUnits.Contains(_AvailableUnits[lIndex]))
                    lUnits.Add(_AvailableUnits[lIndex]);
            }

            return lUnits;
        }

        private bool TryLoadCurrentSelection(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            if (!TeamSelectionState.HasSelection && !TeamSelectionState.LoadSavedUnitIds())
                return false;

            if (TeamSelectionState.SelectedUnits.Count > 0)
            {
                for (int lIndex = 0; lIndex < TeamSelectionState.SelectedUnits.Count; lIndex++)
                {
                    UnitDefinition lUnit = TeamSelectionState.SelectedUnits[lIndex];
                    if (lUnit != null && ContainsUnit(pAvailableUnits, lUnit) && !pTarget.Contains(lUnit))
                        pTarget.Add(lUnit);
                }

                return pTarget.Count > 0;
            }

            return TryResolveSelection(TeamSelectionState.SelectedUnitIds, pAvailableUnits, pTarget);
        }

        private static bool TryResolveSelection(IReadOnlyList<string> pUnitIds, IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            if (pUnitIds == null || pAvailableUnits == null)
                return false;

            for (int lIdIndex = 0; lIdIndex < pUnitIds.Count; lIdIndex++)
            {
                UnitDefinition lUnit = FindAvailableUnitById(pAvailableUnits, pUnitIds[lIdIndex]);
                if (lUnit != null && !pTarget.Contains(lUnit))
                    pTarget.Add(lUnit);
            }

            return pTarget.Count > 0;
        }

        private static UnitDefinition FindAvailableUnitById(IReadOnlyList<UnitDefinition> pAvailableUnits, string pId)
        {
            if (string.IsNullOrWhiteSpace(pId) || pAvailableUnits == null)
                return null;

            for (int lIndex = 0; lIndex < pAvailableUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pAvailableUnits[lIndex];
                if (lUnit != null && string.Equals(lUnit.Id, pId, StringComparison.Ordinal))
                    return lUnit;
            }

            return null;
        }

        private static bool ContainsUnit(IReadOnlyList<UnitDefinition> pUnits, UnitDefinition pUnit)
        {
            if (pUnits == null || pUnit == null)
                return false;

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                if (pUnits[lIndex] == pUnit)
                    return true;
            }

            return false;
        }

        private void FillDefaultSelection(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            int lCount = Mathf.Min(_TeamSize, pAvailableUnits.Count);
            for (int lIndex = 0; lIndex < lCount; lIndex++)
                pTarget.Add(pAvailableUnits[lIndex]);
        }

        private void FillMissingSlots(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            if (pAvailableUnits == null || pAvailableUnits.Count == 0 || pTarget == null)
                return;

            int lTargetCount = Mathf.Min(_TeamSize, pAvailableUnits.Count);
            for (int lIndex = 0; pTarget.Count < lTargetCount && lIndex < pAvailableUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pAvailableUnits[lIndex];
                if (lUnit != null && !pTarget.Contains(lUnit))
                    pTarget.Add(lUnit);
            }

            if (pTarget.Count > _TeamSize)
                pTarget.RemoveRange(_TeamSize, pTarget.Count - _TeamSize);
        }

        private bool IsUnitSelectedInAnotherSlot(UnitDefinition pUnit)
        {
            if (pUnit == null)
                return false;

            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                if (lIndex != _CurrentSlotIndex && _SelectedUnits[lIndex] == pUnit)
                    return true;
            }

            return false;
        }

        private bool IsSkillEquippedAtPendingSlot(SkillDefinition pSkill)
        {
            List<SkillDefinition> lSkills = GetEquippedSkills(CurrentUnit);
            return pSkill != null && _PendingSkillSlotIndex >= 0 && _PendingSkillSlotIndex < lSkills.Count && lSkills[_PendingSkillSlotIndex] == pSkill;
        }

        private bool IsSkillEquippedInAnotherSlot(SkillDefinition pSkill)
        {
            List<SkillDefinition> lSkills = GetEquippedSkills(CurrentUnit);
            for (int lIndex = 0; lIndex < lSkills.Count; lIndex++)
            {
                if (lIndex != _PendingSkillSlotIndex && lSkills[lIndex] == pSkill)
                    return true;
            }

            return false;
        }

        private static string BuildShortSkillMeta(SkillDefinition pSkill) =>
            pSkill != null ? $"{pSkill.ActionPointCost} PA | {pSkill.RangeMin}-{pSkill.RangeMax}" : string.Empty;

        private static string BuildFullSkillMeta(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return string.Empty;

            string lMeta = $"{pSkill.ActionPointCost} PA | Range {pSkill.RangeMin}-{pSkill.RangeMax}";
            if (pSkill.RequiresLineOfSight)
                lMeta += " | LoS";
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

        private static void SetBackground(Image pImage, Color pColor)
        {
            if (pImage != null)
                pImage.color = pColor;
        }

        #endregion
    }
}
