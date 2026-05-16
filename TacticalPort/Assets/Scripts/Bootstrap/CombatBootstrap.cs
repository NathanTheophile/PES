#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.Combat;
using TacticalPort.UI;
using TacticalPort.View;
using UnityEngine;

namespace TacticalPort.Bootstrap
{
    public sealed class CombatBootstrap : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private BattleScenarioDefinition _Scenario;
        [SerializeField] private CombatSceneReferences _SceneReferences;
        [SerializeField] private bool _BootstrapOnStart = true;
        [SerializeField] private bool _UsePlacementPhase = true;
        [SerializeField] private bool _StartFirstTurnOnBootstrap = true;
        [SerializeField] private bool _AutoAdvanceTurns = true;
        [SerializeField] private bool _RefreshPresentationEachFrame;
        [SerializeField, Min(0f)] private float _EnemyTurnDelaySeconds = 0.15f;
        [Tooltip("Optional command sink used to route combat actions through networking. Leave empty for local solo execution.")]
        [SerializeField] private MonoBehaviour _CommandSinkBehaviour;

        private readonly Dictionary<UnitId, UnitView> _UnitViews = new Dictionary<UnitId, UnitView>();
        private readonly List<UnitRuntime> _VisibleUnitsScratch = new List<UnitRuntime>();
        private IBattleService _BattleService;
        private EnemyTurnController _EnemyTurnController;
        private ICombatCommandSink _CommandSink;
        private LocalCombatCommandSink _LocalCommandSink;
        private bool _IsBootstrapped;

        #endregion

        #region _____________________________/ ACCESSORS

        public IBattleService BattleService => _BattleService;
        public bool IsBootstrapped => _IsBootstrapped;
        public BattleTurnContext CurrentTurn => _BattleService != null ? _BattleService.CurrentTurn : null;
        public UnitRuntime ActiveUnit => _BattleService != null ? _BattleService.ActiveUnit : null;
        public HUDManager HudManager => _SceneReferences != null ? _SceneReferences.HudManager : null;
        public BoardCursorView BoardCursorView => _SceneReferences != null ? _SceneReferences.BoardCursorView : null;
        public bool IsPlacementPhaseActive => _BattleService != null && _BattleService.Phase == BattlePhase.Placement;
        public ICombatCommandSink CommandSink => ResolveCommandSink();
        public bool CanLocalPlayerControlTeam(Team pTeam) => CommandSink != null && CommandSink.CanLocallyControlTeam(pTeam);
        public bool CanLocalPlayerControlUnit(UnitRuntime pUnit) => pUnit != null && CanLocalPlayerControlTeam(pUnit.Team);
        public bool TryGetLocalPlayerSlot(out MatchPlayerSlot pSlot)
        {
            if (CommandSink != null && CommandSink.TryGetLocalPlayerSlot(out pSlot))
                return true;

            pSlot = MatchPlayerSlot.None;
            return false;
        }

        public CombatTeamRelation GetLocalTeamRelation(Team pTeam) =>
            CommandSink != null ? CommandSink.GetLocalRelation(pTeam) : CombatTeamRelation.Neutral;

        public CombatTeamRelation GetLocalTeamRelation(UnitRuntime pUnit) =>
            pUnit != null ? GetLocalTeamRelation(pUnit.Team) : CombatTeamRelation.Neutral;

        public bool TryGetPlacementSlotForTeam(Team pTeam, out MatchPlayerSlot pSlot)
        {
            if (CommandSink != null && CommandSink.TryGetSlotForTeam(pTeam, out pSlot))
                return true;

            pSlot = MatchPlayerSlot.None;
            return false;
        }

        #endregion

        #region _____________________________| UNITY

        private void Start()
        {
            if (_BootstrapOnStart)
                BootstrapCombat();
        }

        private void Update()
        {
            if (CanRunEnemyAi())
                _EnemyTurnController?.Tick(Time.deltaTime);

            if (_RefreshPresentationEachFrame && _IsBootstrapped)
                RefreshPresentation();
        }

        #endregion

        #region _____________________________| BOOTSTRAP

