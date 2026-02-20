// Utilité : ce script centralise la validation de ciblage d'une compétence
// en grille x/z avec ligne de vue "raycast" et bonus de portée via élévation (Y).
using System;
using PES.Combat.Actions;
using PES.Core.Simulation;

namespace PES.Combat.Targeting
{
    public sealed class SkillTargetingService
    {
        public SkillTargetingResult Evaluate(BattleState state, EntityId casterId, EntityId targetId, SkillActionPolicy policy)
        {
            if (!policy.IsValid)
            {
                return new SkillTargetingResult(false, SkillTargetingFailure.InvalidPolicy, 0, policy.MaxRange, 0);
            }

            if (casterId.Value == targetId.Value)
            {
                return new SkillTargetingResult(false, SkillTargetingFailure.SelfTargeting, 0, policy.MaxRange, 0);
            }

            if (!state.TryGetEntityPosition(casterId, out var casterPosition) || !state.TryGetEntityPosition(targetId, out var targetPosition))
            {
                return new SkillTargetingResult(false, SkillTargetingFailure.MissingPositions, 0, policy.MaxRange, 0);
            }

            // Distance de gameplay en x/z uniquement (pas de superposition de targeting sur Y).
            var distanceXZ = Math.Abs(targetPosition.X - casterPosition.X) + Math.Abs(targetPosition.Z - casterPosition.Z);

            var elevationSteps = casterPosition.Y / policy.ElevationPerRangeBonus;
            if (elevationSteps < 0)
            {
                elevationSteps = 0;
            }

            var effectiveMaxRange = policy.MaxRange + (elevationSteps * policy.RangeBonusPerElevationStep);

            if (distanceXZ < policy.MinRange)
            {
                return new SkillTargetingResult(false, SkillTargetingFailure.TooClose, distanceXZ, effectiveMaxRange, elevationSteps);
            }

            if (distanceXZ > effectiveMaxRange)
            {
                return new SkillTargetingResult(false, SkillTargetingFailure.OutOfRange, distanceXZ, effectiveMaxRange, elevationSteps);
            }

            if (!HasLineOfSightRaycast(state, casterPosition, targetPosition))
            {
                return new SkillTargetingResult(false, SkillTargetingFailure.LineOfSightBlocked, distanceXZ, effectiveMaxRange, elevationSteps);
            }

            return new SkillTargetingResult(true, SkillTargetingFailure.None, distanceXZ, effectiveMaxRange, elevationSteps);
        }

        private static bool HasLineOfSightRaycast(BattleState state, Position3 casterPosition, Position3 targetPosition)
        {
            var dx = targetPosition.X - casterPosition.X;
            var dz = targetPosition.Z - casterPosition.Z;
            var steps = Math.Max(Math.Abs(dx), Math.Abs(dz));
            if (steps <= 1)
            {
                return true;
            }

            for (var i = 1; i < steps; i++)
            {
                var t = (float)i / steps;
                var sampleX = (int)MathF.Round(casterPosition.X + (dx * t));
                var sampleZ = (int)MathF.Round(casterPosition.Z + (dz * t));
                var lineElevation = casterPosition.Y + ((targetPosition.Y - casterPosition.Y) * t);

                var obstacleElevation = int.MinValue;
                foreach (var blocked in state.GetBlockedPositions())
                {
                    if (blocked.X != sampleX || blocked.Z != sampleZ)
                    {
                        continue;
                    }

                    if (blocked.Y > obstacleElevation)
                    {
                        obstacleElevation = blocked.Y;
                    }
                }

                if (obstacleElevation != int.MinValue && obstacleElevation >= lineElevation)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public readonly struct SkillTargetingResult
    {
        public SkillTargetingResult(bool success, SkillTargetingFailure failure, int distanceXZ, int effectiveMaxRange, int elevationSteps)
        {
            Success = success;
            Failure = failure;
            DistanceXZ = distanceXZ;
            EffectiveMaxRange = effectiveMaxRange;
            ElevationSteps = elevationSteps;
        }

        public bool Success { get; }

        public SkillTargetingFailure Failure { get; }

        public int DistanceXZ { get; }

        public int EffectiveMaxRange { get; }

        public int ElevationSteps { get; }
    }

    public enum SkillTargetingFailure
    {
        None = 0,
        MissingPositions = 1,
        TooClose = 2,
        OutOfRange = 3,
        SelfTargeting = 4,
        InvalidPolicy = 5,
        LineOfSightBlocked = 6,
    }
}
