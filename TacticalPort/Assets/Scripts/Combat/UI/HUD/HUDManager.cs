#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using TacticalPort.Core;
using TacticalPort.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TacticalPort.Shared;

namespace TacticalPort.UI
{
    public sealed class HUDManager : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private RectTransform _HBoxSkills;
        [SerializeField] private TMP_Text _TxtCurrentPhase;
        [SerializeField] private TMP_Text _TxtOutcome;
        [SerializeField] private TMP_Text _TxtActiveUnit;
        [SerializeField] private TMP_Text _TxtCurrentTurn;
        [SerializeField] private TMP_Text _TxtStatus;
        [SerializeField] private TMP_Text _TxtMode;
        [SerializeField] private TMP_Text _HealthText;
        [SerializeField] private TMP_Text _MobilityText;
        [SerializeField] private TMP_Text _EnergyText;
        [SerializeField] private Button _BtnCancelSkill;
        [SerializeField] private Button _BtnEndTurn;
        [SerializeField] private TMP_Text _TxtEndTurnButton;
        [SerializeField] private SkillButtonView _SkillButtonPrefab;
        [SerializeField] private TimelineView _TimelineView;
        [SerializeField] private TimelineElementView _TimelineElementPrefab;
        [SerializeField] private UnitInfoBoxView _ActiveUnitInfobox;
        [SerializeField] private UnitInfoBoxView _HoverUnitInfobox;
        [SerializeField] private EndCombatView _EndCombatView;

