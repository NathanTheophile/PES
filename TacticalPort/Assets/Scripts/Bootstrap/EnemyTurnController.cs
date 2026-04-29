#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using TacticalPort.UI;

namespace TacticalPort.Bootstrap
{
    public sealed class EnemyTurnController
    {
        #region _____________________________/ VALUES

        private readonly CombatBootstrap _Bootstrap;
        private readonly float _DecisionDelaySeconds;
        private readonly IEnemyBrain _Brain = new DefaultEnemyBrain();

        private bool _HasPendingTurn;
        private UnitId _PendingUnitId;
        private float _RemainingDelaySeconds;
        private int _ExecutedActionCount;
        private bool _ForceKiteNextDecision;

        private const int MaxActionsPerTurn = 10;

        #endregion

        #region _____________________________| INIT

        public EnemyTurnController(CombatBootstrap pBootstrap, float pDecisionDelaySeconds)
        {
            _Bootstrap = pBootstrap;
            _DecisionDelaySeconds = Mathf.Max(0f, pDecisionDelaySeconds);
        }

        #endregion

        #region _____________________________| FLOW

        public void Tick(float pDeltaTime)
        {
            if (_Bootstrap == null || !_Bootstrap.IsBootstrapped || _Bootstrap.BattleService == null)
            {
                ClearPendingTurn();
                return;
            }

            if (_Bootstrap.BattleService.Outcome != BattleOutcome.None || _Bootstrap.BattleService.Phase != BattlePhase.AwaitingAction)
                return;

            if (!_Bootstrap.TryGetActiveUnit(out UnitRuntime lActiveUnit) || lActiveUnit.Team != Team.Enemy)
            {
                ClearPendingTurn();
                return;
            }

            if (!_HasPendingTurn || _PendingUnitId != lActiveUnit.Id)
                BeginPendingTurn(lActiveUnit.Id);

            _RemainingDelaySeconds -= Mathf.Max(0f, pDeltaTime);
            if (_RemainingDelaySeconds > 0f)
                return;

            PlayNextAction(lActiveUnit);
        }

        #endregion

        #region _____________________________| ACTIONS

        private void PlayNextAction(UnitRuntime pActiveUnit)
        {
            if (pActiveUnit == null || _ExecutedActionCount >= MaxActionsPerTurn)
            {
                CompleteTurn(pActiveUnit != null ? $"{pActiveUnit.Definition.DisplayName} ends turn." : "Enemy turn completed.");
                return;
            }

            EnemyAiContext lContext = new EnemyAiContext(
                _Bootstrap.BattleService,
                pActiveUnit,
                _Bootstrap.GetReachableCellsForActiveUnit(),
                (pSkill, pTarget) => _Bootstrap.ValidateActiveSkill(new SkillId(pSkill.Id), pTarget),
                _ForceKiteNextDecision);
            _ForceKiteNextDecision = false;
            EnemyAiAction lAction = _Brain.ChooseAction(lContext);
            BattleActionResult lResult = Execute(lAction);
            if (pActiveUnit.Definition.EnemyAiDebugDecisions && !string.IsNullOrWhiteSpace(lAction.Reason))
                _Bootstrap.HudManager?.SetStatus(lAction.Reason);

            if (lAction.Type == EnemyAiActionType.EndTurn || lResult == null || !lResult.IsSuccess)
            {
                CompleteTurn(!string.IsNullOrWhiteSpace(lAction.Reason) ? lAction.Reason : lResult?.Message);
                return;
            }

            _ExecutedActionCount++;
            if (lAction.RequestsKiteAfterUse)
                _ForceKiteNextDecision = true;

            _RemainingDelaySeconds = _DecisionDelaySeconds;
        }

        private void CompleteTurn(string pStatusMessage)
        {
            if (_Bootstrap.BattleService == null || _Bootstrap.BattleService.Outcome != BattleOutcome.None || _Bootstrap.CurrentTurn == null)
            {
                ClearPendingTurn();
                return;
            }

            _Bootstrap.EndActiveTurn(pStatusMessage);
            ClearPendingTurn();
        }

        private BattleActionResult Execute(EnemyAiAction pAction)
        {
            switch (pAction.Type)
            {
                case EnemyAiActionType.Move:
                    return _Bootstrap.MoveActiveUnit(pAction.Destination);

                case EnemyAiActionType.UseSkill:
                    return pAction.Skill != null && pAction.Target != null
                        ? _Bootstrap.UseActiveUnitSkill(new SkillId(pAction.Skill.Id), pAction.Target)
                        : BattleActionResult.Failed(BattleActionType.Skill, "AI selected an invalid skill.");

                default:
                    return null;
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private void BeginPendingTurn(UnitId pUnitId)
        {
            _HasPendingTurn = true;
            _PendingUnitId = pUnitId;
            _RemainingDelaySeconds = _DecisionDelaySeconds;
            _ExecutedActionCount = 0;
            _ForceKiteNextDecision = false;
        }

        private void ClearPendingTurn()
        {
            _HasPendingTurn = false;
            _PendingUnitId = UnitId.None;
            _RemainingDelaySeconds = 0f;
            _ExecutedActionCount = 0;
            _ForceKiteNextDecision = false;
        }

        #endregion
    }
}
