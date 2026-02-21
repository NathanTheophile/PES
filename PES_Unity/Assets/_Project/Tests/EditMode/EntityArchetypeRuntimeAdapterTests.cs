using System.Reflection;
using NUnit.Framework;
using PES.Core.Simulation;
using PES.Presentation.Adapters;
using PES.Presentation.Configuration;
using UnityEngine;

namespace PES.Tests.EditMode
{
    public sealed class EntityArchetypeRuntimeAdapterTests
    {
        [Test]
        public void BuildActorDefinition_WhenArchetypeProvided_MapsStatsToBattleActorDefinition()
        {
            var archetype = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetype, "_startHitPoints", 77);
            SetField(archetype, "_startMovementPoints", 9);

            var definition = EntityArchetypeRuntimeAdapter.BuildActorDefinition(
                new EntityId(500),
                teamId: 3,
                startPosition: new Position3(4, 5, 1),
                archetype: archetype);

            Assert.That(definition.ActorId, Is.EqualTo(new EntityId(500)));
            Assert.That(definition.TeamId, Is.EqualTo(3));
            Assert.That(definition.StartHitPoints, Is.EqualTo(77));
            Assert.That(definition.StartMovementPoints, Is.EqualTo(9));

            Object.DestroyImmediate(archetype);
        }

        [Test]
        public void ApplyRuntimeResources_WhenArchetypeProvided_SetsSkillResourceInBattleState()
        {
            var archetype = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetype, "_startSkillResource", 6);

            var state = new BattleState();
            var actorId = new EntityId(42);
            EntityArchetypeRuntimeAdapter.ApplyRuntimeResources(state, actorId, archetype);

            var hasResource = state.TryGetEntitySkillResource(actorId, out var resource);
            Assert.That(hasResource, Is.True);
            Assert.That(resource, Is.EqualTo(6));

            Assert.That(state.TryGetEntityStat(actorId, EntityStatType.ActionPoints, out var actionPoints), Is.True);
            Assert.That(actionPoints, Is.EqualTo(6));
            Assert.That(state.TryGetEntityStat(actorId, EntityStatType.HitPoints, out var statHitPoints), Is.True);
            Assert.That(statHitPoints, Is.EqualTo(40));

