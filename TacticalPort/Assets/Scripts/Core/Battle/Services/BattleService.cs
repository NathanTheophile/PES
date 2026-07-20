#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
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
        public IReadOnlyList<UnitRuntime> TurnOrder => _TurnSystem.TurnOrder;
        public event Action<BattleTurnContext> TurnStarted;
        public event Action<UnitId> TurnEnded;
        public event Action<BattleActionResult> SkillUsed;
        public event Action<SkillResolutionReport> SkillResolved;
        public event Action<TelegraphedHazardRuntime> HazardScheduled;
        public event Action<BattleActionResult> HazardsResolved;

        #endregion

        #region _____________________________| SETUP

        public void Initialize(BattleScenarioDefinition pScenario, Team pPerfectVelocityTieStartingTeam = Team.TeamA)
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

            BattleServiceMaintenance.RefreshAllPhaseStates(_UnitsById.Values);
            RefreshPersistentPresenceGlyphs();
            _TurnSystem.Initialize(_UnitsById.Values, pPerfectVelocityTieStartingTeam);
            Phase = BattlePhase.Setup;
            Outcome = BattleOutcome.None;
        }

        #endregion

        #region _____________________________| FLOW

        public bool TryStartNextTurn(out BattleTurnContext pTurnContext)
        {
            EvaluateOutcome();
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
            if (TryResolveActiveUnit(out UnitRuntime lAutonomousUnit)
                && lAutonomousUnit.Definition.AutonomousBehavior != AutonomousUnitBehavior.None)
            {
                ResolveAutonomousTurn(lAutonomousUnit);
                if (CurrentTurn?.UnitId == lAutonomousUnit.Id)
                    EndTurn(lAutonomousUnit.Id);
            }
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

            RefreshPersistentPresenceGlyphs();

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

            return _PathService.GetReachableCells(lUnit.Position, lUnit.RemainingMobility, pUnitId);
        }

        public BattleActionResult ValidateSkill(UnitId pUnitId, SkillId pSkillId, SkillTarget pTarget)
        {
            TryResolveSkillRequest(pUnitId, pSkillId, pTarget, out _, out _, out _, out _, out BattleActionResult lError);
            return lError;
        }

        public BattleActionResult MoveUnit(UnitId pUnitId, GridCoord pDestination)
        {
            if (!CanControlCurrentUnit(pUnitId, BattleActionType.Move, out UnitRuntime lUnit, out BattleActionResult lError))
                return lError;

            PathResult lPath = _PathService.FindPath(lUnit.Position, pDestination, lUnit.RemainingMobility, pUnitId);
            if (!lPath.IsSuccess)
                return BattleActionResult.Failed(BattleActionType.Move, lPath.FailureReason);

            if (!_UnitPlacementService.TryRelocateUnit(lUnit, pDestination))
                return BattleActionResult.Failed(BattleActionType.Move, "Grid rejected the movement.");

            if (!lUnit.TrySpendMobility(lPath.TotalCost))
                return BattleActionResult.Failed(BattleActionType.Move, "Unit does not have enough movement.");

            string lMessage = "Movement applied.";
            BattleServiceMaintenance.RefreshPhaseStates(_UnitsById, new[] { pUnitId });
            BattleServiceMaintenance.CleanupDefeatedUnits(_UnitsById.Values, _GridService, _TurnSystem);
            RefreshPersistentPresenceGlyphs();
            BattleServiceMaintenance.CompleteTurnIfActiveUnitIsGone(_TurnSystem, _UnitsById);
            EvaluateOutcome();
            if (Outcome == BattleOutcome.None)
                Phase = CurrentTurn != null ? BattlePhase.AwaitingAction : BattlePhase.TurnEnd;
            return BattleActionResult.Succeeded(BattleActionType.Move, lMessage, new[] { pUnitId }, lPath.Steps);
        }

        public BattleActionResult UseSkill(UnitId pUnitId, SkillId pSkillId, SkillTarget pTarget)
        {
            if (!TryResolveSkillRequest(
                    pUnitId,
                    pSkillId,
                    pTarget,
                    out UnitRuntime lUnit,
                    out SkillDefinition lBaseSkill,
                    out SkillDefinition lEffectiveSkill,
                    out SkillExecutionContext lContext,
                    out BattleActionResult lError))
                return lError;

            Phase = BattlePhase.ResolvingAction;
            UnitRuntime lPrimaryTarget = TryResolveUnitAtCell(pTarget.Cell);
            Dictionary<UnitId, long> lHealthBefore = CaptureHealthByUnit();
            BattleActionResult lResult = _SkillExecutor.Execute(lUnit, lBaseSkill, lEffectiveSkill, pTarget, lContext);
            SkillResolutionReport lReport = null;

            if (lResult.IsSuccess)
            {
                lUnit.TrySpendEnergy(lEffectiveSkill.EnergyCost);
                ApplyDirectSkillTargetInteraction(lUnit, lEffectiveSkill, lPrimaryTarget);
                lReport = BuildSkillResolutionReport(lUnit.Id, lBaseSkill, lEffectiveSkill, lHealthBefore);
                if (lReport.UsedVariant && lBaseSkill.VariantTrigger == SkillVariantTrigger.StateEnabler)
                    lUnit.ConsumeSkillVariantEnablers();

                BattlePassiveResolver.Resolve(lUnit, lReport, _UnitsById.Values);
                BattleServiceMaintenance.RefreshPhaseStates(_UnitsById, lResult.AffectedUnitIds);
                BattleServiceMaintenance.CleanupDefeatedUnits(_UnitsById.Values, _GridService, _TurnSystem);
                RefreshPersistentPresenceGlyphs();
                BattleServiceMaintenance.CompleteTurnIfActiveUnitIsGone(_TurnSystem, _UnitsById);
            }

            EvaluateOutcome();
            if (Outcome == BattleOutcome.None)
                Phase = CurrentTurn != null ? BattlePhase.AwaitingAction : BattlePhase.TurnEnd;

            SkillUsed?.Invoke(lResult);
            if (lReport != null)
                SkillResolved?.Invoke(lReport);
            return lResult;
        }

        public BattleActionResult EndTurn(UnitId pUnitId)
        {
            if (CurrentTurn == null || CurrentTurn.UnitId != pUnitId)
                return BattleActionResult.Failed(BattleActionType.EndTurn, "Unit does not own the active turn.");

            if (_UnitsById.TryGetValue(pUnitId, out UnitRuntime lActiveUnit) && lActiveUnit != null && lActiveUnit.IsAlive)
                BattleHazardResolver.ApplyEndTurnGlyphs(_GridService, lActiveUnit);

            BattleServiceMaintenance.CleanupDefeatedUnits(_UnitsById.Values, _GridService, _TurnSystem);
            _TurnSystem.CompleteCurrentTurn();
            _GridService.AdvancePersistentEffects();
            RefreshPersistentPresenceGlyphs();
            ResolveTelegraphedHazards();
            EvaluateOutcome();

            if (Outcome == BattleOutcome.None)
                Phase = BattlePhase.TurnEnd;

            TurnEnded?.Invoke(pUnitId);
            return BattleActionResult.Succeeded(BattleActionType.EndTurn, "Turn completed.", new[] { pUnitId });
        }

        public bool IsUnitActive(UnitId pUnitId) => CurrentTurn != null && CurrentTurn.UnitId == pUnitId;
        public bool IsInside(GridCoord pCoordinate) => _GridService.IsInside(pCoordinate);
        public bool BlocksVisibility(GridCoord pCoordinate) => _GridService.BlocksVisibility(pCoordinate);
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

            BattleServiceMaintenance.CleanupDefeatedUnits(_UnitsById.Values, _GridService, _TurnSystem);
            RefreshPersistentPresenceGlyphs();
            BattleServiceMaintenance.CompleteTurnIfActiveUnitIsGone(_TurnSystem, _UnitsById);
            EvaluateOutcome();

            if (lResolvedAny)
                HazardsResolved?.Invoke(lResult);

            return lResult;
        }

        public bool TryApplyState(UnitId pUnitId, StateDefinition pState, int pStacks = 1, int pDurationTurns = -1) =>
            _UnitsById.TryGetValue(pUnitId, out UnitRuntime lUnit) && lUnit.IsAlive && lUnit.TryApplyState(pState, pStacks, pDurationTurns);

        public bool TryRemoveState(UnitId pUnitId, StateDefinition pState) =>
            _UnitsById.TryGetValue(pUnitId, out UnitRuntime lUnit) && lUnit.RemoveTemporaryState(pState);

        #endregion

        #region _____________________________| HELPERS

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
            out SkillDefinition pBaseSkill,
            out SkillDefinition pEffectiveSkill,
            out SkillExecutionContext pContext,
            out BattleActionResult pError)
        {
            pContext = null;

            if (!CanControlCurrentUnit(pUnitId, BattleActionType.Skill, out pUnit, out pError))
            {
                pBaseSkill = null;
                pEffectiveSkill = null;
                return false;
            }

            if (!pUnit.TryGetSkill(pSkillId, out pBaseSkill))
            {
                pEffectiveSkill = null;
                pError = BattleActionResult.Failed(BattleActionType.Skill, $"Skill '{pSkillId}' is not available for unit {pUnitId}.");
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
            UnitRuntime lPrimaryTarget = TryResolveUnitAtCell(pTarget.Cell);
            pEffectiveSkill = pUnit.ResolveEffectiveSkill(pBaseSkill, lPrimaryTarget);
            if (pBaseSkill.VariantTrigger == SkillVariantTrigger.PrimaryTargetOwnedState
                && pBaseSkill.VariantTargetRequiredState != null
                && lPrimaryTarget != null
                && lPrimaryTarget.HasState(pBaseSkill.VariantTargetRequiredState)
                && pEffectiveSkill == pBaseSkill)
            {
                pError = BattleActionResult.Failed(BattleActionType.Skill, "The targeted relay is not owned by the caster.");
                return false;
            }
            if (!pUnit.CanSpendEnergy(pEffectiveSkill.EnergyCost))
            {
                pError = BattleActionResult.Failed(BattleActionType.Skill, "Unit does not have enough Energy.");
                return false;
            }

            pError = _SkillExecutor.Validate(pUnit, pBaseSkill, pEffectiveSkill, pTarget, pContext);
            return pError.IsSuccess;
        }

        private Dictionary<UnitId, long> CaptureHealthByUnit()
        {
            Dictionary<UnitId, long> lHealthByUnit = new Dictionary<UnitId, long>(_UnitsById.Count);
            foreach (KeyValuePair<UnitId, UnitRuntime> lPair in _UnitsById)
            {
                if (lPair.Value != null)
                    lHealthByUnit[lPair.Key] = lPair.Value.CumulativeHealthLost;
            }

            return lHealthByUnit;
        }

        private SkillResolutionReport BuildSkillResolutionReport(
            UnitId pActorId,
            SkillDefinition pBaseSkill,
            SkillDefinition pEffectiveSkill,
            IReadOnlyDictionary<UnitId, long> pHealthBefore)
        {
            List<SkillHealthLoss> lHealthLosses = new List<SkillHealthLoss>();
            foreach (KeyValuePair<UnitId, long> lPair in pHealthBefore)
            {
                if (!_UnitsById.TryGetValue(lPair.Key, out UnitRuntime lUnit) || lUnit == null)
                    continue;

                long lHealthLostValue = Math.Max(0L, lUnit.CumulativeHealthLost - lPair.Value);
                int lHealthLost = lHealthLostValue > int.MaxValue ? int.MaxValue : (int)lHealthLostValue;
                if (lHealthLost > 0)
                    lHealthLosses.Add(new SkillHealthLoss(lPair.Key, lHealthLost));
            }

            lHealthLosses.Sort((pLeft, pRight) => pLeft.UnitId.Value.CompareTo(pRight.UnitId.Value));
            return new SkillResolutionReport(pActorId, pBaseSkill, pEffectiveSkill, lHealthLosses);
        }

        private UnitRuntime TrySummonUnit(UnitDefinition pDefinition, SkillSummonTeamRule pTeamRule, UnitRuntime pSummoner, GridCoord pDestination, bool pReplaceOwnedSameDefinition)
        {
            UnitRuntime lRuntime = _UnitPlacementService.TrySummonUnit(pDefinition, pTeamRule, pSummoner, pDestination, pReplaceOwnedSameDefinition);
            if (lRuntime != null)
                BattleServiceMaintenance.RefreshPhaseStates(lRuntime);

            return lRuntime;
        }

        private UnitRuntime TryResolveUnitAtCell(GridCoord pCell)
        {
            return _GridService.TryGetOccupant(pCell, out UnitId lUnitId)
                && _UnitsById.TryGetValue(lUnitId, out UnitRuntime lUnit)
                && lUnit != null
                && lUnit.IsAlive
                ? lUnit
                : null;
        }

        private void ApplyDirectSkillTargetInteraction(UnitRuntime pActor, SkillDefinition pSkill, UnitRuntime pTarget)
        {
            if (pActor == null
                || pSkill?.PrimaryEffectType != SkillPrimaryEffectType.Damage
                || pTarget == null
                || !pTarget.IsAlive
                || pTarget.Definition.DirectSkillDamage <= 0)
            {
                return;
            }

            StateDefinition lSwapState = pTarget.Definition.DirectSkillSwapRequiredState;
            if (lSwapState != null && pTarget.HasState(lSwapState))
                _UnitPlacementService.TrySwapUnits(pActor, pTarget);

            pTarget.ApplyDirectHealthLoss(pTarget.Definition.DirectSkillDamage);
        }

        private void ApplyStartTurnEffects()
        {
            if (!TryResolveActiveUnit(out UnitRuntime lActiveUnit) || lActiveUnit == null || !lActiveUnit.IsAlive)
                return;

            IReadOnlyList<HealthLossResolutionReport> lStateDamageReports = lActiveUnit.ResolveBeginTurnStateDamage();
            for (int lIndex = 0; lIndex < lStateDamageReports.Count; lIndex++)
                BattlePassiveResolver.ResolveObservedHealthLoss(lStateDamageReports[lIndex], _UnitsById.Values);
            BattleServiceMaintenance.TickTemporaryStateDurationsForTurnOwner(lActiveUnit, _UnitsById);
            if (lActiveUnit.IsAlive)
                BattlePassiveResolver.ResolveBeginTurn(lActiveUnit, _UnitsById.Values);
            BattleHazardResolver.ApplyStartTurnGlyphs(_GridService, lActiveUnit);
            BattleServiceMaintenance.RefreshPhaseStates(_UnitsById, new[] { lActiveUnit.Id });
            BattleServiceMaintenance.CleanupDefeatedUnits(_UnitsById.Values, _GridService, _TurnSystem);
            RefreshPersistentPresenceGlyphs();
            BattleServiceMaintenance.CompleteTurnIfActiveUnitIsGone(_TurnSystem, _UnitsById);
            EvaluateOutcome();
        }

        private void ResolveAutonomousTurn(UnitRuntime pUnit)
        {
            if (pUnit == null
                || !pUnit.IsAlive
                || (pUnit.Definition.AutonomousRequiredState != null && !pUnit.HasState(pUnit.Definition.AutonomousRequiredState)))
            {
                return;
            }

            switch (pUnit.Definition.AutonomousBehavior)
            {
                case AutonomousUnitBehavior.PullOrthogonalUnits:
                    ResolveAutonomousPull(pUnit);
                    break;

                case AutonomousUnitBehavior.UseSkillOnNearestEnemy:
                    ResolveAutonomousSkill(pUnit);
                    break;
            }
        }

        private void ResolveAutonomousPull(UnitRuntime pUnit)
        {
            List<UnitRuntime> lTargets = new List<UnitRuntime>();
            foreach (UnitRuntime lCandidate in _UnitsById.Values)
            {
                if (lCandidate == null)
                    continue;

                int lDistance = pUnit.Position.ManhattanDistanceTo(lCandidate.Position);
                if (lCandidate.IsAlive
                    && lCandidate.Id != pUnit.Id
                    && lDistance > 0
                    && lDistance <= pUnit.Definition.AutonomousRange
                    && (lCandidate.Position.X == pUnit.Position.X || lCandidate.Position.Y == pUnit.Position.Y)
                    && (pUnit.Definition.AutonomousExcludedTargetState == null || !lCandidate.HasState(pUnit.Definition.AutonomousExcludedTargetState)))
                {
                    lTargets.Add(lCandidate);
                }
            }

            lTargets.Sort((pLeft, pRight) =>
            {
                int lDistance = pUnit.Position.ManhattanDistanceTo(pLeft.Position)
                    .CompareTo(pUnit.Position.ManhattanDistanceTo(pRight.Position));
                return lDistance != 0 ? lDistance : pLeft.Id.Value.CompareTo(pRight.Id.Value);
            });

            SkillExecutionContext lContext = CreateSkillContext();
            for (int lIndex = 0; lIndex < lTargets.Count; lIndex++)
            {
                int lDistance = pUnit.Position.ManhattanDistanceTo(lTargets[lIndex].Position);
                SkillPullResolver.TryPullUnitToward(pUnit, lTargets[lIndex], Math.Max(0, lDistance - 1), lContext);
            }
        }

        private void ResolveAutonomousSkill(UnitRuntime pUnit)
        {
            SkillDefinition lSkill = pUnit.Definition.AutonomousSkill;
            if (lSkill == null)
                return;

            for (int lExecution = 0; lExecution < pUnit.Definition.AutonomousExecutionsPerTurn; lExecution++)
            {
                List<UnitRuntime> lTargets = ResolveOrderedEnemies(pUnit);
                bool lExecuted = false;
                for (int lIndex = 0; lIndex < lTargets.Count; lIndex++)
                {
                    UnitRuntime lTarget = lTargets[lIndex];
                    SkillTarget lSkillTarget = SkillTarget.ForCell(lTarget.Position);
                    SkillExecutionContext lContext = CreateSkillContext();
                    if (!_SkillExecutor.Validate(pUnit, lSkill, lSkill, lSkillTarget, lContext).IsSuccess)
                        continue;

                    Dictionary<UnitId, long> lHealthBefore = CaptureHealthByUnit();
                    BattleActionResult lResult = _SkillExecutor.Execute(pUnit, lSkill, lSkill, lSkillTarget, lContext);
                    if (!lResult.IsSuccess)
                        continue;

                    pUnit.TrySpendEnergy(lSkill.EnergyCost);
                    ApplyDirectSkillTargetInteraction(pUnit, lSkill, lTarget);
                    SkillResolutionReport lReport = BuildSkillResolutionReport(pUnit.Id, lSkill, lSkill, lHealthBefore);
                    SkillUsed?.Invoke(lResult);
                    SkillResolved?.Invoke(lReport);
                    BattleServiceMaintenance.CleanupDefeatedUnits(_UnitsById.Values, _GridService, _TurnSystem);
                    RefreshPersistentPresenceGlyphs();
                    lExecuted = true;
                    break;
                }

                if (!lExecuted)
                    break;
            }
        }

        private void RefreshPersistentPresenceGlyphs() =>
            BattleHazardResolver.RefreshPersistentPresenceGlyphs(_GridService, _UnitsById.Values);

        private List<UnitRuntime> ResolveOrderedEnemies(UnitRuntime pActor)
        {
            List<UnitRuntime> lTargets = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in _UnitsById.Values)
            {
                if (lUnit != null
                    && lUnit.IsAlive
                    && ((pActor.Team == Team.TeamA && lUnit.Team == Team.TeamB)
                        || (pActor.Team == Team.TeamB && lUnit.Team == Team.TeamA)))
                {
                    lTargets.Add(lUnit);
                }
            }

            lTargets.Sort((pLeft, pRight) =>
            {
                int lDistance = pActor.Position.ManhattanDistanceTo(pLeft.Position)
                    .CompareTo(pActor.Position.ManhattanDistanceTo(pRight.Position));
                return lDistance != 0 ? lDistance : pLeft.Id.Value.CompareTo(pRight.Id.Value);
            });
            return lTargets;
        }

        private SkillExecutionContext CreateSkillContext() => new SkillExecutionContext(
            _GridService,
            _UnitsById,
            _UnitPlacementService.TryRelocateUnit,
            _UnitPlacementService.TrySwapUnits,
            TrySummonUnit,
            pGlyph => _GridService.AddOrReplaceGlyph(pGlyph),
            TryScheduleHazard);

        private void EvaluateOutcome()
        {
            Outcome = BattleServiceMaintenance.EvaluateOutcome(_UnitsById.Values, _TurnSystem);

            if (Outcome != BattleOutcome.None)
                Phase = BattlePhase.Completed;
        }

        #endregion
    }
}
