using PES.Combat.Actions;
using UnityEngine;

namespace PES.Presentation.Configuration
{
    [CreateAssetMenu(
        fileName = "SkillDefinition",
        menuName = "PES/Skill Definition",
        order = 20)]
    public sealed class SkillDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [Min(0)] [SerializeField] private int _skillId;

        [Header("Scope")]
        [Min(0)] [SerializeField] private int _minRange = 1;
        [Min(1)] [SerializeField] private int _maxRange = 3;
        [Min(1)] [SerializeField] private int _elevationPerRangeBonus = 2;
        [Min(0)] [SerializeField] private int _rangeBonusPerElevationStep = 1;

        [Header("Base Effects")]
        [Min(0)] [SerializeField] private int _baseDamage = 8;
        [Range(0, 100)] [SerializeField] private int _baseHitChance = 85;

        [Header("Costs")]
        [Min(0)] [SerializeField] private int _resourceCost;
        [Min(0)] [SerializeField] private int _cooldownTurns;

        public SkillActionPolicy ToSkillActionPolicy()
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
                cooldownTurns: _cooldownTurns);
        }
    }
}
