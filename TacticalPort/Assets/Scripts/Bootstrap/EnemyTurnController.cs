using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Bootstrap
{
    public sealed class EnemyTurnController
    {
        #region _____________________________| VALUES

        private readonly CombatBootstrap _Bootstrap;
        private readonly float _DecisionDelaySeconds;

        private bool _HasPendingTurn;
        private bool _HasExecutedTurn;
        private BattleUnitId _PendingUnitId;
        private float _RemainingDelaySeconds;

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

            if (!_Bootstrap.TryGetActiveUnit(out BattleUnitRuntime lActiveUnit) || lActiveUnit.Team != BattleTeam.Enemy)
            {
                ClearPendingTurn();
                return;
            }

            if (!_HasPendingTurn || _PendingUnitId != lActiveUnit.Id)
                BeginPendingTurn(lActiveUnit.Id);

            if (_HasExecutedTurn)
                return;

            _RemainingDelaySeconds -= Mathf.Max(0f, pDeltaTime);
            if (_RemainingDelaySeconds > 0f)
                return;

            _HasExecutedTurn = true;
            PlayActiveTurn(lActiveUnit);
        }

        #endregion

        #region _____________________________| ACTIONS

        private void PlayActiveTurn(BattleUnitRuntime pActiveUnit)
        {
            BattleActionResult lActionResult = TryUseOffensiveSkill(pActiveUnit);

            if (lActionResult == null || !lActionResult.IsSuccess)
                lActionResult = TryUseHealingSkill(pActiveUnit);

            if (lActionResult == null || !lActionResult.IsSuccess)
                lActionResult = TryMoveTowardClosestPlayer(pActiveUnit);

            if (lActionResult != null && lActionResult.IsSuccess)
            {
                CompleteTurn(lActionResult.Message);
                return;
            }

            CompleteTurn($"{pActiveUnit.Definition.DisplayName} ends turn with no valid action.");
        }

        private BattleActionResult TryUseOffensiveSkill(BattleUnitRuntime pActiveUnit)
        {
            if (pActiveUnit == null)
                return null;

            SkillDefinition lBestSkill = null;
            BattleUnitRuntime lBestTarget = null;
            int lBestDistance = int.MaxValue;
            int lBestPower = int.MinValue;

            foreach (BattleUnitRuntime lCandidateUnit in _Bootstrap.BattleService.Units)
            {
                if (!IsTargetableEnemyUnit(pActiveUnit, lCandidateUnit))
                    continue;

                int lDistance = pActiveUnit.Position.ManhattanDistanceTo(lCandidateUnit.Position);

                foreach (SkillDefinition lSkill in pActiveUnit.Skills)
                {
                    if (!IsOffensiveSkill(lSkill))
                        continue;

                    SkillTarget lTarget = CreateSkillTarget(pActiveUnit, lSkill, lCandidateUnit);
                    if (lTarget == null)
                        continue;

                    BattleActionResult lValidation = _Bootstrap.ValidateActiveSkill(new SkillId(lSkill.Id), lTarget);
                    if (!lValidation.IsSuccess)
                        continue;

                    if (lBestSkill != null && (lDistance > lBestDistance || (lDistance == lBestDistance && lSkill.Power <= lBestPower)))
                        continue;

                    lBestSkill = lSkill;
                    lBestTarget = lCandidateUnit;
                    lBestDistance = lDistance;
                    lBestPower = lSkill.Power;
                }
            }

            if (lBestSkill == null || lBestTarget == null)
                return null;

            return _Bootstrap.UseActiveUnitSkill(new SkillId(lBestSkill.Id), CreateSkillTarget(pActiveUnit, lBestSkill, lBestTarget));
        }

        private BattleActionResult TryUseHealingSkill(BattleUnitRuntime pActiveUnit)
        {
            if (pActiveUnit == null)
                return null;

            SkillDefinition lBestSkill = null;
            BattleUnitRuntime lBestTarget = null;
            int lBestMissingHealth = 0;

            foreach (BattleUnitRuntime lCandidateUnit in _Bootstrap.BattleService.Units)
            {
                if (!IsTargetableAllyUnit(pActiveUnit, lCandidateUnit))
                    continue;

                int lMissingHealth = Mathf.Max(0, lCandidateUnit.Definition.MaxHealth - lCandidateUnit.CurrentHealth);
                if (lMissingHealth <= 0)
                    continue;

                foreach (SkillDefinition lSkill in pActiveUnit.Skills)
                {
                    if (!IsHealingSkill(lSkill))
                        continue;

                    SkillTarget lTarget = CreateSkillTarget(pActiveUnit, lSkill, lCandidateUnit);
                    if (lTarget == null)
                        continue;

                    BattleActionResult lValidation = _Bootstrap.ValidateActiveSkill(new SkillId(lSkill.Id), lTarget);
                    if (!lValidation.IsSuccess)
                        continue;

                    if (lBestSkill != null && lMissingHealth <= lBestMissingHealth)
                        continue;

                    lBestSkill = lSkill;
                    lBestTarget = lCandidateUnit;
                    lBestMissingHealth = lMissingHealth;
                }
            }

            if (lBestSkill == null || lBestTarget == null)
                return null;

            return _Bootstrap.UseActiveUnitSkill(new SkillId(lBestSkill.Id), CreateSkillTarget(pActiveUnit, lBestSkill, lBestTarget));
        }

        private BattleActionResult TryMoveTowardClosestPlayer(BattleUnitRuntime pActiveUnit)
        {
            if (pActiveUnit == null)
                return null;

            BattleUnitRuntime lClosestPlayer = FindClosestUnit(pActiveUnit.Position, BattleTeam.Player);
            if (lClosestPlayer == null)
                return null;

            GridCoord lBestCell = pActiveUnit.Position;
            int lCurrentDistance = pActiveUnit.Position.ManhattanDistanceTo(lClosestPlayer.Position);
            int lBestDistance = lCurrentDistance;
            bool lFoundBetterCell = false;

            foreach (GridCoord lReachableCell in _Bootstrap.GetReachableCellsForActiveUnit())
            {
                if (lReachableCell == pActiveUnit.Position)
                    continue;

                int lCandidateDistance = lReachableCell.ManhattanDistanceTo(lClosestPlayer.Position);
                if (lCandidateDistance > lBestDistance)
                    continue;

                if (lCandidateDistance == lBestDistance && (!lFoundBetterCell || !IsBetterCell(lReachableCell, lBestCell)))
                    continue;

                lBestCell = lReachableCell;
                lBestDistance = lCandidateDistance;
                lFoundBetterCell = true;
            }

            if (!lFoundBetterCell || lBestDistance >= lCurrentDistance)
                return null;

            return _Bootstrap.MoveActiveUnit(lBestCell);
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

        #endregion

        #region _____________________________| HELPERS

        private void BeginPendingTurn(BattleUnitId pUnitId)
        {
            _HasPendingTurn = true;
            _HasExecutedTurn = false;
            _PendingUnitId = pUnitId;
            _RemainingDelaySeconds = _DecisionDelaySeconds;
        }

        private void ClearPendingTurn()
        {
            _HasPendingTurn = false;
            _HasExecutedTurn = false;
            _PendingUnitId = BattleUnitId.None;
            _RemainingDelaySeconds = 0f;
        }

        private BattleUnitRuntime FindClosestUnit(GridCoord pOrigin, BattleTeam pTeam)
        {
            BattleUnitRuntime lClosestUnit = null;
            int lBestDistance = int.MaxValue;

            foreach (BattleUnitRuntime lCandidateUnit in _Bootstrap.BattleService.Units)
            {
                if (lCandidateUnit == null || !lCandidateUnit.IsAlive || lCandidateUnit.Team != pTeam)
                    continue;

                int lDistance = pOrigin.ManhattanDistanceTo(lCandidateUnit.Position);
                if (lClosestUnit != null && (lDistance > lBestDistance || (lDistance == lBestDistance && lCandidateUnit.Id.Value >= lClosestUnit.Id.Value)))
                    continue;

                lClosestUnit = lCandidateUnit;
                lBestDistance = lDistance;
            }

            return lClosestUnit;
        }

        private static SkillTarget CreateSkillTarget(BattleUnitRuntime pActor, SkillDefinition pSkill, BattleUnitRuntime pTargetUnit)
        {
            if (pSkill == null || pTargetUnit == null)
                return null;

            switch (pSkill.TargetType)
            {
                case SkillTargetType.Self:
                    return pActor != null && pTargetUnit.Id == pActor.Id ? SkillTarget.ForSelf(pActor.Id) : null;

                case SkillTargetType.Unit:
                    return SkillTarget.ForUnit(pTargetUnit.Id);

                case SkillTargetType.Cell:
                    return SkillTarget.ForCell(pTargetUnit.Position);

                default:
                    return null;
            }
        }

        private static bool IsOffensiveSkill(SkillDefinition pSkill)
        {
            return pSkill != null && pSkill.PrimaryEffectType == SkillPrimaryEffectType.Damage;
        }

        private static bool IsHealingSkill(SkillDefinition pSkill)
        {
            return pSkill != null && pSkill.PrimaryEffectType == SkillPrimaryEffectType.Heal;
        }

        private static bool IsTargetableEnemyUnit(BattleUnitRuntime pActor, BattleUnitRuntime pCandidateUnit)
        {
            return pActor != null
                && pCandidateUnit != null
                && pCandidateUnit.IsAlive
                && pCandidateUnit.Team == BattleTeam.Player
                && pCandidateUnit.Id != pActor.Id;
        }

        private static bool IsTargetableAllyUnit(BattleUnitRuntime pActor, BattleUnitRuntime pCandidateUnit)
        {
            return pActor != null
                && pCandidateUnit != null
                && pCandidateUnit.IsAlive
                && pCandidateUnit.Team == pActor.Team;
        }

        private static bool IsBetterCell(GridCoord pCandidate, GridCoord pCurrentBest)
        {
            if (pCandidate.X != pCurrentBest.X)
                return pCandidate.X < pCurrentBest.X;

            return pCandidate.Y < pCurrentBest.Y;
        }

        #endregion
    }
}
