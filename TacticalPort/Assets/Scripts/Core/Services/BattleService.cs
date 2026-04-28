#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Core.Factories;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    public sealed class BattleService : IBattleService
    {
        #region _____________________________/ VALUES

        private readonly IGridService _GridService;
        private readonly IPathService _PathService;
        private readonly ITurnSystem _TurnSystem;
        private readonly ISkillExecutor _SkillExecutor;
        private readonly Dictionary<UnitId, UnitRuntime> _UnitsById = new Dictionary<UnitId, UnitRuntime>();
        private readonly List<TelegraphedHazardRuntime> _TelegraphedHazards = new List<TelegraphedHazardRuntime>();
        private readonly BattleUnitPlacementService _UnitPlacementService;

        #endregion

        public BattleService(
            IGridService pGridService,
            IPathService pPathService,
            ITurnSystem pTurnSystem,
            ISkillExecutor pSkillExecutor)
        {
            _GridService = pGridService ?? throw new ArgumentNullException(nameof(pGridService));
            _PathService = pPathService ?? throw new ArgumentNullException(nameof(pPathService));
            _TurnSystem = pTurnSystem ?? throw new ArgumentNullException(nameof(pTurnSystem));
            _SkillExecutor = pSkillExecutor ?? throw new ArgumentNullException(nameof(pSkillExecutor));
            _UnitPlacementService = new BattleUnitPlacementService(_GridService, _TurnSystem, _UnitsById);

            Phase = BattlePhase.Setup;
            Outcome = BattleOutcome.None;
        }

        #region _____________________________/ ACCESSORS

        public BattlePhase Phase { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public BattleTurnContext CurrentTurn => _TurnSystem.CurrentTurn;
        public UnitRuntime ActiveUnit => TryResolveActiveUnit(out UnitRuntime lUnit) ? lUnit : null;
        public bool HasActiveTurn => CurrentTurn != null && ActiveUnit != null;
        public IReadOnlyCollection<UnitRuntime> Units => _UnitsById.Values;
        public event Action<BattleTurnContext> TurnStarted;
        public event Action<UnitId> TurnEnded;
        public event Action<BattleActionResult> SkillUsed;
        public event Action<TelegraphedHazardRuntime> HazardScheduled;
        public event Action<BattleActionResult> HazardsResolved;

        #endregion

        #region _____________________________| SETUP

        public void Initialize(BattleScenarioDefinition pScenario)
        {
            if (pScenario == null)
                throw new ArgumentNullException(nameof(pScenario));

            if (!pScenario.TryValidate(out string lFailureReason))
                throw new InvalidOperationException(lFailureReason);

            _UnitsById.Clear();
            _TelegraphedHazards.Clear();
            _GridService.Initialize(pScenario);
            _UnitPlacementService.ResetNextUnitId(1);

            for (int lIndex = 0; lIndex < pScenario.Units.Count; lIndex++)
            {
                UnitId lUnitId = new UnitId(lIndex + 1);
                UnitRuntime lRuntime = UnitRuntimeFactory.Create(lUnitId, pScenario.Units[lIndex]);

                if (!_UnitPlacementService.TryPlaceInitialUnit(lRuntime))
                    throw new InvalidOperationException($"Unable to place unit {lUnitId} on {lRuntime.Position}.");

                _UnitsById[lUnitId] = lRuntime;
                _UnitPlacementService.TrackExistingUnitId(lUnitId);
            }

            RefreshAllPhaseStates();
            _TurnSystem.Initialize(_UnitsById.Values);
            Phase = BattlePhase.Setup;
            Outcome = BattleOutcome.None;
            EvaluateOutcome();
        }

        #endregion

        #region _____________________________| FLOW

        public bool TryStartNextTurn(out BattleTurnContext pTurnContext)
        {
            if (Outcome != BattleOutcome.None)
            {
                pTurnContext = null;
                return false;
            }

            if (!_TurnSystem.TryStartNextTurn(out pTurnContext))
            {
                EvaluateOutcome();
                return false;
            }

            ApplyStartTurnEffects();
            Phase = BattlePhase.AwaitingAction;
            TurnStarted?.Invoke(pTurnContext);
            return true;
        }

        public void EnterPlacementPhase()
        {
            if (Outcome != BattleOutcome.None || CurrentTurn != null)
                return;

            Phase = BattlePhase.Placement;
        }

        public BattleActionResult RepositionUnitDuringPlacement(UnitId pUnitId, GridCoord pDestination)
        {
            if (Outcome != BattleOutcome.None)
                return BattleActionResult.Failed(BattleActionType.Placement, "Battle is already complete.");

            if (CurrentTurn != null || Phase != BattlePhase.Placement)
                return BattleActionResult.Failed(BattleActionType.Placement, "Unit placement is not active.");

            if (!_UnitsById.TryGetValue(pUnitId, out UnitRuntime lUnit) || lUnit == null || !lUnit.IsAlive)
                return BattleActionResult.Failed(BattleActionType.Placement, "Unit is not available.");

            if (!_UnitPlacementService.TryRelocateUnit(lUnit, pDestination))
                return BattleActionResult.Failed(BattleActionType.Placement, "Target placement cell is not available.");

            return BattleActionResult.Succeeded(
                BattleActionType.Placement,
                $"{lUnit.Definition.DisplayName} repositioned.",
                new[] { pUnitId },
                new[] { pDestination });
        }

        public IReadOnlyCollection<GridCoord> GetReachableCells(UnitId pUnitId)
        {
            if (!_UnitsById.TryGetValue(pUnitId, out UnitRuntime lUnit) || !lUnit.IsAlive)
                return Array.Empty<GridCoord>();

            return _PathService.GetReachableCells(lUnit.Position, lUnit.RemainingMovement, pUnitId);
        }

        public BattleActionResult ValidateSkill(UnitId pUnitId, SkillId pSkillId, SkillTarget pTarget)
        {
            if (!TryResolveSkillRequest(pUnitId, pSkillId, pTarget, out _, out _, out _, out BattleActionResult lError))
                return lError;

            return lError;
        }

        public BattleActionResult MoveUnit(UnitId pUnitId, GridCoord pDestination)
        {
            if (!CanControlCurrentUnit(pUnitId, BattleActionType.Move, out UnitRuntime lUnit, out BattleActionResult lError))
                return lError;

            PathResult lPath = _PathService.FindPath(lUnit.Position, pDestination, lUnit.RemainingMovement, pUnitId);
            if (!lPath.IsSuccess)
                return BattleActionResult.Failed(BattleActionType.Move, lPath.FailureReason);

            if (!_UnitPlacementService.TryRelocateUnit(lUnit, pDestination))
                return BattleActionResult.Failed(BattleActionType.Move, "Grid rejected the movement.");

            if (!lUnit.TrySpendMovement(lPath.TotalCost))
                return BattleActionResult.Failed(BattleActionType.Move, "Unit does not have enough movement.");

            string lMessage = "Movement applied.";
            RefreshPhaseStates(new[] { pUnitId });
            CleanupDefeatedUnits();
            CompleteTurnIfActiveUnitIsGone();
            EvaluateOutcome();
            if (Outcome == BattleOutcome.None)
                Phase = CurrentTurn != null ? BattlePhase.AwaitingAction : BattlePhase.TurnEnd;
            return BattleActionResult.Succeeded(BattleActionType.Move, lMessage, new[] { pUnitId }, lPath.Steps);
        }

        public BattleActionResult UseSkill(UnitId pUnitId, SkillId pSkillId, SkillTarget pTarget)
        {
            if (!TryResolveSkillRequest(pUnitId, pSkillId, pTarget, out UnitRuntime lUnit, out SkillDefinition lSkill, out SkillExecutionContext lContext, out BattleActionResult lError))
                return lError;

            Phase = BattlePhase.ResolvingAction;
            BattleActionResult lResult = _SkillExecutor.Execute(lUnit, lSkill, pTarget, lContext);

            if (lResult.IsSuccess)
            {
                lUnit.TrySpendActionPoints(lSkill.ActionPointCost);
                RefreshPhaseStates(lResult.AffectedUnitIds);
                CleanupDefeatedUnits();
                CompleteTurnIfActiveUnitIsGone();
            }

            EvaluateOutcome();
            if (Outcome == BattleOutcome.None)
                Phase = CurrentTurn != null ? BattlePhase.AwaitingAction : BattlePhase.TurnEnd;

            SkillUsed?.Invoke(lResult);
            return lResult;
        }

        public BattleActionResult EndTurn(UnitId pUnitId)
        {
            if (CurrentTurn == null || CurrentTurn.UnitId != pUnitId)
                return BattleActionResult.Failed(BattleActionType.EndTurn, "Unit does not own the active turn.");

            CleanupDefeatedUnits();
            _TurnSystem.CompleteCurrentTurn();
            _GridService.AdvancePersistentEffects();
            ResolveTelegraphedHazards();
            EvaluateOutcome();

            if (Outcome == BattleOutcome.None)
                Phase = BattlePhase.TurnEnd;

            TurnEnded?.Invoke(pUnitId);
            return BattleActionResult.Succeeded(BattleActionType.EndTurn, "Turn completed.", new[] { pUnitId });
        }

        public bool IsUnitActive(UnitId pUnitId) => CurrentTurn != null && CurrentTurn.UnitId == pUnitId;
        public bool IsInside(GridCoord pCoordinate) => _GridService.IsInside(pCoordinate);
        public bool BlocksLineOfSight(GridCoord pCoordinate) => _GridService.BlocksLineOfSight(pCoordinate);
        public bool TryGetActiveUnit(out UnitRuntime pUnit) => TryResolveActiveUnit(out pUnit);
        public bool TryGetUnit(UnitId pUnitId, out UnitRuntime pUnit) => _UnitsById.TryGetValue(pUnitId, out pUnit);
        public IReadOnlyCollection<GridGlyphRuntime> GetActiveGlyphs() => _GridService.GetAllGlyphs();
        public IReadOnlyCollection<TelegraphedHazardRuntime> GetTelegraphedHazards() => _TelegraphedHazards.AsReadOnly();

        public bool TryScheduleHazard(TelegraphedHazardRuntime pHazard)
        {
            if (!BattleHazardResolver.TryScheduleHazard(_GridService, _TelegraphedHazards, pHazard))
                return false;

            HazardScheduled?.Invoke(pHazard);
            return true;
        }

        public BattleActionResult ResolveTelegraphedHazards()
        {
            if (_TelegraphedHazards.Count == 0)
                return BattleActionResult.Succeeded(BattleActionType.Hazard, "No telegraphed hazard resolved.");

            BattleActionResult lResult = BattleHazardResolver.ResolveTelegraphedHazards(
                _GridService,
                _UnitsById,
                _TelegraphedHazards,
                out bool lResolvedAny);

            CleanupDefeatedUnits();
            CompleteTurnIfActiveUnitIsGone();
            EvaluateOutcome();

            if (lResolvedAny)
                HazardsResolved?.Invoke(lResult);

            return lResult;
        }

        public bool TryApplyState(UnitId pUnitId, StateDefinition pState, int pStacks = 1, int pDurationTurns = -1)
        {
            if (!_UnitsById.TryGetValue(pUnitId, out UnitRuntime lUnit) || !lUnit.IsAlive)
                return false;

            return lUnit.TryApplyState(pState, pStacks, pDurationTurns);
        }

        public bool TryRemoveState(UnitId pUnitId, StateDefinition pState)
        {
            if (!_UnitsById.TryGetValue(pUnitId, out UnitRuntime lUnit))
                return false;

            return lUnit.RemoveTemporaryState(pState);
        }

        #endregion

        #region _____________________________| HELPERS

        private void RefreshAllPhaseStates()
        {
            BattlePhaseStateResolver.RefreshAll(_UnitsById.Values);
        }

        private void RefreshPhaseStates(IEnumerable<UnitId> pUnitIds) => BattlePhaseStateResolver.Refresh(_UnitsById, pUnitIds);

        private void RefreshPhaseStates(UnitRuntime pUnit) => BattlePhaseStateResolver.Refresh(pUnit);

        private bool CanControlCurrentUnit(
            UnitId pUnitId,
            BattleActionType pActionType,
            out UnitRuntime pUnit,
            out BattleActionResult pError)
        {
            if (Outcome != BattleOutcome.None)
            {
                pUnit = null;
                pError = BattleActionResult.Failed(pActionType, "Battle is already complete.");
                return false;
            }

            if (CurrentTurn == null || CurrentTurn.UnitId != pUnitId)
            {
                pUnit = null;
                pError = BattleActionResult.Failed(pActionType, "Unit does not own the active turn.");
                return false;
            }

            if (!_UnitsById.TryGetValue(pUnitId, out pUnit) || !pUnit.IsAlive)
            {
                pError = BattleActionResult.Failed(pActionType, "Unit is not available.");
                return false;
            }

            if (Phase != BattlePhase.AwaitingAction)
            {
                pError = BattleActionResult.Failed(pActionType, "Battle is not ready for a new action.");
                return false;
            }

            pError = null;
            return true;
        }

        private bool TryResolveActiveUnit(out UnitRuntime pUnit)
        {
            if (CurrentTurn == null)
            {
                pUnit = null;
                return false;
            }

            return _UnitsById.TryGetValue(CurrentTurn.UnitId, out pUnit) && pUnit != null && pUnit.IsAlive;
        }

        private bool TryResolveSkillRequest(
            UnitId pUnitId,
            SkillId pSkillId,
            SkillTarget pTarget,
            out UnitRuntime pUnit,
            out SkillDefinition pSkill,
            out SkillExecutionContext pContext,
            out BattleActionResult pError)
        {
            pContext = null;

            if (!CanControlCurrentUnit(pUnitId, BattleActionType.Skill, out pUnit, out pError))
            {
                pSkill = null;
                return false;
            }

            if (!pUnit.TryGetSkill(pSkillId, out pSkill))
            {
                pError = BattleActionResult.Failed(BattleActionType.Skill, $"Skill '{pSkillId}' is not available for unit {pUnitId}.");
                return false;
            }

            if (!pUnit.CanSpendActionPoints(pSkill.ActionPointCost))
            {
                pError = BattleActionResult.Failed(BattleActionType.Skill, "Unit does not have enough action points.");
                return false;
            }

            pContext = new SkillExecutionContext(
                _GridService,
                _UnitsById,
                _UnitPlacementService.TryRelocateUnit,
                _UnitPlacementService.TrySwapUnits,
                TrySummonUnit,
                pGlyph => _GridService.AddOrReplaceGlyph(pGlyph),
                TryScheduleHazard);
            pError = _SkillExecutor.Validate(pUnit, pSkill, pTarget, pContext);
            return pError.IsSuccess;
        }

        private UnitRuntime TrySummonUnit(UnitDefinition pDefinition, SkillSummonTeamRule pTeamRule, UnitRuntime pSummoner, GridCoord pDestination)
        {
            UnitRuntime lRuntime = _UnitPlacementService.TrySummonUnit(pDefinition, pTeamRule, pSummoner, pDestination);
            if (lRuntime != null)
                RefreshPhaseStates(lRuntime);

            return lRuntime;
        }

        private void ApplyStartTurnEffects()
        {
            if (!TryResolveActiveUnit(out UnitRuntime lActiveUnit) || lActiveUnit == null || !lActiveUnit.IsAlive)
                return;

            BattleHazardResolver.ApplyStartTurnGlyphs(_GridService, lActiveUnit);
            RefreshPhaseStates(new[] { lActiveUnit.Id });
            CleanupDefeatedUnits();
            CompleteTurnIfActiveUnitIsGone();
            EvaluateOutcome();
        }

        private void CleanupDefeatedUnits()
        {
            List<UnitRuntime> lDefeatedUnits = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in _UnitsById.Values)
            {
                if (lUnit != null && !lUnit.IsAlive)
                    lDefeatedUnits.Add(lUnit);
            }

            foreach (UnitRuntime lDefeatedUnit in lDefeatedUnits)
            {
                _GridService.RemoveUnit(lDefeatedUnit.Id);
                _TurnSystem.RemoveUnit(lDefeatedUnit.Id);
            }
        }

        private void CompleteTurnIfActiveUnitIsGone()
        {
            if (CurrentTurn == null)
                return;

            if (TryResolveActiveUnit(out _))
                return;

            _TurnSystem.CompleteCurrentTurn();
        }

        private void EvaluateOutcome()
        {
            Outcome = BattleOutcomeEvaluator.Evaluate(_UnitsById.Values);

            if (Outcome != BattleOutcome.None)
            {
                _TurnSystem.CompleteCurrentTurn();
                Phase = BattlePhase.Completed;
            }
        }

        #endregion
    }
}
