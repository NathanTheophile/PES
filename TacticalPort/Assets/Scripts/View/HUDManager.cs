using System;
using System.Collections.Generic;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.View
{
    public sealed class HUDManager : MonoBehaviour
    {
        #region _____________________________| VALUES

        [SerializeField] private RectTransform _HBoxSpells;
        [SerializeField] private TMP_Text _TxtCurrentPhase;
        [SerializeField] private TMP_Text _TxtOutcome;
        [SerializeField] private TMP_Text _TxtActiveUnit;
        [SerializeField] private TMP_Text _TxtCurrentTurn;
        [SerializeField] private TMP_Text _TxtStatus;
        [SerializeField] private TMP_Text _TxtMode;
        [SerializeField] private TMP_Text _TxtHP;
        [SerializeField] private TMP_Text _TxtMP;
        [SerializeField] private TMP_Text _TxtAP;
        [SerializeField] private Button _BtnCancelSkill;
        [SerializeField] private Button _BtnEndTurn;
        [SerializeField] private SkillButtonView _SkillButtonPrefab;

        private readonly List<SkillButtonView> _RuntimeSkillButtons = new List<SkillButtonView>();
        private readonly List<SkillDefinition> _DisplayedSkills = new List<SkillDefinition>();
        private IBattleService _BattleService;
        private string _StatusMessage = string.Empty;
        private string _SkillModeMessage = "Mode: Move.";
        private bool _CanUseSkills;
        private bool _CanCancelSkill;
        private bool _IsEndTurnAvailable = true;
        private Action<int> _OnSkillButtonClicked;
        private Action _OnCancelSkillButtonClicked;
        private Action _OnEndTurnButtonClicked;
        private UnitId _DisplayedSkillUnitId = UnitId.None;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ValidateReferences();
            ConfigureButtons();
            Refresh();
        }

        private void OnDestroy()
        {
            RemoveButtonHandler(_BtnCancelSkill, OnCancelSkillButtonClickedInternal);
            RemoveButtonHandler(_BtnEndTurn, OnEndTurnButtonClickedInternal);
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(IBattleService pBattleService)
        {
            _BattleService = pBattleService;
            Refresh();
        }

        public void SetStatus(string pMessage)
        {
            _StatusMessage = pMessage ?? string.Empty;
            Refresh();
        }

        public void SetSkillMode(string pMessage)
        {
            _SkillModeMessage = string.IsNullOrWhiteSpace(pMessage) ? "Mode: Move." : pMessage;
            Refresh();
        }

        public void SetSkillState(
            bool pCanUseSkills,
            bool pCanCancelSkill,
            Action<int> pOnSkillButtonClicked,
            Action pOnCancelSkillButtonClicked)
        {
            _CanUseSkills = pCanUseSkills;
            _CanCancelSkill = pCanCancelSkill;
            _OnSkillButtonClicked = pOnSkillButtonClicked;
            _OnCancelSkillButtonClicked = pOnCancelSkillButtonClicked;

            RefreshSkillArea();
        }

        public void SetEndTurnAvailable(bool pValue, Action pOnEndTurnButtonClicked = null)
        {
            _IsEndTurnAvailable = pValue;
            _OnEndTurnButtonClicked = pOnEndTurnButtonClicked;
            RefreshActionButtons();
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh()
        {
            UnitRuntime lActiveUnit = null;
            if (_BattleService != null)
                _BattleService.TryGetActiveUnit(out lActiveUnit);

            SetText(_TxtCurrentPhase, _BattleService != null ? $"Phase: {_BattleService.Phase}" : "Phase: Unbound");
            SetText(_TxtOutcome, _BattleService != null ? $"Outcome: {_BattleService.Outcome}" : "Outcome: None");
            SetText(_TxtActiveUnit, $"Active Unit: {ResolveActiveUnitLabel()}");
            SetText(_TxtCurrentTurn, $"Turn: {ResolveTurnLabel()}");
            SetText(_TxtStatus, _StatusMessage);
            SetText(_TxtMode, _SkillModeMessage);
            SetUnitStats(lActiveUnit);
            RefreshSkillArea();
            RefreshActionButtons();
        }

        private string ResolveActiveUnitLabel()
        {
            if (_BattleService == null)
                return "None";

            BattleTurnContext lTurn = _BattleService.CurrentTurn;
            if (lTurn == null)
                return "None";

            return _BattleService.TryGetUnit(lTurn.UnitId, out var lUnit)
                ? lUnit.Definition.DisplayName
                : lTurn.UnitId.ToString();
        }

        private string ResolveTurnLabel()
        {
            if (_BattleService == null)
                return "-";

            BattleTurnContext lTurn = _BattleService.CurrentTurn;
            if (lTurn == null)
                return "-";

            return $"Round {lTurn.RoundIndex} / Turn {lTurn.TurnIndex}";
        }

        private void SetUnitStats(UnitRuntime pUnit)
        {
            if (pUnit == null)
            {
                SetText(_TxtHP, "HP: -");
                SetText(_TxtMP, "MP: -");
                SetText(_TxtAP, "AP: -");
                return;
            }

            SetText(_TxtHP, $"HP: {pUnit.CurrentHealth}/{pUnit.Definition.MaxHealth}");
            SetText(_TxtMP, $"MP: {pUnit.RemainingMovement}/{pUnit.Definition.MoveRange}");
            SetText(_TxtAP, $"AP: {pUnit.RemainingActionPoints}/{pUnit.Definition.ActionPointsPerTurn}");
        }

        private void RefreshSkillArea()
        {
            if (!TryResolveDisplayedSkills(out UnitRuntime lActiveUnit, out IReadOnlyList<SkillDefinition> lSkills))
            {
                SetSkillBarVisible(false);
                ClearRuntimeSkillButtons();
                _DisplayedSkillUnitId = UnitId.None;
                _DisplayedSkills.Clear();
                RefreshActionButtons();
                return;
            }

            SetSkillBarVisible(true);

            if (ShouldRebuildSkillButtons(lActiveUnit, lSkills))
                RebuildSkillButtons(lActiveUnit, lSkills);

            RefreshRuntimeSkillButtons();
            RefreshActionButtons();
        }

        private bool TryResolveDisplayedSkills(out UnitRuntime pUnit, out IReadOnlyList<SkillDefinition> pSkills)
        {
            pUnit = null;
            pSkills = null;

            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out pUnit) || pUnit == null)
                return false;

            if (pUnit.Team != Team.Player || pUnit.Skills == null || pUnit.Skills.Count == 0)
                return false;

            pSkills = pUnit.Skills;
            return true;
        }

        private bool ShouldRebuildSkillButtons(UnitRuntime pUnit, IReadOnlyList<SkillDefinition> pSkills)
        {
            if (_DisplayedSkillUnitId != pUnit.Id)
                return true;

            if (_DisplayedSkills.Count != pSkills.Count || _RuntimeSkillButtons.Count != pSkills.Count)
                return true;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                if (_RuntimeSkillButtons[lIndex] == null || _DisplayedSkills[lIndex] != pSkills[lIndex])
                    return true;
            }

            return false;
        }

        private void RebuildSkillButtons(UnitRuntime pUnit, IReadOnlyList<SkillDefinition> pSkills)
        {
            ClearRuntimeSkillButtons();
            _DisplayedSkills.Clear();
            _DisplayedSkillUnitId = pUnit.Id;

            if (_HBoxSpells == null || _SkillButtonPrefab == null)
                return;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                int lCapturedIndex = lIndex;
                SkillButtonView lButtonView = Instantiate(_SkillButtonPrefab, _HBoxSpells);
                lButtonView.Bind(lSkill, () => _OnSkillButtonClicked?.Invoke(lCapturedIndex));
                lButtonView.SetInteractable(_CanUseSkills);
                _RuntimeSkillButtons.Add(lButtonView);
                _DisplayedSkills.Add(lSkill);
            }
        }

        private void RefreshRuntimeSkillButtons()
        {
            UnitRuntime lActiveUnit = null;
            if (_BattleService != null)
                _BattleService.TryGetActiveUnit(out lActiveUnit);

            for (int lIndex = 0; lIndex < _RuntimeSkillButtons.Count; lIndex++)
            {
                SkillButtonView lButtonView = _RuntimeSkillButtons[lIndex];
                if (lButtonView == null)
                    continue;

                SkillDefinition lSkill = lIndex >= 0 && lIndex < _DisplayedSkills.Count
                    ? _DisplayedSkills[lIndex]
                    : null;

                lButtonView.SetInteractable(IsSkillInteractable(lActiveUnit, lSkill));
            }
        }

        #endregion

        #region _____________________________| HELPERS

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
        }

        private bool IsSkillInteractable(UnitRuntime pUnit, SkillDefinition pSkill)
        {
            if (!_CanUseSkills || pUnit == null || pSkill == null)
                return false;

            if (!pUnit.CanSpendActionPoints(pSkill.ActionPointCost))
                return false;

            if (pUnit.GetRemainingCooldown(pSkill) > 0)
                return false;

            return pSkill.UsePerTurn <= 0 || pUnit.GetSkillUsesThisTurn(pSkill) < pSkill.UsePerTurn;
        }

        private void SetSkillBarVisible(bool pIsVisible)
        {
            if (_HBoxSpells != null && _HBoxSpells.gameObject.activeSelf != pIsVisible)
                _HBoxSpells.gameObject.SetActive(pIsVisible);
        }

        private void ClearRuntimeSkillButtons()
        {
            for (int lIndex = 0; lIndex < _RuntimeSkillButtons.Count; lIndex++)
            {
                SkillButtonView lButtonView = _RuntimeSkillButtons[lIndex];
                if (lButtonView == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(lButtonView.gameObject);
                else
                    DestroyImmediate(lButtonView.gameObject);
            }

            _RuntimeSkillButtons.Clear();
        }

        private static void SetText(TMP_Text pTarget, string pValue)
        {
            if (pTarget != null)
                pTarget.text = pValue ?? string.Empty;
        }

        private static void SetButtonInteractable(Button pButton, bool pIsInteractable)
        {
            if (pButton == null)
                return;

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

        private void ValidateReferences()
        {
            LogMissingReference(_HBoxSpells, nameof(_HBoxSpells));
            LogMissingReference(_TxtCurrentPhase, nameof(_TxtCurrentPhase));
            LogMissingReference(_TxtOutcome, nameof(_TxtOutcome));
            LogMissingReference(_TxtActiveUnit, nameof(_TxtActiveUnit));
            LogMissingReference(_TxtCurrentTurn, nameof(_TxtCurrentTurn));
            LogMissingReference(_TxtStatus, nameof(_TxtStatus));
            LogMissingReference(_TxtMode, nameof(_TxtMode));
            LogMissingReference(_TxtHP, nameof(_TxtHP));
            LogMissingReference(_TxtMP, nameof(_TxtMP));
            LogMissingReference(_TxtAP, nameof(_TxtAP));
            LogMissingReference(_BtnCancelSkill, nameof(_BtnCancelSkill));
            LogMissingReference(_BtnEndTurn, nameof(_BtnEndTurn));
            LogMissingReference(_SkillButtonPrefab, nameof(_SkillButtonPrefab));
        }

        private void LogMissingReference(UnityEngine.Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"HUDManager is missing reference '{pFieldName}'.", this);
        }

        #endregion
    }
}
