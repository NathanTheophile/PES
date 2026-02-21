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

            Object.DestroyImmediate(archetype);
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

            var loadout = EntityArchetypeRuntimeAdapter.BuildSkillLoadoutMap(
                new EntityId(100), archetypeA,
                new EntityId(101), archetypeB);

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