            Object.DestroyImmediate(archetype);
        }

        [Test]
        public void ToStatBlock_MapsConfiguredRpgStats()
        {
            var archetype = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetype, "_startActionPoints", 11);
            SetField(archetype, "_startMovementPoints", 5);
            SetField(archetype, "_startRange", 7);
            SetField(archetype, "_startElevation", 2);
            SetField(archetype, "_startSummonSlots", 3);
            SetField(archetype, "_startHitPoints", 99);
            SetField(archetype, "_diligence", 12);
            SetField(archetype, "_quickness", 14);
            SetField(archetype, "_affinityElement", AttackElement.Elementaire);
            SetField(archetype, "_masteryContondante", 20);
            SetField(archetype, "_masteryPhysique", 30);
            SetField(archetype, "_masteryElementaire", 40);
            SetField(archetype, "_masterySpeciale", 50);
            SetField(archetype, "_masterySpirituelle", 60);
            SetField(archetype, "_criticalChancePercent", 15);
            SetField(archetype, "_criticalDamagePercent", 70);
            SetField(archetype, "_resistancePercent", 11);
            SetField(archetype, "_specialResistancePercent", 13);
            SetField(archetype, "_criticalResistancePercent", 9);

            var block = archetype.ToStatBlock();

            Assert.That(block.ActionPoints, Is.EqualTo(11));
            Assert.That(block.MovementPoints, Is.EqualTo(5));
            Assert.That(block.Range, Is.EqualTo(7));
            Assert.That(block.Elevation, Is.EqualTo(2));
            Assert.That(block.SummonSlots, Is.EqualTo(3));
            Assert.That(block.HitPoints, Is.EqualTo(99));
            Assert.That(block.Diligence, Is.EqualTo(12));
            Assert.That(block.Quickness, Is.EqualTo(14));
            Assert.That(block.AffinityElement, Is.EqualTo(AttackElement.Elementaire));
            Assert.That(block.MasteryContondante, Is.EqualTo(20));
            Assert.That(block.MasteryPhysique, Is.EqualTo(30));
            Assert.That(block.MasteryElementaire, Is.EqualTo(40));
            Assert.That(block.MasterySpeciale, Is.EqualTo(50));
            Assert.That(block.MasterySpirituelle, Is.EqualTo(60));
            Assert.That(block.CriticalChancePercent, Is.EqualTo(15));
            Assert.That(block.CriticalDamagePercent, Is.EqualTo(70));
            Assert.That(block.ResistancePercent, Is.EqualTo(11));
            Assert.That(block.SpecialResistancePercent, Is.EqualTo(13));
            Assert.That(block.CriticalResistancePercent, Is.EqualTo(9));

            Object.DestroyImmediate(archetype);
        }


        [Test]
        public void BuildActorDefinitions_WithBindings_MapsAllActors()
        {
            var archetypeA = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetypeA, "_startHitPoints", 30);
            var archetypeB = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetypeB, "_startHitPoints", 45);

            var bindings = new[]
            {
                new BattleActorArchetypeBinding(new EntityId(100), 1, new Position3(0, 0, 0), archetypeA),
                new BattleActorArchetypeBinding(new EntityId(101), 2, new Position3(2, 0, 1), archetypeB),
            };

            var definitions = EntityArchetypeRuntimeAdapter.BuildActorDefinitions(bindings);

            Assert.That(definitions.Count, Is.EqualTo(2));
            Assert.That(definitions[0].StartHitPoints, Is.EqualTo(30));
            Assert.That(definitions[1].StartHitPoints, Is.EqualTo(45));

            Object.DestroyImmediate(archetypeA);
            Object.DestroyImmediate(archetypeB);
        }

        [Test]
        public void ApplyRuntimeResources_WithBindings_SetsEachActorResource()
        {
            var archetypeA = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetypeA, "_startSkillResource", 3);
            var archetypeB = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetypeB, "_startSkillResource", 7);

            var bindings = new[]
            {
                new BattleActorArchetypeBinding(new EntityId(100), 1, new Position3(0, 0, 0), archetypeA),
                new BattleActorArchetypeBinding(new EntityId(101), 2, new Position3(2, 0, 1), archetypeB),
            };

            var state = new BattleState();
            EntityArchetypeRuntimeAdapter.ApplyRuntimeResources(state, bindings);

            Assert.That(state.TryGetEntitySkillResource(new EntityId(100), out var valueA), Is.True);
            Assert.That(valueA, Is.EqualTo(3));
            Assert.That(state.TryGetEntitySkillResource(new EntityId(101), out var valueB), Is.True);
            Assert.That(valueB, Is.EqualTo(7));

            Object.DestroyImmediate(archetypeA);
            Object.DestroyImmediate(archetypeB);
        }

        [Test]
        public void BuildSkillLoadoutMap_ConvertsSkillAssetsToPolicyArraysPerActor()
        {
            var skillA = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            SetField(skillA, "_skillId", 10);
            SetField(skillA, "_baseDamage", 13);

            var skillB = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            SetField(skillB, "_skillId", 11);
            SetField(skillB, "_baseDamage", 21);

            var archetypeA = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetypeA, "_skills", new[] { skillA, skillB });
            var archetypeB = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetField(archetypeB, "_skills", new[] { skillB });

            var bindings = new[]
            {
                new BattleActorArchetypeBinding(new EntityId(100), 1, new Position3(0, 0, 0), archetypeA),
                new BattleActorArchetypeBinding(new EntityId(101), 2, new Position3(2, 0, 1), archetypeB),
            };

            var loadout = EntityArchetypeRuntimeAdapter.BuildSkillLoadoutMap(bindings);

            Assert.That(loadout[new EntityId(100)].Length, Is.EqualTo(2));
            Assert.That(loadout[new EntityId(100)][0].SkillId, Is.EqualTo(10));
            Assert.That(loadout[new EntityId(100)][1].BaseDamage, Is.EqualTo(21));
            Assert.That(loadout[new EntityId(101)].Length, Is.EqualTo(1));
            Assert.That(loadout[new EntityId(101)][0].SkillId, Is.EqualTo(11));

            Object.DestroyImmediate(skillA);
            Object.DestroyImmediate(skillB);
            Object.DestroyImmediate(archetypeA);
            Object.DestroyImmediate(archetypeB);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = target.GetType().GetField(fieldName, flags);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
