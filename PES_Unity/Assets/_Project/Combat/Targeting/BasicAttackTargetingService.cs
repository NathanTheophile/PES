// Utilité : ce script centralise les validations de ciblage d'une attaque de base
// (positions connues, portée, ligne de vue) pour garder les actions métier lisibles.
using System;
using PES.Core.Simulation;

namespace PES.Combat.Targeting
{
    public sealed class BasicAttackTargetingService
    {
        public BasicAttackTargetingResult Evaluate(
            BattleState state,
            EntityId attackerId,
            EntityId targetId,
            int minRange,
            int maxRange,
            int maxLineOfSightDelta)
        {
            if (attackerId.Equals(targetId))
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.SelfTargeting, default, default, 0, 0);
            }

            if (minRange < 0 || maxRange < 0 || maxRange < minRange || maxLineOfSightDelta < 0)
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.InvalidPolicy, default, default, 0, 0);
            }

            if (!state.TryGetEntityPosition(attackerId, out var attackerPosition) ||
                !state.TryGetEntityPosition(targetId, out var targetPosition))
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.MissingPositions, default, default, 0, 0);
            }

            var horizontalDistance = Math.Abs(targetPosition.X - attackerPosition.X) + Math.Abs(targetPosition.Y - attackerPosition.Y);
            if (horizontalDistance < minRange)
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.TooClose, attackerPosition, targetPosition, horizontalDistance, 0);
            }

            if (horizontalDistance > maxRange)
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.OutOfRange, attackerPosition, targetPosition, horizontalDistance, 0);
            }

            var verticalDelta = targetPosition.Z - attackerPosition.Z;
            if (Math.Abs(verticalDelta) > maxLineOfSightDelta)
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.LineOfSightBlocked, attackerPosition, targetPosition, horizontalDistance, verticalDelta);
            }

            if (IsLineBlocked(state, attackerPosition, targetPosition, attackerId, targetId))
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.LineOfSightBlocked, attackerPosition, targetPosition, horizontalDistance, verticalDelta);
            }

            return BasicAttackTargetingResult.Accept(attackerPosition, targetPosition, horizontalDistance, verticalDelta);
        }

        private static bool IsLineBlocked(BattleState state, Position3 from, Position3 to, EntityId attackerId, EntityId targetId)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var dz = to.Z - from.Z;
            var steps = Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz));

            if (steps <= 1)
            {
                return false;
            }

            for (var i = 1; i < steps; i++)
            {
                var t = (double)i / steps;
                var x = (int)Math.Round(from.X + (dx * t));
                var y = (int)Math.Round(from.Y + (dy * t));
                var z = (int)Math.Round(from.Z + (dz * t));
                var sample = new Position3(x, y, z);

                if (state.IsPositionBlocked(sample))
                {
                    return true;
                }

                if (state.IsPositionOccupied(sample, attackerId) && !sample.Equals(to))
                {
                    return true;
                }

                if (state.IsPositionOccupied(sample, targetId) && !sample.Equals(to))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public readonly struct BasicAttackTargetingResult
    {
        private BasicAttackTargetingResult(
            bool success,
            BasicAttackTargetingFailure failure,
            Position3 attackerPosition,
            Position3 targetPosition,
            int horizontalDistance,
            int verticalDelta)
        {
            Success = success;
            Failure = failure;
            AttackerPosition = attackerPosition;
            TargetPosition = targetPosition;
            HorizontalDistance = horizontalDistance;
            VerticalDelta = verticalDelta;
        }

        public bool Success { get; }
        public BasicAttackTargetingFailure Failure { get; }
        public Position3 AttackerPosition { get; }
        public Position3 TargetPosition { get; }
        public int HorizontalDistance { get; }
        public int VerticalDelta { get; }

        public static BasicAttackTargetingResult Accept(
            Position3 attackerPosition,
            Position3 targetPosition,
            int horizontalDistance,
            int verticalDelta)
        {
            return new BasicAttackTargetingResult(true, BasicAttackTargetingFailure.None, attackerPosition, targetPosition, horizontalDistance, verticalDelta);
        }

        public static BasicAttackTargetingResult Reject(
            BasicAttackTargetingFailure failure,
            Position3 attackerPosition,
            Position3 targetPosition,
            int horizontalDistance,
            int verticalDelta)
        {
            return new BasicAttackTargetingResult(false, failure, attackerPosition, targetPosition, horizontalDistance, verticalDelta);
        }
    }

    public enum BasicAttackTargetingFailure
    {
        None = 0,
        MissingPositions = 1,
        TooClose = 2,
        OutOfRange = 3,
        LineOfSightBlocked = 4,
        SelfTargeting = 5,
        InvalidPolicy = 6,
    }
}
