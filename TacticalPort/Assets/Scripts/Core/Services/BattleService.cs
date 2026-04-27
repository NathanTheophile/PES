using System;
using System.Collections.Generic;
using System.Linq;
using TacticalPort.Core.Factories;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    public sealed class BattleService : IBattleService
    {
        #region _____________________________| VALUES

        private readonly IGridService _GridService;
        private readonly IPathService _PathService;
        private readonly ITurnSystem _TurnSystem;
        private readonly ISkillExecutor _SkillExecutor;
        private readonly Dictionary<UnitId, UnitRuntime> _UnitsById = new Dictionary<UnitId, UnitRuntime>();
        private int _NextUnitIdValue = 1;

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

            Phase = BattlePhase.Setup;
            Outcome = BattleOutcome.None;
        }

        #region _____________________________| ACCESSORS

        public BattlePhase Phase { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public BattleTurnContext CurrentTurn => _TurnSystem.CurrentTurn;
        public UnitRuntime ActiveUnit => TryResolveActiveUnit(out UnitRuntime lUnit) ? lUnit : null;
        public bool HasActiveTurn => CurrentTurn != null && ActiveUnit != null;
        public IReadOnlyCollection<UnitRuntime> Units => _UnitsById.Values;

        #endregion

        #region _____________________________| SETUP

        public void Initialize(BattleScenarioDefinition pScenario)
        {
            if (pScenario == null)
                throw new ArgumentNullException(nameof(pScenario));

            if (!pScenario.TryValidate(out string lFailureReason))
                throw new InvalidOperationException(lFailureReason);

            _UnitsById.Clear();
            _GridService.Initialize(pScenario);
            _NextUnitIdValue = 1;

            for (int lIndex = 0; lIndex < pScenario.Units.Count; lIndex++)
            {
                UnitId lUnitId = new UnitId(lIndex + 1);
                UnitRuntime lRuntime = UnitRuntimeFactory.Create(lUnitId, pScenario.Units[lIndex]);

                _GridService.RegisterUnitFootprint(lUnitId, lRuntime.OccupiedCellOffsets);
                if (!_GridService.TryPlaceUnit(lUnitId, lRuntime.Position))
                    throw new InvalidOperationException($"Unable to place unit {lUnitId} on {lRuntime.Position}.");

                _UnitsById[lUnitId] = lRuntime;
                _NextUnitIdValue = Math.Max(_NextUnitIdValue, lUnitId.Value + 1);
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
            return true;
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

            if (!TryRelocateUnit(lUnit, pDestination))
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

            return lResult;
        }

        public BattleActionResult EndTurn(UnitId pUnitId)
        {
            if (CurrentTurn == null || CurrentTurn.UnitId != pUnitId)
                return BattleActionResult.Failed(BattleActionType.EndTurn, "Unit does not own the active turn.");

            CleanupDefeatedUnits();
            _TurnSystem.CompleteCurrentTurn();
            _GridService.AdvancePersistentEffects();
            EvaluateOutcome();

            if (Outcome == BattleOutcome.None)
                Phase = BattlePhase.TurnEnd;

            return BattleActionResult.Succeeded(BattleActionType.EndTurn, "Turn completed.", new[] { pUnitId });
        }

        public bool IsUnitActive(UnitId pUnitId) => CurrentTurn != null && CurrentTurn.UnitId == pUnitId;
        public bool TryGetActiveUnit(out UnitRuntime pUnit) => TryResolveActiveUnit(out pUnit);
        public bool TryGetUnit(UnitId pUnitId, out UnitRuntime pUnit) => _UnitsById.TryGetValue(pUnitId, out pUnit);
        public IReadOnlyCollection<GridGlyphRuntime> GetActiveGlyphs() => _GridService.GetAllGlyphs();

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
            foreach (UnitRuntime lUnit in _UnitsById.Values)
                RefreshPhaseStates(lUnit);
        }

        private void RefreshPhaseStates(IEnumerable<UnitId> pUnitIds)
        {
            if (pUnitIds == null)
                return;

            HashSet<UnitId> lUniqueIds = new HashSet<UnitId>();
            foreach (UnitId lUnitId in pUnitIds)
            {
                if (!lUniqueIds.Add(lUnitId))
                    continue;

                if (_UnitsById.TryGetValue(lUnitId, out UnitRuntime lUnit))
                    RefreshPhaseStates(lUnit);
            }
        }

        private void RefreshPhaseStates(UnitRuntime pUnit)
        {
            if (pUnit?.Definition?.PhaseStates == null)
                return;

            for (int lIndex = 0; lIndex < pUnit.Definition.PhaseStates.Count; lIndex++)
            {
                UnitPhaseStateDefinition lPhaseState = pUnit.Definition.PhaseStates[lIndex];
                string lPhaseKey = $"phase::{lIndex}";

                if (ShouldPhaseStateBeActive(pUnit, lPhaseState))
                {
                    pUnit.SetPersistentState(lPhaseKey, lPhaseState.State, lPhaseState.Stacks);
                    continue;
                }

                pUnit.RemoveStateByKey(lPhaseKey);
            }
        }

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
                TryRelocateUnit,
                TrySwapUnits,
                TrySummonUnit,
                pGlyph => _GridService.AddOrReplaceGlyph(pGlyph));
            pError = _SkillExecutor.Validate(pUnit, pSkill, pTarget, pContext);
            return pError.IsSuccess;
        }

        private bool TryRelocateUnit(UnitRuntime pUnit, GridCoord pDestination)
        {
            if (pUnit == null || !pUnit.IsAlive)
                return false;

            GridCoord lOrigin = pUnit.Position;
            if (!_GridService.TryMoveUnit(pUnit.Id, pDestination))
                return false;

            pUnit.SetPosition(pDestination);
            pUnit.FaceDirection(new GridCoord(pDestination.X - lOrigin.X, pDestination.Y - lOrigin.Y));
            return true;
        }

        private bool TrySwapUnits(UnitRuntime pFirstUnit, UnitRuntime pSecondUnit)
        {
            if (pFirstUnit == null || pSecondUnit == null || !pFirstUnit.IsAlive || !pSecondUnit.IsAlive)
                return false;

            GridCoord lFirstPosition = pFirstUnit.Position;
            GridCoord lSecondPosition = pSecondUnit.Position;

            if (!_GridService.RemoveUnit(pFirstUnit.Id))
                return false;

            if (!_GridService.RemoveUnit(pSecondUnit.Id))
            {
                _GridService.TryPlaceUnit(pFirstUnit.Id, lFirstPosition);
                return false;
            }

            bool lPlacedSecond = _GridService.TryPlaceUnit(pSecondUnit.Id, lFirstPosition);
            bool lPlacedFirst = lPlacedSecond && _GridService.TryPlaceUnit(pFirstUnit.Id, lSecondPosition);
            if (!lPlacedFirst)
            {
                _GridService.RemoveUnit(pFirstUnit.Id);
                _GridService.RemoveUnit(pSecondUnit.Id);
                _GridService.TryPlaceUnit(pFirstUnit.Id, lFirstPosition);
                _GridService.TryPlaceUnit(pSecondUnit.Id, lSecondPosition);
                return false;
            }

            pFirstUnit.SetPosition(lSecondPosition);
            pSecondUnit.SetPosition(lFirstPosition);
            pFirstUnit.FaceTowards(lSecondPosition);
            pSecondUnit.FaceTowards(lFirstPosition);
            return true;
        }

        private UnitRuntime TrySummonUnit(UnitDefinition pDefinition, SkillSummonTeamRule pTeamRule, UnitRuntime pSummoner, GridCoord pDestination)
        {
            if (pDefinition == null || !_GridService.IsInside(pDestination))
                return null;

            Team? lTeamOverride = ResolveSummonTeamOverride(pDefinition, pTeamRule, pSummoner);
            UnitDefinition lRuntimeDefinition = lTeamOverride.HasValue && lTeamOverride.Value != pDefinition.Team
                ? UnitDefinition.CreateRuntimeClone(pDefinition, lTeamOverride)
                : pDefinition;

            UnitId lUnitId = new UnitId(_NextUnitIdValue++);
            UnitRuntime lRuntime = new UnitRuntime(lUnitId, lRuntimeDefinition, pDestination);
            _GridService.RegisterUnitFootprint(lUnitId, lRuntime.OccupiedCellOffsets);

            if (!_GridService.TryPlaceUnit(lUnitId, pDestination))
                return null;

            _UnitsById[lUnitId] = lRuntime;
            RefreshPhaseStates(lRuntime);
            _TurnSystem.AddUnit(lRuntime);
            return lRuntime;
        }

        private static Team? ResolveSummonTeamOverride(UnitDefinition pDefinition, SkillSummonTeamRule pTeamRule, UnitRuntime pSummoner)
        {
            switch (pTeamRule)
            {
                case SkillSummonTeamRule.Caster:
                    return pSummoner != null ? pSummoner.Team : pDefinition.Team;

                case SkillSummonTeamRule.EnemyOfCaster:
                    if (pSummoner == null)
                        return pDefinition.Team;

                    return pSummoner.Team == Team.Player ? Team.Enemy : Team.Player;

                default:
                    return pDefinition != null ? pDefinition.Team : Team.Neutral;
            }
        }

        private void ApplyStartTurnEffects()
        {
            if (!TryResolveActiveUnit(out UnitRuntime lActiveUnit) || lActiveUnit == null || !lActiveUnit.IsAlive)
                return;

            HashSet<string> lProcessedGlyphGroups = new HashSet<string>();
            foreach (GridCoord lCell in _GridService.GetOccupiedCells(lActiveUnit.Id))
            {
                foreach (GridGlyphRuntime lGlyph in _GridService.GetGlyphsAt(lCell))
                {
                    if (lGlyph == null || !lGlyph.CanAffect(lActiveUnit))
                        continue;

                    string lGlyphGroupId = string.IsNullOrWhiteSpace(lGlyph.GlyphGroupId)
                        ? $"{lGlyph.SourceSkillId}:{lGlyph.Cell.X}:{lGlyph.Cell.Y}"
                        : lGlyph.GlyphGroupId;
                    if (!lProcessedGlyphGroups.Add(lGlyphGroupId))
                        continue;

                    lActiveUnit.ApplyDamage(lGlyph.Power);
                }
            }

            RefreshPhaseStates(new[] { lActiveUnit.Id });
            CleanupDefeatedUnits();
            CompleteTurnIfActiveUnitIsGone();
            EvaluateOutcome();
        }

        private void CleanupDefeatedUnits()
        {
            foreach (UnitRuntime lDefeatedUnit in _UnitsById.Values.Where(unit => !unit.IsAlive).ToArray())
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
            bool lAnyPlayersAlive = _UnitsById.Values.Any(unit => unit.IsAlive && unit.Team == Team.Player);
            bool lAnyEnemiesAlive = _UnitsById.Values.Any(unit => unit.IsAlive && unit.Team == Team.Enemy);

            if (!lAnyPlayersAlive && !lAnyEnemiesAlive)
                Outcome = BattleOutcome.Draw;
            else if (!lAnyEnemiesAlive)
                Outcome = BattleOutcome.PlayerVictory;
            else if (!lAnyPlayersAlive)
                Outcome = BattleOutcome.EnemyVictory;
            else
                Outcome = BattleOutcome.None;

            if (Outcome != BattleOutcome.None)
            {
                _TurnSystem.CompleteCurrentTurn();
                Phase = BattlePhase.Completed;
            }
        }

        private static bool ShouldPhaseStateBeActive(UnitRuntime pUnit, UnitPhaseStateDefinition pPhaseState)
        {
            if (pUnit == null || pPhaseState?.State == null || !pUnit.IsAlive)
                return false;

            int lThresholdHealth = (int)Math.Ceiling(pUnit.Definition.MaxHealth * pPhaseState.HealthThresholdNormalized);
            return pUnit.CurrentHealth <= Math.Max(0, lThresholdHealth);
        }

        #endregion
    }
}
