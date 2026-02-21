using System;
using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Core.TurnSystem;
using PES.Presentation.Configuration;

namespace PES.Presentation.Adapters
{
    public readonly struct BattleActorArchetypeBinding
    {
        public BattleActorArchetypeBinding(EntityId actorId, int teamId, Position3 startPosition, EntityArchetypeAsset archetype)
        {
            ActorId = actorId;
            TeamId = teamId;
            StartPosition = startPosition;
            Archetype = archetype;
        }

        public EntityId ActorId { get; }

        public int TeamId { get; }

        public Position3 StartPosition { get; }

        public EntityArchetypeAsset Archetype { get; }
    }

    public static class EntityArchetypeRuntimeAdapter
    {
        public static BattleActorDefinition BuildActorDefinition(EntityId actorId, int teamId, Position3 startPosition, EntityArchetypeAsset archetype)
        {
            if (archetype == null)
            {
                return new BattleActorDefinition(actorId, teamId, startPosition, 40, 6);
            }

            var stats = archetype.ToCombatantStats();
            return new BattleActorDefinition(
                actorId,
                teamId,
                startPosition,
                archetype.StartHitPoints,
                archetype.StartMovementPoints,
                stats.Rapidity);
        }

        public static IReadOnlyList<BattleActorDefinition> BuildActorDefinitions(IReadOnlyList<BattleActorArchetypeBinding> bindings)
        {
            if (bindings == null || bindings.Count == 0)
            {
                return Array.Empty<BattleActorDefinition>();
            }

            var definitions = new BattleActorDefinition[bindings.Count];
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                definitions[i] = BuildActorDefinition(binding.ActorId, binding.TeamId, binding.StartPosition, binding.Archetype);
            }

            return definitions;
        }

        public static void ApplyRuntimeResources(BattleState state, EntityId actorId, EntityArchetypeAsset archetype)
        {
            if (state == null || archetype == null)
            {
                return;
            }

            state.SetEntitySkillResource(actorId, archetype.StartSkillResource);
            state.SetEntityRpgStats(actorId, archetype.ToCombatantStats());
        }

        public static void ApplyRuntimeResources(BattleState state, IReadOnlyList<BattleActorArchetypeBinding> bindings)
        {
            if (state == null || bindings == null)
            {
                return;
            }

            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                ApplyRuntimeResources(state, binding.ActorId, binding.Archetype);
            }
        }

        public static IReadOnlyDictionary<EntityId, SkillActionPolicy[]> BuildSkillLoadoutMap(
            EntityId actorA,
            EntityArchetypeAsset archetypeA,
            EntityId actorB,
            EntityArchetypeAsset archetypeB)
        {
            return BuildSkillLoadoutMap(
                new[]
                {
                    new BattleActorArchetypeBinding(actorA, 1, default, archetypeA),
                    new BattleActorArchetypeBinding(actorB, 2, default, archetypeB),
                });
        }

        public static IReadOnlyDictionary<EntityId, SkillActionPolicy[]> BuildSkillLoadoutMap(IReadOnlyList<BattleActorArchetypeBinding> bindings)
        {
            if (bindings == null || bindings.Count == 0)
            {
                return new Dictionary<EntityId, SkillActionPolicy[]>(0);
            }

            var map = new Dictionary<EntityId, SkillActionPolicy[]>(bindings.Count);
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                map[binding.ActorId] = ToPolicies(binding.Archetype);
            }

            return map;
        }

        private static SkillActionPolicy[] ToPolicies(EntityArchetypeAsset archetype)
        {
            if (archetype == null || archetype.Skills.Count == 0)
            {
                return Array.Empty<SkillActionPolicy>();
            }

            var list = new List<SkillActionPolicy>(archetype.Skills.Count);
            for (var i = 0; i < archetype.Skills.Count; i++)
            {
                var skill = archetype.Skills[i];
                if (skill == null)
                {
                    continue;
                }

                list.Add(skill.ToPolicy());
            }

            return list.ToArray();
        }
    }
}