        private IBattleService _BattleService;
        private HudSkillBarController _SkillBar;
        private HudUnitFeedbackController _UnitFeedback;
        private string _StatusMessage = string.Empty;
        private string _SkillModeMessage = "Mode: Move.";
        private bool _CanCancelSkill;
        private bool _IsEndTurnAvailable = true;
        private string _EndTurnButtonLabel = "End Turn";
        private Action _OnCancelSkillButtonClicked;
        private Action _OnEndTurnButtonClicked;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            LogMissingReferences();
            ConfigureButtons();
            CreateControllers();
            BindControllers();
            Refresh();
        }

        private void OnDestroy()
        {
            RemoveButtonHandler(_BtnCancelSkill, OnCancelSkillButtonClickedInternal);
            RemoveButtonHandler(_BtnEndTurn, OnEndTurnButtonClickedInternal);
            _SkillBar?.Clear();
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(IBattleService pBattleService)
        {
            _BattleService = pBattleService;
            CreateControllers();
            BindControllers();
            Refresh();
        }

        public void SetLocalPlayerSlot(MatchPlayerSlot pSlot)
        {
            if (pSlot != MatchPlayerSlot.None)
                _TimelineView?.SetLocalPlayerSlot(pSlot);
        }

        public void SetStatus(string pMessage)
        {
            string lMessage = pMessage ?? string.Empty;
            if (_StatusMessage == lMessage)
                return;

            _StatusMessage = lMessage;
            Refresh();
        }

        public void SetSkillMode(string pMessage)
        {
            string lMessage = string.IsNullOrWhiteSpace(pMessage) ? "Mode: Move." : pMessage;
            if (_SkillModeMessage == lMessage)
                return;

            _SkillModeMessage = lMessage;
            Refresh();
        }

        public void SetSkillState(
            bool pCanUseSkills,
            bool pCanCancelSkill,
            int pSelectedSkillSlotIndex,
            Func<UnitRuntime, bool> pCanDisplaySkills,
            Action<int> pOnSkillButtonClicked,
            Action pOnCancelSkillButtonClicked)
        {
            CreateControllers();
            _SkillBar?.SetState(pCanUseSkills, pSelectedSkillSlotIndex, pCanDisplaySkills, pOnSkillButtonClicked);

            if (_CanCancelSkill == pCanCancelSkill && _OnCancelSkillButtonClicked == pOnCancelSkillButtonClicked)
                return;

            _CanCancelSkill = pCanCancelSkill;
            _OnCancelSkillButtonClicked = pOnCancelSkillButtonClicked;
            RefreshActionButtons();
        }

        public void SetEndTurnAvailable(bool pValue, Action pOnEndTurnButtonClicked = null)
        {
            if (_IsEndTurnAvailable == pValue && _OnEndTurnButtonClicked == pOnEndTurnButtonClicked)
                return;

            _IsEndTurnAvailable = pValue;
            _OnEndTurnButtonClicked = pOnEndTurnButtonClicked;
            RefreshActionButtons();
        }

        public void SetEndTurnLabel(string pLabel)
        {
            string lLabel = string.IsNullOrWhiteSpace(pLabel) ? "End Turn" : pLabel;
            if (_EndTurnButtonLabel == lLabel)
                return;

            _EndTurnButtonLabel = lLabel;
            RefreshActionButtons();
        }

        public void SetMapHoveredUnit(UnitRuntime pUnit)
        {
            CreateControllers();
            _UnitFeedback?.SetMapHoveredUnit(pUnit);
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh()
        {
            CreateControllers();

            UnitRuntime lActiveUnit = null;
            if (_BattleService != null)
                _BattleService.TryGetActiveUnit(out lActiveUnit);

            string lPhase = _BattleService != null
                ? _BattleService.Phase.ToString()
                : GameLocalization.Get(GameLocalization.CombatLogTable, "battle.unbound", "Unbound");
            string lOutcome = _BattleService != null
                ? _BattleService.Outcome.ToString()
                : GameLocalization.Get(GameLocalization.CombatLogTable, "battle.no_outcome", "None");
            SetText(_TxtCurrentPhase, GameLocalization.Get(GameLocalization.UiTable, "hud.phase", "Phase: {0}", lPhase));
            SetText(_TxtOutcome, GameLocalization.Get(GameLocalization.UiTable, "hud.outcome", "Outcome: {0}", lOutcome));
            SetText(_TxtActiveUnit, GameLocalization.Get(GameLocalization.UiTable, "hud.active_unit", "Active Unit: {0}", ResolveActiveUnitLabel()));
            SetText(_TxtCurrentTurn, GameLocalization.Get(GameLocalization.UiTable, "hud.turn", "Turn: {0}", ResolveTurnLabel()));
            SetText(_TxtStatus, _StatusMessage);
            SetText(_TxtMode, _SkillModeMessage);
            SetUnitStats(lActiveUnit);
            _UnitFeedback?.Refresh(lActiveUnit);
            _SkillBar?.Refresh();
            RefreshActionButtons();
        }

        private string ResolveActiveUnitLabel()
        {
            if (_BattleService == null)
                return "None";

            BattleTurnContext lTurn = _BattleService.CurrentTurn;
            if (lTurn == null)
                return "None";

            return _BattleService.TryGetUnit(lTurn.UnitId, out UnitRuntime lUnit)
                ? lUnit.Definition.DisplayName
                : lTurn.UnitId.ToString();
        }

        private string ResolveTurnLabel() =>
            _BattleService?.CurrentTurn is BattleTurnContext lTurn
                ? $"Round {lTurn.RoundIndex} / Turn {lTurn.TurnIndex}"
                : "-";

        private void SetUnitStats(UnitRuntime pUnit)
        {
            if (pUnit == null)
            {
                SetText(_HealthText, GameLocalization.Get(GameLocalization.UiTable, "stats.health_empty", "Health: -"));
                SetText(_MobilityText, GameLocalization.Get(GameLocalization.UiTable, "stats.mobility_empty", "Mobility: -"));
                SetText(_EnergyText, GameLocalization.Get(GameLocalization.UiTable, "stats.energy_empty", "Energy: -"));
                return;
            }

            SetText(_HealthText, GameLocalization.Get(GameLocalization.UiTable, "stats.health_wear", "Health: {0}/{1} | Wear: {2}%",
                pUnit.CurrentHealth, pUnit.CurrentMaxHealth, pUnit.EffectiveWearPercent));
            SetText(_MobilityText, GameLocalization.Get(GameLocalization.UiTable, "stats.mobility", "Mobility: {0}/{1}",
                pUnit.RemainingMobility, Math.Max(0, pUnit.Definition.MobilityPerTurn + pUnit.GetMobilityModifier())));
            SetText(_EnergyText, GameLocalization.Get(GameLocalization.UiTable, "stats.energy", "Energy: {0}/{1}",
                pUnit.RemainingEnergy, Math.Max(0, pUnit.Definition.EnergyPerTurn + pUnit.GetEnergyModifier())));
        }

        #endregion

        #region _____________________________| HELPERS

        private void CreateControllers()
        {
            _SkillBar ??= new HudSkillBarController(_HBoxSkills, _SkillButtonPrefab);
            _UnitFeedback ??= new HudUnitFeedbackController(
                _TimelineView,
                _TimelineElementPrefab,
                _ActiveUnitInfobox,
                _HoverUnitInfobox,
                _EndCombatView);
        }

        private void BindControllers()
        {
            _SkillBar?.Bind(_BattleService);
            _UnitFeedback?.Bind(_BattleService);
        }

        private void ConfigureButtons()
        {
            AddButtonHandler(_BtnCancelSkill, OnCancelSkillButtonClickedInternal);
            AddButtonHandler(_BtnEndTurn, OnEndTurnButtonClickedInternal);
        }

        private void OnCancelSkillButtonClickedInternal() => _OnCancelSkillButtonClicked?.Invoke();
        private void OnEndTurnButtonClickedInternal() => _OnEndTurnButtonClicked?.Invoke();

        private void RefreshActionButtons()
        {
            SetButtonInteractable(_BtnCancelSkill, _CanCancelSkill);
            SetButtonInteractable(_BtnEndTurn, _IsEndTurnAvailable);
            SetText(_TxtEndTurnButton, _EndTurnButtonLabel);
        }

        private static void SetText(TMP_Text pTarget, string pValue)
        {
            if (pTarget != null)
                pTarget.text = pValue ?? string.Empty;
        }

        private static void SetButtonInteractable(Button pButton, bool pIsInteractable)
        {
            if (pButton != null)
                pButton.interactable = pIsInteractable;
        }

        private static void AddButtonHandler(Button pButton, Action pHandler)
        {
            if (pButton == null || pHandler == null)
                return;

            pButton.onClick.RemoveListener(pHandler.Invoke);
            pButton.onClick.AddListener(pHandler.Invoke);
        }

        private static void RemoveButtonHandler(Button pButton, Action pHandler)
        {
            if (pButton == null || pHandler == null)
                return;

            pButton.onClick.RemoveListener(pHandler.Invoke);
        }

        private void LogMissingReferences()
        {
            LogMissingReference(_HBoxSkills, nameof(_HBoxSkills));
            LogMissingReference(_TxtCurrentPhase, nameof(_TxtCurrentPhase));
            LogMissingReference(_TxtOutcome, nameof(_TxtOutcome));
            LogMissingReference(_TxtActiveUnit, nameof(_TxtActiveUnit));
            LogMissingReference(_TxtCurrentTurn, nameof(_TxtCurrentTurn));
            LogMissingReference(_TxtStatus, nameof(_TxtStatus));
            LogMissingReference(_TxtMode, nameof(_TxtMode));
            LogMissingReference(_HealthText, nameof(_HealthText));
            LogMissingReference(_MobilityText, nameof(_MobilityText));
            LogMissingReference(_EnergyText, nameof(_EnergyText));
            LogMissingReference(_BtnCancelSkill, nameof(_BtnCancelSkill));
            LogMissingReference(_BtnEndTurn, nameof(_BtnEndTurn));
            LogMissingReference(_TxtEndTurnButton, nameof(_TxtEndTurnButton));
            LogMissingReference(_SkillButtonPrefab, nameof(_SkillButtonPrefab));
            LogMissingReference(_TimelineView, nameof(_TimelineView));
            LogMissingReference(_ActiveUnitInfobox, nameof(_ActiveUnitInfobox));
            LogMissingReference(_HoverUnitInfobox, nameof(_HoverUnitInfobox));
            LogMissingReference(_EndCombatView, nameof(_EndCombatView));
        }

        private void LogMissingReference(UnityEngine.Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"HUDManager is missing reference '{pFieldName}'.", this);
        }

        #endregion
    }
}
