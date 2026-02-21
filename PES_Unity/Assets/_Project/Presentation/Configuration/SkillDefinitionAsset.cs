using PES.Combat.Actions;
using PES.Core.Simulation;
using UnityEngine;

namespace PES.Presentation.Configuration
{
    [CreateAssetMenu(
        fileName = "SkillDefinition",
        menuName = "PES/Skills/Skill Definition",
        order = 20)]
    public sealed class SkillDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [Min(0)] [SerializeField] private int _skillId;
        [SerializeField] private string _displayName = "New Skill";

        [Header("Range")]
        [Min(0)] [SerializeField] private int _minRange = 1;
        [Min(1)] [SerializeField] private int _maxRange = 3;
        [Min(1)] [SerializeField] private int _elevationPerRangeBonus = 2;
        [Min(0)] [SerializeField] private int _rangeBonusPerElevationStep = 1;

        [Header("Resolution")]
        [Min(0)] [SerializeField] private int _baseDamage = 8;
        [Range(0, 100)] [SerializeField] private int _baseHitChance = 85;
        [SerializeField] private DamageElement _damageElement = DamageElement.Elemental;
        [Range(0, 100)] [SerializeField] private int _baseCriticalChance = 5;

        [Header("Costs")]
        [Min(0)] [SerializeField] private int _resourceCost;
        [Min(0)] [SerializeField] private int _cooldownTurns;

        [Header("AOE (optional)")]
        [Min(0)] [SerializeField] private int _splashRadiusXZ;
        [Range(0, 100)] [SerializeField] private int _splashDamagePercent;

        [Header("Periodic Damage (optional)")]
        [Min(0)] [SerializeField] private int _periodicDamage;
        [Min(0)] [SerializeField] private int _periodicDurationTurns;
        [SerializeField] private PES.Core.Simulation.StatusEffectTickMoment _periodicTickMoment = PES.Core.Simulation.StatusEffectTickMoment.TurnStart;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? $"Skill {_skillId}" : _displayName;

        public SkillActionPolicy ToPolicy()
        {
            return new SkillActionPolicy(
                skillId: _skillId,
                minRange: _minRange,
                maxRange: _maxRange,
                baseDamage: _baseDamage,
                baseHitChance: _baseHitChance,
                elevationPerRangeBonus: _elevationPerRangeBonus,
                rangeBonusPerElevationStep: _rangeBonusPerElevationStep,
                resourceCost: _resourceCost,
                cooldownTurns: _cooldownTurns,
                splashRadiusXZ: _splashRadiusXZ,
                splashDamagePercent: _splashDamagePercent,
                periodicDamage: _periodicDamage,
                periodicDurationTurns: _periodicDurationTurns,
                periodicTickMoment: _periodicTickMoment,
                damageElement: _damageElement,
                baseCriticalChance: _baseCriticalChance);
        }
    }
}
