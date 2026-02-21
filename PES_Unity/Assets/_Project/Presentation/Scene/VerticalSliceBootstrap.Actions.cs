using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using UnityEngine;
using EntityId = PES.Core.Simulation.EntityId;

namespace PES.Presentation.Scene
{
    public sealed partial class VerticalSliceBootstrap
    {
        private void TryAutoPassTurnWhenNoActionsRemaining()
        {
            if (_battleLoop.IsBattleOver || _battleLoop.RemainingActions > 0 || _planner.HasPlannedAction)
            {
                return;
            }

            var actorId = _battleLoop.CurrentActorId;
            if (!_battleLoop.TryPassTurn(actorId, out var passResult))
            {
                return;
            }

            _lastResult = passResult;
            SyncUnitViews();
            Debug.Log($"[VerticalSlice] AutoPass: {_lastResult.Description}");
        }

        private ActionResolution PlanAndTryMove(EntityId actorId, GridCoord3 destination)
        {
            var destinationPosition = new Position3(destination.X, destination.Y, destination.Z);
            if (_battleLoop.State.IsPositionOccupied(destinationPosition, actorId))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination occupied ({destination})", ActionFailureReason.DestinationOccupied);
            }

            if (!_currentReachableTiles.Contains(destinationPosition))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination unreachable ({destination})", ActionFailureReason.MovementBudgetExceeded);
            }

            _planner.PlanMove(destination);
            TryExecutePlanned();
            return _lastResult;
        }

        private bool TryFindAdjacentMoveDestinationAndPlan(EntityId actorId)
        {
            if (!TryFindAdjacentMoveDestination(actorId, out var destination))
            {
                return false;
            }

            _planner.PlanMove(destination);
            return true;
        }

        private void PlanAttackToOtherActor(EntityId actorId)
        {
            var target = actorId.Equals(VerticalSliceBattleLoop.UnitA)
                ? VerticalSliceBattleLoop.UnitB
                : VerticalSliceBattleLoop.UnitA;
            _planner.PlanAttack(target);
        }

        private void CancelPlannedAction()
        {
            if (!_planner.HasPlannedAction)
            {
                _lastResult = new ActionResolution(false, ActionResolutionCode.Rejected, "CancelIgnored: no planned action", ActionFailureReason.InvalidTargeting);
                return;
            }

            _planner.ClearPlannedAction();
            _lastResult = new ActionResolution(true, ActionResolutionCode.Succeeded, "PlanCancelled: cleared planned action");
        }

        private void TryPassTurn()
        {
            if (!_planner.HasActorSelection)
            {
                return;
            }

            _battleLoop.TryPassTurn(_planner.SelectedActorId, out _lastResult);
            PlayActionVfxPlaceholder(_planner.SelectedActorId, null, _lastResult);
            _planner.ClearPlannedAction();
            SyncUnitViews();
            Debug.Log($"[VerticalSlice] {_lastResult.Description}");
        }

        private void TryExecutePlanned()
        {
            if (_battleLoop.IsBattleOver)
            {
                return;
            }

            if (_planner.TryBuildCommand(out var actorId, out var command))
            {
                var hadTarget = _planner.TryGetPlannedTarget(out var plannedTarget);
                _battleLoop.TryExecutePlannedCommand(actorId, command, out _lastResult);
                PlayActionVfxPlaceholder(actorId, hadTarget ? plannedTarget : (EntityId?)null, _lastResult);
                _planner.ClearPlannedAction();
                SyncUnitViews();
                Debug.Log($"[VerticalSlice] {_lastResult.Description}");
            }
        }

        private bool TryFindAdjacentMoveDestination(EntityId actorId, out GridCoord3 destination)
        {
            destination = default;
            if (!_battleLoop.State.TryGetEntityPosition(actorId, out var currentPosition))
            {
                return false;
            }

            var origin = new GridCoord3(currentPosition.X, currentPosition.Y, currentPosition.Z);
            var candidates = new List<GridCoord3>
            {
                new(origin.X + 1, origin.Y, origin.Z),
                new(origin.X - 1, origin.Y, origin.Z),
                new(origin.X, origin.Y + 1, origin.Z),
                new(origin.X, origin.Y - 1, origin.Z),
                new(origin.X, origin.Y, origin.Z + 1),
                new(origin.X, origin.Y, origin.Z - 1),
            };

            foreach (var candidate in candidates)
            {
                var asPosition = new Position3(candidate.X, candidate.Y, candidate.Z);
                if (_battleLoop.State.IsPositionBlocked(asPosition) || _battleLoop.State.IsPositionOccupied(asPosition, actorId))
                {
                    continue;
                }

                destination = candidate;
                return true;
            }

            return false;
        }

        private bool TryPlanSkill(EntityId targetId)
        {
            if (!_planner.HasActorSelection)
            {
                return false;
            }

            if (!_planner.TryGetSkillPolicy(_planner.SelectedActorId, _selectedSkillSlot, out var policy))
            {
                _lastResult = new ActionResolution(false, ActionResolutionCode.Rejected, $"SkillSelectionRejected: no skill in slot {_selectedSkillSlot + 1} for {_planner.SelectedActorId}", ActionFailureReason.InvalidPolicy);
                return false;
            }

            _planner.PlanSkill(targetId, _selectedSkillSlot);
            _lastResult = new ActionResolution(true, ActionResolutionCode.Succeeded, $"SkillSelected: {_planner.SelectedActorId} slot:{_selectedSkillSlot + 1} skill:{policy.SkillId}");
            return true;
        }

        private string GetSelectedSkillLabel()
        {
            if (_planner == null || !_planner.HasActorSelection)
            {
                return "n/a";
            }

            if (!_planner.TryGetSkillPolicy(_planner.SelectedActorId, _selectedSkillSlot, out var policy))
            {
                return "none";
            }

            return $"SkillId:{policy.SkillId}";
        }

        private void SyncSelectedSkillSlot()
        {
            if (_planner == null || !_planner.HasActorSelection)
            {
                _selectedSkillSlot = 0;
                return;
            }

            var availableSkills = _planner.GetAvailableSkillCount(_planner.SelectedActorId);
            if (availableSkills <= 0 || _selectedSkillSlot < 0 || _selectedSkillSlot >= availableSkills)
            {
                _selectedSkillSlot = 0;
            }
        }
    }
}
