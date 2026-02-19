// Utilité : ce script centralise les validations de ciblage d'une attaque de base
// (positions connues, portée, ligne de vue) pour garder les actions métier lisibles.
using System;
using PES.Core.Simulation;

namespace PES.Combat.Targeting
{
    /// <summary>
    /// Service de validation de ciblage pour BasicAttackAction.
    /// </summary>
    public sealed class BasicAttackTargetingService
    {
        /// <summary>
        /// Évalue la validité du ciblage d'une attaque entre deux entités.
        /// </summary>
        public BasicAttackTargetingResult Evaluate(
            BattleState state,
            EntityId attackerId,
            EntityId targetId,
            int maxRange,
            int maxLineOfSightDelta)
        {
            if (!state.TryGetEntityPosition(attackerId, out var attackerPosition) ||
                !state.TryGetEntityPosition(targetId, out var targetPosition))
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.MissingPositions, default, default, 0, 0);
            }

            var horizontalDistance = Math.Abs(targetPosition.X - attackerPosition.X) + Math.Abs(targetPosition.Y - attackerPosition.Y);
            if (horizontalDistance > maxRange)
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.OutOfRange, attackerPosition, targetPosition, horizontalDistance, 0);
            }

            var verticalDelta = Math.Abs(targetPosition.Z - attackerPosition.Z);
            if (verticalDelta > maxLineOfSightDelta)
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.LineOfSightBlocked, attackerPosition, targetPosition, horizontalDistance, verticalDelta);
            }

            if (IsLineBlockedByTerrain(state, attackerPosition, targetPosition))
            {
                return BasicAttackTargetingResult.Reject(BasicAttackTargetingFailure.LineOfSightBlocked, attackerPosition, targetPosition, horizontalDistance, verticalDelta);
            }

            return BasicAttackTargetingResult.Accept(attackerPosition, targetPosition, horizontalDistance, verticalDelta);
        }

        private static bool IsLineBlockedByTerrain(BattleState state, Position3 from, Position3 to)
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
                if (state.IsPositionBlocked(new Position3(x, y, z)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Détails de validation de ciblage d'une attaque basique.
    /// </summary>
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

    /// <summary>
    /// Causes standard de rejet du ciblage basique.
    /// </summary>
    public enum BasicAttackTargetingFailure
    {
        None = 0,
        MissingPositions = 1,
        OutOfRange = 2,
        LineOfSightBlocked = 3,
    }
}
