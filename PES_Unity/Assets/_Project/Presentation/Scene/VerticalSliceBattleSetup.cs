using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Core.TurnSystem;
using PES.Presentation.Adapters;
using PES.Presentation.Configuration;

namespace PES.Presentation.Scene
{
    public sealed class VerticalSliceBattleSetup
    {
        private VerticalSliceBattleSetup(
            MoveActionPolicy effectiveMovePolicy,
            BasicAttackActionPolicy? basicAttackPolicyOverride,
            SkillActionPolicy? skillPolicyOverride,
            IReadOnlyList<BattleActorDefinition> actorDefinitions,
            int seed,
            IReadOnlyDictionary<EntityId, SkillActionPolicy[]> skillLoadoutMap,
            IReadOnlyList<BattleActorArchetypeBinding> actorBindings)
        {
            EffectiveMovePolicy = effectiveMovePolicy;
            BasicAttackPolicyOverride = basicAttackPolicyOverride;
            SkillPolicyOverride = skillPolicyOverride;
            ActorDefinitions = actorDefinitions;
            Seed = seed;
            SkillLoadoutMap = skillLoadoutMap;
            ActorBindings = actorBindings;
        }

        public MoveActionPolicy EffectiveMovePolicy { get; }

        public BasicAttackActionPolicy? BasicAttackPolicyOverride { get; }

        public SkillActionPolicy? SkillPolicyOverride { get; }

        public IReadOnlyList<BattleActorDefinition> ActorDefinitions { get; }

        public int Seed { get; }

        public IReadOnlyDictionary<EntityId, SkillActionPolicy[]> SkillLoadoutMap { get; }

        public IReadOnlyList<BattleActorArchetypeBinding> ActorBindings { get; }

        public static VerticalSliceBattleSetup Create(
            CombatRuntimeConfigAsset runtimeConfig,
            EntityArchetypeAsset unitAArchetype,
            EntityArchetypeAsset unitBArchetype,
            int seed = 7)
        {
            var runtimePolicies = CombatRuntimePolicyProvider.FromAsset(runtimeConfig);
            var effectiveMovePolicy = runtimePolicies.MovePolicyOverride
                ?? new MoveActionPolicy(maxMovementCostPerAction: 6, maxVerticalStepPerTile: 1);

            var actorBindings = new[]
            {
                new BattleActorArchetypeBinding(
                    VerticalSliceBattleLoop.UnitA,
                    teamId: 1,
                    startPosition: new Position3(0, 0, 0),
                    archetype: unitAArchetype),
                new BattleActorArchetypeBinding(
                    VerticalSliceBattleLoop.UnitB,
                    teamId: 2,
                    startPosition: new Position3(2, 0, 1),
                    archetype: unitBArchetype),
            };

            var actorDefinitions = EntityArchetypeRuntimeAdapter.BuildActorDefinitions(actorBindings);
            var skillLoadoutMap = EntityArchetypeRuntimeAdapter.BuildSkillLoadoutMap(actorBindings);

            return new VerticalSliceBattleSetup(
                effectiveMovePolicy,
                runtimePolicies.BasicAttackPolicyOverride,
                runtimePolicies.SkillPolicyOverride,
                actorDefinitions,
                seed,
                skillLoadoutMap,
                actorBindings);
        }

        public void ApplyRuntimeResources(BattleState state)
        {
            EntityArchetypeRuntimeAdapter.ApplyRuntimeResources(state, ActorBindings);
        }
    }
}