        public void BootstrapCombat()
        {
            if (_SceneReferences == null)
            {
                Debug.LogError("CombatBootstrap requires scene references.", this);
                return;
            }

            BattleScenarioDefinition lActiveScenario = ResolveScenario();
            if (lActiveScenario == null)
            {
                Debug.LogError("Combat bootstrap requires a scenario definition.", this);
                return;
            }

            if (!lActiveScenario.TryValidate(out string lFailureReason))
            {
                Debug.LogError($"Failed to bootstrap combat: {lFailureReason}", this);
                _SceneReferences?.HudManager?.SetStatus(lFailureReason);

                return;
            }

            _BattleService = BattleRuntimeCompositionRoot.CreateDefaultBattleService();
            _BattleService.Initialize(lActiveScenario);
            _EnemyTurnController = new EnemyTurnController(this, _EnemyTurnDelaySeconds);
            _LocalCommandSink = new LocalCombatCommandSink(this);
            _CommandSink = ResolveCommandSink();

            if (_SceneReferences != null)
            {
                _SceneReferences.BoardView?.Configure(lActiveScenario);
                _SceneReferences.HudManager?.Bind(_BattleService);
            }

            RebuildUnitViews(lActiveScenario);
            _IsBootstrapped = true;

            if (_UsePlacementPhase)
            {
                _BattleService.EnterPlacementPhase();
                _SceneReferences.HudManager?.SetStatus("Placement phase: select a player unit, choose an empty spawn cell, then press Ready.");
            }
            else if (_StartFirstTurnOnBootstrap)
            {
                TryAdvanceBattle();
            }
            else
            {
                _SceneReferences.HudManager?.SetStatus(ResolveBootstrapStatus(lActiveScenario));
            }

            RefreshPresentation();
        }

        #endregion

        #region _____________________________| ACTIONS

        public bool TryAdvanceBattle()
        {
            if (_BattleService == null || _BattleService.Outcome != BattleOutcome.None || _BattleService.CurrentTurn != null)
                return false;

            if (!_BattleService.TryStartNextTurn(out BattleTurnContext lTurnContext))
            {
                RefreshPresentation();
                return false;
            }

            if (_SceneReferences != null)
                _SceneReferences.HudManager?.SetStatus($"Turn started for {ResolveUnitLabel(lTurnContext.UnitId)}.");

            RefreshPresentation();
            return true;
        }

        public bool StartCombatFromPlacement()
        {
            if (!IsPlacementPhaseActive)
                return false;

            BattleActionResult lResult = CommandSink.Submit(BattleCommand.ReadyPlacement());
            ApplyCommandSinkResultFeedback(lResult);
            return lResult.IsSuccess;
        }

        public bool TryGetActiveUnit(out UnitRuntime pUnit) => (pUnit = ActiveUnit) != null;

        public IReadOnlyCollection<GridCoord> GetReachableCellsForActiveUnit() =>
            _BattleService != null && _BattleService.TryGetActiveUnit(out UnitRuntime lUnit)
                ? _BattleService.GetReachableCells(lUnit.Id)
                : System.Array.Empty<GridCoord>();

        public BattleActionResult ValidateActiveSkill(SkillId pSkillId, SkillTarget pTarget) =>
            _BattleService != null && _BattleService.TryGetActiveUnit(out UnitRuntime lUnit)
                ? _BattleService.ValidateSkill(lUnit.Id, pSkillId, pTarget)
                : BattleActionResult.Failed(BattleActionType.Skill, "No active unit is available.");

        public BattleActionResult MoveActiveUnit(GridCoord pDestination)
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return BattleActionResult.Failed(BattleActionType.Move, "No active unit is available.");

