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

        [Header("Skill Kit")]
        [SerializeField] private SkillDefinitionAsset[] _skills;

        public int StartHitPoints => _startHitPoints;

        public int StartMovementPoints => _startMovementPoints;

        public int StartSkillResource => _startSkillResource;

        public SkillDefinitionAsset[] Skills => _skills;
    }
}
