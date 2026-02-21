using PES.Combat.Actions;
using PES.Combat.Resolution;
using UnityEngine;

namespace PES.Presentation.Configuration
{
    [CreateAssetMenu(
        fileName = "CombatRuntimeConfig",
        menuName = "PES/Combat Runtime Config",
        order = 10)]
    public sealed class CombatRuntimeConfigAsset : ScriptableObject
    {
        [Header("Move Policy")]
        [Min(1)] [SerializeField] private int _maxMovementCostPerAction = 3;
        [Min(0)] [SerializeField] private int _maxVerticalStepPerTile = 1;

        [Header("Basic Attack Policy")]
        [Min(0)] [SerializeField] private int _minRange = 1;
        [Min(1)] [SerializeField] private int _maxRange = 2;
        [Min(0)] [SerializeField] private int _maxLineOfSightDelta = 2;

        [Header("Basic Attack Resolution")]
        [SerializeField] private int _baseDamage = 12;
        [Range(0, 100)] [SerializeField] private int _baseHitChance = 80;

        [Header("Skill Policy")]
        [Min(0)] [SerializeField] private int _skillId = 0;
        [Min(0)] [SerializeField] private int _skillMinRange = 1;
        [Min(1)] [SerializeField] private int _skillMaxRange = 3;
        [Min(0)] [SerializeField] private int _skillBaseDamage = 8;
        [Range(0, 100)] [SerializeField] private int _skillBaseHitChance = 85;
        [Min(1)] [SerializeField] private int _skillElevationPerRangeBonus = 2;
        [Min(0)] [SerializeField] private int _skillRangeBonusPerElevationStep = 1;
        [Min(0)] [SerializeField] private int _skillSplashRadiusXZ = 0;
        [Range(0, 100)] [SerializeField] private int _skillSplashDamagePercent = 0;
        [Min(0)] [SerializeField] private int _skillPeriodicDamage = 0;
        [Min(0)] [SerializeField] private int _skillPeriodicDurationTurns = 0;
        [SerializeField] private PES.Core.Simulation.StatusEffectTickMoment _skillPeriodicTickMoment = PES.Core.Simulation.StatusEffectTickMoment.TurnStart;
        [Range(0, 100)] [SerializeField] private int _skillVulnerablePotencyPercent = 0;
        [Min(0)] [SerializeField] private int _skillVulnerableDurationTurns = 0;

        public MoveActionPolicy ToMovePolicy()
        {
            return new MoveActionPolicy(
                maxMovementCostPerAction: _maxMovementCostPerAction,
                maxVerticalStepPerTile: _maxVerticalStepPerTile);
        }

        public BasicAttackActionPolicy ToBasicAttackPolicy()
        {
            return new BasicAttackActionPolicy(
                minRange: _minRange,
                maxRange: _maxRange,
                maxLineOfSightDelta: _maxLineOfSightDelta,
                resolutionPolicy: new BasicAttackResolutionPolicy(
                    baseDamage: _baseDamage,
                    baseHitChance: _baseHitChance));
        }

        public SkillActionPolicy ToSkillPolicy()
        {
            return new SkillActionPolicy(
                skillId: _skillId,
                minRange: _skillMinRange,
                maxRange: _skillMaxRange,
                baseDamage: _skillBaseDamage,
                baseHitChance: _skillBaseHitChance,
                elevationPerRangeBonus: _skillElevationPerRangeBonus,
                rangeBonusPerElevationStep: _skillRangeBonusPerElevationStep,
                splashRadiusXZ: _skillSplashRadiusXZ,
                splashDamagePercent: _skillSplashDamagePercent,
                periodicDamage: _skillPeriodicDamage,
                periodicDurationTurns: _skillPeriodicDurationTurns,
                periodicTickMoment: _skillPeriodicTickMoment,
                vulnerablePotencyPercent: _skillVulnerablePotencyPercent,
                vulnerableDurationTurns: _skillVulnerableDurationTurns);
        }
    }
}