            BattleActionResult lResult = CommandSink.Submit(BattleCommand.Move(lUnit.Id, pDestination));
            ApplyCommandSinkResultFeedback(lResult);
            return lResult;
        }

        public BattleActionResult UseActiveUnitSkill(SkillId pSkillId, SkillTarget pTarget)
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return BattleActionResult.Failed(BattleActionType.Skill, "No active unit is available.");

            if (pTarget == null)
                return BattleActionResult.Failed(BattleActionType.Skill, "No skill target is available.");

            BattleActionResult lResult = CommandSink.Submit(BattleCommand.UseSkill(lUnit.Id, pSkillId, pTarget.Cell));
            ApplyCommandSinkResultFeedback(lResult);
            return lResult;
        }

        public BattleActionResult EndActiveTurn(string pStatusOverride = null)
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return BattleActionResult.Failed(BattleActionType.EndTurn, "No active unit is available.");

            BattleActionResult lResult = CommandSink.Submit(BattleCommand.EndTurn(lUnit.Id));
            ApplyCommandSinkResultFeedback(lResult);
            if (lResult.IsSuccess)
                ApplyStatusOverride(pStatusOverride);

            return lResult;
        }

        public BattleActionResult RepositionUnitDuringPlacement(UnitId pUnitId, GridCoord pDestination)
        {
            if (!TryValidatePlacementCommand(pUnitId, pDestination, true, out BattleActionResult lFailure))
                return lFailure;

            return CommandSink.Submit(BattleCommand.PlaceUnit(pUnitId, pDestination));
        }

        public bool TryValidatePlacementReadyForSlot(MatchPlayerSlot pSlot, out BattleActionResult pFailure)
        {
            pFailure = null;

            if (!IsPlacementPhaseActive)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Placement phase is not active.");
                return false;
            }

            if (_BattleService == null || _SceneReferences?.BoardView == null)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Placement is not ready.");
                return false;
            }

            if (pSlot == MatchPlayerSlot.None)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "No placement slot is assigned.");
                return false;
            }

            int lUnitCount = 0;
            foreach (UnitRuntime lUnit in _BattleService.Units)
            {
                if (lUnit == null || !lUnit.IsAlive)
                    continue;

                if (!TryGetPlacementSlotForTeam(lUnit.Team, out MatchPlayerSlot lSlot) || lSlot != pSlot)
                    continue;

                lUnitCount++;
                if (!_SceneReferences.BoardView.IsSpawnerCell(lUnit.Position, pSlot))
                {
                    pFailure = BattleActionResult.Failed(BattleActionType.Placement, $"{lUnit.Definition.DisplayName} must be placed on a valid {pSlot} spawn cell.");
                    return false;
                }
            }

            if (lUnitCount > 0)
                return true;

            pFailure = BattleActionResult.Failed(BattleActionType.Placement, $"No units are assigned to {pSlot}.");
            return false;
        }

        public BattleActionResult ExecuteLocalCommand(BattleCommand pCommand)
        {
            BattleActionResult lResult = ExecuteCommand(pCommand);
            ApplyActionFeedback(lResult);
            return lResult;
        }

        public void ApplyRemoteCommandResult(BattleActionResult pResult)
        {
            ApplyActionFeedback(pResult);
        }

        #endregion

        #region _____________________________| DISPLAY

        public void RefreshPresentation()
        {
            if (!_IsBootstrapped || _BattleService == null)
                return;

            SyncRuntimeUnitViews();
            BoardView lBoardView = _SceneReferences != null ? _SceneReferences.BoardView : null;
            lBoardView?.BeginCellStateUpdate();
            try
            {
                if (TryGetLocalPlayerSlot(out MatchPlayerSlot lLocalPlayerSlot))
                {
                    lBoardView?.SetLocalPlayerSlot(lLocalPlayerSlot);
                    _SceneReferences?.HudManager?.SetLocalPlayerSlot(lLocalPlayerSlot);
                }

                lBoardView?.SetSpawnerCellsVisible(IsPlacementPhaseActive, ResolveVisibleSpawnerSlot());
                lBoardView?.SetOccupiedCells(ResolveVisibleUnitsForPresentation(), _BattleService.ActiveUnit != null ? _BattleService.ActiveUnit.Id : UnitId.None);
                lBoardView?.SetGlyphCells(_BattleService.GetActiveGlyphs());
                lBoardView?.SetHazardCells(_BattleService.GetTelegraphedHazards());
            }
            finally
            {
                lBoardView?.EndCellStateUpdate();
            }

            foreach (UnitView lUnitView in _UnitViews.Values)
            {
                if (lUnitView == null)
                    continue;

                if (_BattleService.TryGetUnit(lUnitView.UnitId, out UnitRuntime lRuntimeUnit))
                    lUnitView.gameObject.SetActive(ShouldDisplayUnit(lRuntimeUnit));

                if (!lUnitView.gameObject.activeSelf)
                    continue;

                lUnitView.Refresh();
            }

            RefreshSelectionCursor();
            _SceneReferences?.HudManager?.Refresh();
        }

        #endregion

        #region _____________________________| HELPERS

        private BattleScenarioDefinition ResolveScenario() =>
            _SceneReferences != null ? _SceneReferences.ResolveScenario(_Scenario) : _Scenario;

        private ICombatCommandSink ResolveCommandSink()
        {
            if (_CommandSink != null)
                return _CommandSink;

            _LocalCommandSink ??= new LocalCombatCommandSink(this);
            if (_CommandSinkBehaviour != null)
            {
                if (_CommandSinkBehaviour is ICombatCommandSink lCommandSink)
                    return _CommandSink = lCommandSink;

                Debug.LogWarning($"Assigned command sink '{_CommandSinkBehaviour.name}' does not implement {nameof(ICombatCommandSink)}.", this);
            }

            return _CommandSink = _LocalCommandSink;
        }

        private bool CanRunAuthoritativeSimulation() => CommandSink == null || CommandSink.CanRunAuthoritativeSimulation;

        private bool CanRunEnemyAi() => CommandSink == null || CommandSink.CanRunEnemyAi;

        private void ApplyCommandSinkResultFeedback(BattleActionResult pResult)
        {
            if (pResult == null || CommandSink is LocalCombatCommandSink)
                return;

            if (CommandSink.UsesRemoteAuthority || !pResult.IsSuccess)
                ApplyActionFeedback(pResult);
        }

        private BattleActionResult ExecuteCommand(BattleCommand pCommand)
        {
            if (_BattleService == null)
                return BattleActionResult.Failed(pCommand.ActionType, "Battle service is not initialized.");

            switch (pCommand.Type)
            {
                case BattleCommandType.Move:
                    return AdvanceAfterImplicitTurnEnd(_BattleService.MoveUnit(pCommand.UnitId, pCommand.Cell));

                case BattleCommandType.UseSkill:
                    return AdvanceAfterImplicitTurnEnd(_BattleService.UseSkill(pCommand.UnitId, pCommand.SkillId, SkillTarget.ForCell(pCommand.Cell)));

                case BattleCommandType.EndTurn:
                    return ExecuteEndTurnCommand(pCommand.UnitId);

                case BattleCommandType.PlaceUnit:
                    return ExecutePlacementCommand(pCommand.UnitId, pCommand.Cell);

                case BattleCommandType.ReadyPlacement:
                    return ExecuteReadyPlacementCommand();

                default:
                    return BattleActionResult.Failed(pCommand.ActionType, "Unsupported battle command.");
            }
        }

        private BattleActionResult ExecuteEndTurnCommand(UnitId pUnitId)
        {
            BattleActionResult lResult = _BattleService.EndTurn(pUnitId);
            if (!lResult.IsSuccess || !_AutoAdvanceTurns)
                return lResult;

            bool lStartedNextTurn = TryAdvanceBattle();
            return lStartedNextTurn && _BattleService.CurrentTurn != null
                ? BattleActionResult.Succeeded(BattleActionType.EndTurn, $"Turn started for {ResolveUnitLabel(_BattleService.CurrentTurn.UnitId)}.")
                : lResult;
        }

        private BattleActionResult AdvanceAfterImplicitTurnEnd(BattleActionResult pResult)
        {
            if (pResult == null || !pResult.IsSuccess || !_AutoAdvanceTurns)
                return pResult;

            if (_BattleService == null
                || _BattleService.Outcome != BattleOutcome.None
                || _BattleService.CurrentTurn != null
                || _BattleService.Phase != BattlePhase.TurnEnd)
            {
                return pResult;
            }

            bool lStartedNextTurn = TryAdvanceBattle();
            return lStartedNextTurn && _BattleService.CurrentTurn != null
                ? BattleActionResult.Succeeded(
                    pResult.ActionType,
                    $"{pResult.Message} Turn started for {ResolveUnitLabel(_BattleService.CurrentTurn.UnitId)}.",
                    pResult.AffectedUnitIds,
                    pResult.TraversedPath)
                : pResult;
        }

        private BattleActionResult ExecuteReadyPlacementCommand()
        {
            if (!IsPlacementPhaseActive)
                return BattleActionResult.Failed(BattleActionType.Placement, "Placement phase is not active.");

            if (!TryValidatePlacementReadyForSlot(MatchPlayerSlot.TeamA, out BattleActionResult lTeamAFailure))
                return lTeamAFailure;

            if (!TryValidatePlacementReadyForSlot(MatchPlayerSlot.TeamB, out BattleActionResult lTeamBFailure))
                return lTeamBFailure;

            _SceneReferences?.HudManager?.SetStatus("Combat started.");
            bool lStartedFirstTurn = TryAdvanceBattle();
            return lStartedFirstTurn && _BattleService.CurrentTurn != null
                ? BattleActionResult.Succeeded(BattleActionType.Placement, $"Combat started. Turn started for {ResolveUnitLabel(_BattleService.CurrentTurn.UnitId)}.")
                : BattleActionResult.Failed(BattleActionType.Placement, "Combat could not start.");
        }

        private BattleActionResult ExecutePlacementCommand(UnitId pUnitId, GridCoord pDestination)
        {
            if (!TryValidatePlacementCommand(pUnitId, pDestination, false, out BattleActionResult lFailure))
                return lFailure;

            return _BattleService.RepositionUnitDuringPlacement(pUnitId, pDestination);
        }

        private bool TryValidatePlacementCommand(UnitId pUnitId, GridCoord pDestination, bool pRequireLocalControl, out BattleActionResult pFailure)
        {
            return CombatPlacementValidator.TryValidatePlacementCommand(
                _BattleService,
                _SceneReferences != null ? _SceneReferences.BoardView : null,
                CommandSink,
                pUnitId,
                pDestination,
                pRequireLocalControl,
                out pFailure);
        }

        private IReadOnlyCollection<UnitRuntime> ResolveVisibleUnitsForPresentation()
        {
            _VisibleUnitsScratch.Clear();
            if (_BattleService?.Units == null)
                return _VisibleUnitsScratch;

            foreach (UnitRuntime lUnit in _BattleService.Units)
            {
                if (ShouldDisplayUnit(lUnit))
                    _VisibleUnitsScratch.Add(lUnit);
            }

            return _VisibleUnitsScratch;
        }

        private bool ShouldDisplayUnit(UnitRuntime pUnit)
        {
            if (pUnit == null)
                return false;

            return !IsPlacementPhaseActive || GetLocalTeamRelation(pUnit) != CombatTeamRelation.Opponent;
        }

        private void RebuildUnitViews(BattleScenarioDefinition pActiveScenario)
        {
            ClearUnitViews();

            for (int lIndex = 0; lIndex < pActiveScenario.Units.Count; lIndex++)
            {
                UnitId lUnitId = new UnitId(lIndex + 1);
                if (!_BattleService.TryGetUnit(lUnitId, out var lRuntimeUnit))
                    continue;

                if (!pActiveScenario.TryGetSpawn(lUnitId, out UnitSpawnDefinition lSpawn) || lSpawn == null)
                    continue;

                UnitView lPrefab = ResolvePrefab(lSpawn.Unit);
                if (lPrefab == null)
                {
                    string lUnitLabel = lSpawn.Unit != null ? lSpawn.Unit.DisplayName : lUnitId.ToString();
                    Debug.LogError($"Cannot create UnitView for '{lUnitLabel}': no UnitView prefab is assigned on the unit definition or CombatSceneReferences.", this);
                    continue;
                }

                Transform lParent = _SceneReferences != null ? _SceneReferences.UnitRoot : transform;
                UnitView lInstance = Instantiate(lPrefab, lParent);
                lInstance.Bind(lRuntimeUnit, lSpawn.Unit, _SceneReferences != null ? _SceneReferences.BoardView : null);
                _UnitViews[lUnitId] = lInstance;
            }
        }

        private UnitView ResolvePrefab(UnitDefinition pUnitDefinition) =>
            pUnitDefinition != null && pUnitDefinition.UnitViewPrefab != null
                ? pUnitDefinition.UnitViewPrefab
                : _SceneReferences != null && _SceneReferences.DefaultUnitViewPrefab != null
                    ? _SceneReferences.DefaultUnitViewPrefab
                    : null;

        private void RefreshSelectionCursor()
        {
            BoardCursorView lBoardCursorView = BoardCursorView;
            if (lBoardCursorView == null)
                return;

            bool lCanShowSelection =
                _IsBootstrapped
                && _BattleService != null
                && _BattleService.Outcome == BattleOutcome.None
                && _BattleService.CurrentTurn != null
                && _BattleService.ActiveUnit != null;

            if (!lCanShowSelection)
            {
                lBoardCursorView.SetSelection(default, false);
                return;
            }

            lBoardCursorView.SetSelection(_BattleService.ActiveUnit.Position, true);
        }

        private void ClearUnitViews()
        {
            foreach (UnitView lUnitView in _UnitViews.Values)
            {
                if (lUnitView == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(lUnitView.gameObject);
                else
                    DestroyImmediate(lUnitView.gameObject);
            }

            _UnitViews.Clear();
        }

        private void SyncRuntimeUnitViews()
        {
            if (_BattleService == null)
                return;

            foreach (UnitRuntime lRuntimeUnit in _BattleService.Units)
            {
                if (lRuntimeUnit == null || _UnitViews.ContainsKey(lRuntimeUnit.Id))
                    continue;

                UnitView lPrefab = ResolvePrefab(lRuntimeUnit.Definition);
                if (lPrefab == null)
                    continue;

                Transform lParent = _SceneReferences != null ? _SceneReferences.UnitRoot : transform;
                UnitView lInstance = Instantiate(lPrefab, lParent);
                lInstance.Bind(lRuntimeUnit, lRuntimeUnit.Definition, _SceneReferences != null ? _SceneReferences.BoardView : null);
                _UnitViews[lRuntimeUnit.Id] = lInstance;
            }
        }

        private void ApplyActionFeedback(BattleActionResult pResult)
        {
            if (_SceneReferences != null && pResult != null)
                _SceneReferences.HudManager?.SetStatus(pResult.Message);

            RefreshPresentation();
        }

        private void ApplyStatusOverride(string pStatusOverride)
        {
            if (_SceneReferences == null || string.IsNullOrWhiteSpace(pStatusOverride))
                return;

            if (_BattleService != null && _BattleService.CurrentTurn != null)
            {
                _SceneReferences.HudManager?.SetStatus($"{pStatusOverride} Next turn: {ResolveUnitLabel(_BattleService.CurrentTurn.UnitId)}.");
                return;
            }

            _SceneReferences.HudManager?.SetStatus(pStatusOverride);
        }

        private string ResolveUnitLabel(UnitId pUnitId) =>
            _BattleService != null && _BattleService.TryGetUnit(pUnitId, out UnitRuntime lUnit)
                ? lUnit.Definition.DisplayName
                : pUnitId.ToString();

        private MatchPlayerSlot ResolveVisibleSpawnerSlot()
        {
            if (!IsPlacementPhaseActive || _BattleService == null)
                return MatchPlayerSlot.None;

            foreach (UnitRuntime lUnit in _BattleService.Units)
            {
                if (lUnit != null && lUnit.IsAlive && CanLocalPlayerControlUnit(lUnit) && TryGetPlacementSlotForTeam(lUnit.Team, out MatchPlayerSlot lSlot))
                    return lSlot;
            }

            return MatchPlayerSlot.None;
        }

        private string ResolveBootstrapStatus(BattleScenarioDefinition pScenario)
        {
            string lScenarioName = pScenario != null ? pScenario.DisplayName : "Unknown";

            if (_BattleService != null && _BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return $"Scenario '{lScenarioName}' initialized. Active turn: {lUnit.Definition.DisplayName}.";

            return $"Scenario '{lScenarioName}' initialized.";
        }

        #endregion
    }
}
