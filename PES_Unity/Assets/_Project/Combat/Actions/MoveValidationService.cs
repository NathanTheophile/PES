using System;
using System.Collections.Generic;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Grid.Pathfinding;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Paramètres métiers qui encadrent la validation d'un déplacement.
    /// </summary>
    public readonly struct MoveActionPolicy
    {
        public MoveActionPolicy(int maxMovementCostPerAction, int maxVerticalStepPerTile)
        {
            MaxMovementCostPerAction = maxMovementCostPerAction;
            MaxVerticalStepPerTile = maxVerticalStepPerTile;
        }

        public int MaxMovementCostPerAction { get; }

        public int MaxVerticalStepPerTile { get; }
    }

    /// <summary>
    /// Raisons normalisées de refus d'un déplacement.
    /// </summary>
    public enum MoveValidationFailure
    {
        None = 0,
        InvalidOrigin = 1,
        DestinationBlocked = 2,
        DestinationOccupied = 3,
        BlockedPath = 4,
        VerticalStepTooHigh = 5,
        MovementBudgetExceeded = 6,
        StateMutationFailed = 7,
        NoMovement = 8,
        InvalidPolicy = 9,
    }

    /// <summary>
    /// Résultat structuré d'une validation de déplacement.
    /// </summary>
    public readonly struct MoveValidationResult
    {
        public MoveValidationResult(bool success, MoveValidationFailure failure, int movementCost)
        {
            Success = success;
            Failure = failure;
            MovementCost = movementCost;
        }

        public bool Success { get; }

        public MoveValidationFailure Failure { get; }

        public int MovementCost { get; }
    }

    /// <summary>
    /// Service métier qui valide un MoveAction sans dépendre de MonoBehaviour.
    /// </summary>
    public sealed class MoveValidationService
    {

        private static bool IsPolicyValid(MoveActionPolicy policy)
        {
            return policy.MaxMovementCostPerAction > 0 && policy.MaxVerticalStepPerTile >= 0;
        }
        public MoveValidationResult Validate(BattleState state, EntityId actorId, GridCoord3 origin, GridCoord3 destination, MoveActionPolicy policy)
        {
            if (!IsPolicyValid(policy))
            {
                return new MoveValidationResult(false, MoveValidationFailure.InvalidPolicy, 0);
            }

            var originPosition = ToPosition(origin);
            var destinationPosition = ToPosition(destination);

            if (!state.TryGetEntityPosition(actorId, out var actorCurrentPosition) || !actorCurrentPosition.Equals(originPosition))
            {
                return new MoveValidationResult(false, MoveValidationFailure.InvalidOrigin, 0);
            }

            if (state.IsPositionBlocked(destinationPosition))
            {
                return new MoveValidationResult(false, MoveValidationFailure.DestinationBlocked, 0);
            }

            if (state.IsPositionOccupied(destinationPosition, actorId))
            {
                return new MoveValidationResult(false, MoveValidationFailure.DestinationOccupied, 0);
            }

            if (origin.Equals(destination))
            {
                return new MoveValidationResult(false, MoveValidationFailure.NoMovement, 0);
            }

            if (!state.IsWalkablePosition(destinationPosition))
            {
                return new MoveValidationResult(false, MoveValidationFailure.DestinationBlocked, 0);
            }

            // Contrainte explicite de saut vertical par action (indépendante du coût).
            var actionVerticalDelta = Math.Abs(destination.Z - origin.Z);
            if (actionVerticalDelta > policy.MaxVerticalStepPerTile)
            {
                return new MoveValidationResult(false, MoveValidationFailure.VerticalStepTooHigh, 0);
            }

            var blockedCells = BuildBlockedCellSet(state, actorId, originPosition, destinationPosition);
            var walkableCells = BuildWalkableCellSet(state);
            var pathService = new PathfindingService();
            if (!pathService.TryComputePath(origin, destination, blockedCells, out var path, walkableCells, policy.MaxVerticalStepPerTile))
            {
                return new MoveValidationResult(false, MoveValidationFailure.BlockedPath, 0);
            }

            var movementCost = ComputeMovementCost(state, path, policy.MaxVerticalStepPerTile);
            if (movementCost == int.MaxValue)
            {
                return new MoveValidationResult(false, MoveValidationFailure.VerticalStepTooHigh, 0);
            }

            if (movementCost > policy.MaxMovementCostPerAction)
            {
                return new MoveValidationResult(false, MoveValidationFailure.MovementBudgetExceeded, movementCost);
            }

            return new MoveValidationResult(true, MoveValidationFailure.None, movementCost);
        }

        private static int ComputeMovementCost(BattleState state, IReadOnlyList<GridCoord3> path, int maxVerticalStepPerTile)
        {
            var totalCost = 0;
            for (var i = 1; i < path.Count; i++)
            {
                var previous = path[i - 1];
                var current = path[i];

                var verticalStep = Math.Abs(current.Z - previous.Z);
                if (verticalStep > maxVerticalStepPerTile)
                {
                    return int.MaxValue;
                }

                var cellCost = state.GetMovementCost(ToPosition(current));
                var stepCost = cellCost + verticalStep;
                totalCost += stepCost;
            }

            return totalCost;
        }

        private static HashSet<GridCoord3> BuildBlockedCellSet(BattleState state, EntityId actorId, Position3 origin, Position3 destination)
        {
            var blocked = new HashSet<GridCoord3>();

            foreach (var blockedPosition in state.GetBlockedPositions())
            {
                blocked.Add(new GridCoord3(blockedPosition.X, blockedPosition.Y, blockedPosition.Z));
            }

            foreach (var pair in state.GetEntityPositions())
            {
                if (pair.Key.Equals(actorId))
                {
                    continue;
                }

                var position = pair.Value;
                if (position.Equals(origin) || position.Equals(destination))
                {
                    continue;
                }

                blocked.Add(new GridCoord3(position.X, position.Y, position.Z));
            }

            return blocked;
        }


        private static HashSet<GridCoord3> BuildWalkableCellSet(BattleState state)
        {
            var walkable = new HashSet<GridCoord3>();
            foreach (var walkablePosition in state.GetWalkablePositions())
            {
                walkable.Add(new GridCoord3(walkablePosition.X, walkablePosition.Y, walkablePosition.Z));
            }

            return walkable;
        }

        private static Position3 ToPosition(GridCoord3 coord)
        {
            return new Position3(coord.X, coord.Y, coord.Z);
        }
    }
}
