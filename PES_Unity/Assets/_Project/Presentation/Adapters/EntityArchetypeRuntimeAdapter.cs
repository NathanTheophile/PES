using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Core.TurnSystem;
using PES.Presentation.Configuration;

namespace PES.Presentation.Adapters
{
    public static class EntityArchetypeRuntimeAdapter
    {
        public static BattleActorDefinition BuildActorDefinition(EntityId actorId, int teamId, Position3 startPosition, EntityArchetypeAsset archetype)
        {
            if (archetype == null)
            {
                return new BattleActorDefinition(actorId, teamId, startPosition, 40, 6);
            }

            return new BattleActorDefinition(
                actorId,
                teamId,
                startPosition,
                archetype.StartHitPoints,
                archetype.StartMovementPoints);
        }

        public static void ApplyRuntimeResources(BattleState state, EntityId actorId, EntityArchetypeAsset archetype)
        {
            if (state == null || archetype == null)
            {
                return;
            }

            state.SetEntitySkillResource(actorId, archetype.StartSkillResource);
        }

        public static IReadOnlyDictionary<EntityId, SkillActionPolicy[]> BuildSkillLoadoutMap(
            EntityId actorA,
            EntityArchetypeAsset archetypeA,
            EntityId actorB,
            EntityArchetypeAsset archetypeB)
        {
            var map = new Dictionary<EntityId, SkillActionPolicy[]>(2)
            {
                [actorA] = ToPolicies(archetypeA),
                [actorB] = ToPolicies(archetypeB),
            };

            return map;
        }

        private static SkillActionPolicy[] ToPolicies(EntityArchetypeAsset archetype)
        {
            if (archetype == null || archetype.Skills == null || archetype.Skills.Length == 0)
            {
                return new SkillActionPolicy[0];
            }

            var list = new List<SkillActionPolicy>(archetype.Skills.Length);
            for (var i = 0; i < archetype.Skills.Length; i++)
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
