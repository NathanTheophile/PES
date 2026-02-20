using System.Reflection;
using NUnit.Framework;
using PES.Core.Simulation;
using PES.Presentation.Adapters;
using PES.Presentation.Configuration;
using UnityEngine;

namespace PES.Tests.EditMode
{
    public sealed class EntityArchetypeAdapterTests
    {
        [Test]
        public void ToInitializationData_WhenAssetIsConfigured_ReturnsStableDeterministicData()
        {
            var asset = ScriptableObject.CreateInstance<EntityArchetypeAsset>();
            SetPrivateField(asset, "_baseHitPoints", 120);
            SetPrivateField(asset, "_baseMovementPoints", 7);
            SetPrivateField(asset, "_baseSkillResource", 5);
            SetPrivateField(asset, "_tags", new[] { "  boss", "pirate", "Pirate", "", "boss" });

            var entityId = new EntityId(99);
            var spawn = new Position3(1, 0, 2);

            var first = EntityArchetypeAdapter.ToInitializationData(asset, entityId, spawn);
            var second = EntityArchetypeAdapter.ToInitializationData(asset, entityId, spawn);

            Assert.That(first.EntityId, Is.EqualTo(entityId));
            Assert.That(first.SpawnPosition, Is.EqualTo(spawn));
            Assert.That(first.HitPoints, Is.EqualTo(120));
            Assert.That(first.MovementPoints, Is.EqualTo(7));
            Assert.That(first.SkillResource, Is.EqualTo(5));
            Assert.That(first.Tags, Is.EqualTo(new[] { "boss", "pirate" }));

            Assert.That(second.EntityId, Is.EqualTo(first.EntityId));
            Assert.That(second.SpawnPosition, Is.EqualTo(first.SpawnPosition));
            Assert.That(second.HitPoints, Is.EqualTo(first.HitPoints));
            Assert.That(second.MovementPoints, Is.EqualTo(first.MovementPoints));
            Assert.That(second.SkillResource, Is.EqualTo(first.SkillResource));
            Assert.That(second.Tags, Is.EqualTo(first.Tags));

            Object.DestroyImmediate(asset);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'");
            field.SetValue(target, value);
        }
    }
}
