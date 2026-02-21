using System;
using System.Collections.Generic;
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
        [SerializeField] [Min(0)] private int _startActionPoints = 6;
        [SerializeField] [Min(0)] private int _startRange = 3;
        [SerializeField] [Min(0)] private int _startElevation = 0;
        [SerializeField] [Min(0)] private int _startSummonSlots = 1;
        [SerializeField] [Min(0)] private int _diligence;
        [SerializeField] [Min(0)] private int _quickness;
        [SerializeField] private PES.Core.Simulation.AttackElement _affinityElement = PES.Core.Simulation.AttackElement.Physique;
        [SerializeField] [Min(0)] private int _masteryContondante;
        [SerializeField] [Min(0)] private int _masteryPhysique;
        [SerializeField] [Min(0)] private int _masteryElementaire;
        [SerializeField] [Min(0)] private int _masterySpeciale;
        [SerializeField] [Min(0)] private int _masterySpirituelle;
        [SerializeField] [Range(0, 100)] private int _criticalChancePercent;
        [SerializeField] [Min(0)] private int _criticalDamagePercent;
        [SerializeField] private int _resistancePercent;
        [SerializeField] private int _specialResistancePercent;
        [SerializeField] private int _criticalResistancePercent;

        [Header("Skill Kit")]
        [SerializeField] private SkillDefinitionAsset[] _skills;

        public int StartHitPoints => _startHitPoints;

        public int StartMovementPoints => _startMovementPoints;

        public int StartSkillResource => _startSkillResource;

        public EntityStatBlock ToStatBlock()
        {
            return new EntityStatBlock(
                actionPoints: _startActionPoints,
                movementPoints: _startMovementPoints,
                range: _startRange,
                elevation: _startElevation,
                summonSlots: _startSummonSlots,
                hitPoints: _startHitPoints,
                diligence: _diligence,
                quickness: _quickness,
                affinityElement: _affinityElement,
                masteryContondante: _masteryContondante,
                masteryPhysique: _masteryPhysique,
                masteryElementaire: _masteryElementaire,
                masterySpeciale: _masterySpeciale,
                masterySpirituelle: _masterySpirituelle,
                criticalChancePercent: _criticalChancePercent,
                criticalDamagePercent: _criticalDamagePercent,
                resistancePercent: _resistancePercent,
                specialResistancePercent: _specialResistancePercent,
                criticalResistancePercent: _criticalResistancePercent);
        }

        public IReadOnlyList<SkillDefinitionAsset> Skills => _skills ?? Array.Empty<SkillDefinitionAsset>();
    }
}
