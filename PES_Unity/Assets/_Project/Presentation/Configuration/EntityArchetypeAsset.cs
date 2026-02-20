using UnityEngine;

namespace PES.Presentation.Configuration
{
    [CreateAssetMenu(
        fileName = "EntityArchetype",
        menuName = "PES/Entity Archetype",
        order = 30)]
    public sealed class EntityArchetypeAsset : ScriptableObject
    {
        [Header("Base Stats")]
        [Min(0)] [SerializeField] private int _baseHitPoints = 100;
        [Min(0)] [SerializeField] private int _baseMovementPoints = 6;

        [Header("Resources")]
        [Min(0)] [SerializeField] private int _baseSkillResource = 3;

        [Header("Tags")]
        [SerializeField] private string[] _tags = new string[0];

        public int BaseHitPoints => _baseHitPoints;

        public int BaseMovementPoints => _baseMovementPoints;

        public int BaseSkillResource => _baseSkillResource;

        public string[] Tags => _tags;
    }
}
