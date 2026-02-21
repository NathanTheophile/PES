using System;
using System.Collections.Generic;
using PES.Core.Simulation;
using UnityEngine;

namespace PES.Presentation.Configuration
{
    [CreateAssetMenu(
        fileName = "EntityArchetype",
        menuName = "PES/Combat/Entity Archetype",
        order = 21)]
    public sealed class EntityArchetypeAsset : ScriptableObject
    {
        [Header("Core Stats")]
        [SerializeField] [Min(1)] private int _startHitPoints = 40;
        [SerializeField] [Min(0)] private int _startMovementPoints = 6;
        [SerializeField] [Min(0)] private int _startSkillResource = 2;

        [Header("RPG Baseline")]
        [SerializeField] [Min(0)] private int _actionPoints = 6;
        [SerializeField] [Min(0)] private int _baseRange = 1;
        [SerializeField] [Min(0)] private int _baseElevation = 1;
        [SerializeField] [Min(0)] private int _summonCapacity = 1;
        [SerializeField] [Min(0)] private int _assiduity;
        [SerializeField] [Min(0)] private int _rapidity;
        [SerializeField] [Min(0)] private int _criticalChance;
        [SerializeField] [Min(0)] private int _criticalDamage;
        [SerializeField] [Min(0)] private int _criticalResistance;

        [Header("Attack (flat)")]
        [SerializeField] private DamageElementValuesSerialized _attack;

        [Header("Power (%)")]
        [SerializeField] private DamageElementValuesSerialized _power = DamageElementValuesSerialized.HundredPercent;

        [Header("Defense (flat)")]
        [SerializeField] private DamageElementValuesSerialized _defense;

        [Header("Resistance (%)")]
        [SerializeField] private DamageElementValuesSerialized _resistance;

        [Header("Skill Kit")]
        [SerializeField] private SkillDefinitionAsset[] _skills;

        public int StartHitPoints => _startHitPoints;

        public int StartMovementPoints => _startMovementPoints;

        public int StartSkillResource => _startSkillResource;

        public IReadOnlyList<SkillDefinitionAsset> Skills => _skills ?? Array.Empty<SkillDefinitionAsset>();

        public CombatantRpgStats ToCombatantStats()
        {
            return new CombatantRpgStats(
                actionPoints: _actionPoints,
                movementPoints: _startMovementPoints,
                range: _baseRange,
                elevation: _baseElevation,
                summonCapacity: _summonCapacity,
                hitPoints: _startHitPoints,
                assiduity: _assiduity,
                rapidity: _rapidity,
                criticalChance: _criticalChance,
                criticalDamage: _criticalDamage,
                criticalResistance: _criticalResistance,
                attack: _attack.ToDomainValues(),
                power: _power.ToDomainValues(),
                defense: _defense.ToDomainValues(),
                resistance: _resistance.ToDomainValues());
        }
    }

    [Serializable]
    public struct DamageElementValuesSerialized
    {
        [Min(0)] [SerializeField] private int _blunt;
        [Min(0)] [SerializeField] private int _physical;
        [Min(0)] [SerializeField] private int _piercing;
        [Min(0)] [SerializeField] private int _explosive;
        [Min(0)] [SerializeField] private int _elemental;
        [Min(0)] [SerializeField] private int _spiritual;

        public static DamageElementValuesSerialized HundredPercent => new()
        {
            _blunt = 100,
            _physical = 100,
            _piercing = 100,
            _explosive = 100,
            _elemental = 100,
            _spiritual = 100,
        };

        public DamageElementValues ToDomainValues()
        {
            return new DamageElementValues(_blunt, _physical, _piercing, _explosive, _elemental, _spiritual);
        }
    }
}
