#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Bootstrap
#endregion

using TacticalPort.Combat;
using TacticalPort.Core;
using TacticalPort.Shared;
using TacticalPort.View;

namespace TacticalPort.Bootstrap
{
    internal interface ICombatCommandExecutionContext
    {
        IBattleService BattleService { get; }
        BoardView BoardView { get; }
        ICombatCommandSink CommandSink { get; }
        bool AutoAdvanceTurns { get; }
        bool IsPlacementPhaseActive { get; }

        bool TryAdvanceBattle();
        bool TryValidatePlacementReadyForSlot(MatchPlayerSlot pSlot, out BattleActionResult pFailure);
        string ResolveUnitLabel(UnitId pUnitId);
        void SetStatus(string pStatus);
    }

    internal sealed class CombatCommandExecutor
    {
        #region _____________________________/ VALUES

        private readonly ICombatCommandExecutionContext _Context;

        #endregion

        #region _____________________________| INIT

        public CombatCommandExecutor(ICombatCommandExecutionContext pContext)
        {
            _Context = pContext;
        }

        #endregion

        #region _____________________________| EXECUTE

        public BattleActionResult Execute(BattleCommand pCommand)
        {
            if (_Context.BattleService == null)
                return BattleActionResult.Failed(pCommand.ActionType, "Battle service is not initialized.");

            switch (pCommand.Type)
            {
                case BattleCommandType.Move:
                    return AdvanceAfterImplicitTurnEnd(_Context.BattleService.MoveUnit(pCommand.UnitId, pCommand.Cell));

                case BattleCommandType.UseSkill:
                    return AdvanceAfterImplicitTurnEnd(_Context.BattleService.UseSkill(pCommand.UnitId, pCommand.SkillId, SkillTarget.ForCell(pCommand.Cell)));

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

        public BattleActionResult SubmitPlacementCommand(UnitId pUnitId, GridCoord pDestination, bool pRequireLocalControl)
        {
            if (!TryValidatePlacementCommand(pUnitId, pDestination, pRequireLocalControl, out BattleActionResult lFailure))
                return lFailure;

            return _Context.CommandSink.Submit(BattleCommand.PlaceUnit(pUnitId, pDestination));
        }

        #endregion

        #region _____________________________| HELPERS

        private BattleActionResult ExecuteEndTurnCommand(UnitId pUnitId)
        {
            BattleActionResult lResult = _Context.BattleService.EndTurn(pUnitId);
            if (!lResult.IsSuccess || !_Context.AutoAdvanceTurns)
                return lResult;

            bool lStartedNextTurn = _Context.TryAdvanceBattle();
            return lStartedNextTurn && _Context.BattleService.CurrentTurn != null
                ? BattleActionResult.Succeeded(BattleActionType.EndTurn, $"Turn started for {_Context.ResolveUnitLabel(_Context.BattleService.CurrentTurn.UnitId)}.")
                : lResult;
        }

        private BattleActionResult AdvanceAfterImplicitTurnEnd(BattleActionResult pResult)
        {
            if (pResult == null || !pResult.IsSuccess || !_Context.AutoAdvanceTurns)
                return pResult;

            if (_Context.BattleService == null
                || _Context.BattleService.Outcome != BattleOutcome.None
                || _Context.BattleService.CurrentTurn != null
                || _Context.BattleService.Phase != BattlePhase.TurnEnd)
            {
                return pResult;
            }

            bool lStartedNextTurn = _Context.TryAdvanceBattle();
            return lStartedNextTurn && _Context.BattleService.CurrentTurn != null
                ? BattleActionResult.Succeeded(
                    pResult.ActionType,
                    $"{pResult.Message} Turn started for {_Context.ResolveUnitLabel(_Context.BattleService.CurrentTurn.UnitId)}.",
                    pResult.AffectedUnitIds,
                    pResult.TraversedPath)
                : pResult;
        }

        private BattleActionResult ExecuteReadyPlacementCommand()
        {
            if (!_Context.IsPlacementPhaseActive)
                return BattleActionResult.Failed(BattleActionType.Placement, "Placement phase is not active.");

            if (!_Context.TryValidatePlacementReadyForSlot(MatchPlayerSlot.TeamA, out BattleActionResult lTeamAFailure))
                return lTeamAFailure;

            if (!_Context.TryValidatePlacementReadyForSlot(MatchPlayerSlot.TeamB, out BattleActionResult lTeamBFailure))
                return lTeamBFailure;

            _Context.SetStatus("Combat started.");
            bool lStartedFirstTurn = _Context.TryAdvanceBattle();
            return lStartedFirstTurn && _Context.BattleService.CurrentTurn != null
                ? BattleActionResult.Succeeded(BattleActionType.Placement, $"Combat started. Turn started for {_Context.ResolveUnitLabel(_Context.BattleService.CurrentTurn.UnitId)}.")
                : BattleActionResult.Failed(BattleActionType.Placement, "Combat could not start.");
        }

        private BattleActionResult ExecutePlacementCommand(UnitId pUnitId, GridCoord pDestination)
        {
            if (!TryValidatePlacementCommand(pUnitId, pDestination, false, out BattleActionResult lFailure))
                return lFailure;

            return _Context.BattleService.RepositionUnitDuringPlacement(pUnitId, pDestination);
        }

        private bool TryValidatePlacementCommand(UnitId pUnitId, GridCoord pDestination, bool pRequireLocalControl, out BattleActionResult pFailure)
        {
            return CombatPlacementValidator.TryValidatePlacementCommand(
                _Context.BattleService,
                _Context.BoardView,
                _Context.CommandSink,
                pUnitId,
                pDestination,
                pRequireLocalControl,
                out pFailure);
        }

        #endregion
    }
}
